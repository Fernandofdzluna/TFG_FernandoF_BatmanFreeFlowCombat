using Cinemachine;
using DG.Tweening;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;


#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        public Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        [SerializeField] private GameObject characterMesh;

        [SerializeField] float timeToAproachFarEnemy;
        public GameObject enemySelected = null;
        Vector3 playerToEnemyDir;
        private bool movingToEnemy = false;
        private bool playerMovingFree = true;

        private const float _threshold = 0.01f;

        private bool _hasAnimator = false;

        [Space(10)]
        [Header("Particle Effect")]
        public GameObject ParticleHit;

        bool firstPunchGiven = false;
        bool stunned = false;
        bool lastKill = false;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            playerMovingFree = true;
            ParticleHit.SetActive(false);
            firstPunchGiven = false;
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            if (_animator != null) _hasAnimator = true;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        }

        private void Update()
        {
            RaycastChecker();
            if (playerMovingFree)
            {
                Move();
            }
            AttackChecker();
            DodgeChecker();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat("Speed", _animationBlend);
            }
        }

        public bool canPunch = true;
        int punchCount;
        private void AttackChecker()
        {
            if(_input.attack && enemySelected != null)
            {
                Enemys = GameObject.FindGameObjectsWithTag("NPC");
                if (Enemys.Length == 1 && enemySelected.GetComponent<NPC_Script>().lives == 1)
                {
                    StartCoroutine(LastKillCamera());
                }

                playerToEnemyDir = (enemySelected.transform.position - transform.position).normalized;
                float distancePlayerEnemy = Vector3.Distance(transform.position, enemySelected.transform.position);
                if (distancePlayerEnemy > 2)
                {
                    playerMovingFree = false;
                    if (movingToEnemy == false)
                    {
                        movingToEnemy = true;
                        float distanceInTime = (distancePlayerEnemy / 0.1f) / timeToAproachFarEnemy;
                        enemySelected.GetComponent<NPC_Script>().GettingJumped();
                        //transform.DOLookAt(enemySelected.transform.position, 0.8f);
                        canPunch = false;
                        StartCoroutine(PunchCooldown());
                        _animator.SetTrigger("Jump");
                        AproachFarEnemy();
                        punchCount = 0;
                    }
                }
                else if(canPunch && movingToEnemy == false)
                {
                    canPunch = false;
                    StartCoroutine(PunchCooldown());
                    punchCount += 1;
                    if (punchCount > 4) punchCount = 1;

                    float newCalculatedDistance = Vector3.Distance(transform.position, enemySelected.transform.position);
                    transform.DOLookAt(enemySelected.transform.position, 0.5f);
                    _animator.SetFloat("PunchCount", punchCount);
                    _animator.SetTrigger("Punch");
                }
                _input.attack = false;
            }

            IEnumerator PunchCooldown()
            {
                yield return new WaitForSeconds(0.6f);
                canPunch = true;
            }

            IEnumerator LastKillCamera()
            {
                lastKill = true;
                CinemachineVirtualCamera cinemachineVirtual = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
                cinemachineVirtual.m_Lens.FieldOfView = 10;
                Time.timeScale = 0.3f;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
                yield return new WaitForSeconds(1.5f);
                cinemachineVirtual.m_Lens.FieldOfView = 40;
                while (Time.timeScale < 1f)
                {
                    Time.timeScale += (1f / 4f) * Time.unscaledDeltaTime;
                    Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                }
            }

            //IEnumerator JumpLastKill()
            //{
            //    yield return new WaitForSeconds(timeToAproachFarEnemy/2);
            //    float distancePlayerEnemy = Vector3.Distance(transform.position, enemySelected.transform.position);
            //    if (distancePlayerEnemy < 2)
            //    {
            //        StartCoroutine(LastKillCamera());
            //    }
            //}
        }

        int dodgesCount = 1;
        public bool dodge = false;
        public GameObject chargingEnemy;
        private void DodgeChecker()
        {
            if (_input.dodge && dodge == false && chargingEnemy != null)
            {
                if (Vector3.Distance(transform.position, chargingEnemy.transform.position) < 2)
                {
                    dodge = true;
                    transform.DOLookAt(chargingEnemy.transform.position, 0.5f);
                    _animator.SetFloat("DodgesBlock", dodgesCount);
                    _animator.SetTrigger("Dodge");
                    StartCoroutine(DodgeCooldown());
                    dodgesCount += 1;
                    if (dodgesCount > 3) dodgesCount = 0;
                }
            }
            _input.dodge = false;

            IEnumerator DodgeCooldown()
            {
                enemySelected = chargingEnemy;
                chargingEnemy = null;
                yield return new WaitForSeconds(1);
                dodge = false;
            }
        }

        GameObject[] Enemys;
        GameObject newEnemy;
        public void DoAttackHit()
        {
            //if (stunned) return;

            if(firstPunchGiven == false)
            {
                firstPunchGiven = true;
                GameManager.instance.beginFight();
            }

            playerToEnemyDir = (enemySelected.transform.position - transform.position).normalized;
            float secondDistance = Vector3.Distance(transform.position, enemySelected.transform.position);
            if (secondDistance <= 2 || lastKill == true)
            {
                ParticleHit.SetActive(true);
                StartCoroutine(ShakeCamera(0.3f, 12));
                enemySelected.GetComponent<NPC_Script>().whichPunchReciving = punchCount;
                enemySelected.GetComponent<NPC_Script>().ChangeLives(1);
                if (enemySelected.GetComponent<NPC_Script>().isDead)
                {
                    Enemys = GameObject.FindGameObjectsWithTag("NPC");
                    if (Enemys.Length == 0)
                    {
                        enemySelected = null;
                    }
                    else
                    {
                        float minDistance = Vector3.Distance(transform.position, Enemys[0].transform.position);
                        newEnemy = Enemys[0];
                        for(int i = 1; i < Enemys.Length; i++)
                        {
                            if (Enemys[i].CompareTag("NPC") && Enemys[i].GetComponent<NPC_Script>().isDead == false && (Vector3.Distance(transform.position, Enemys[i].transform.position) < minDistance))
                            {
                                minDistance = (Vector3.Distance(transform.position, Enemys[i].transform.position));
                                newEnemy = Enemys[i].gameObject;
                            }
                        }
                        enemySelected = newEnemy;
                    }
                }
            }
        }

        private void AproachFarEnemy()
        {
            transform.DOLookAt(enemySelected.transform.position, .2f);
            transform.DOMove(enemySelected.transform.position - playerToEnemyDir * 0.75f, timeToAproachFarEnemy, false).OnComplete(AproachFinished);
        }

        private void AproachFinished()
        {
            movingToEnemy = false;
            playerMovingFree = true;
        }

        private void RaycastChecker()
        {
            RaycastHit hit;
            int layerMask = 1 << LayerMask.NameToLayer("Enemy");

            if (Physics.SphereCast(transform.position, 3, Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward, out hit, 10, ~layerMask))
            {
                if (hit.collider.tag == "NPC" && movingToEnemy == false && hit.collider.GetComponent<NPC_Script>().isDead == false)
                {
                    enemySelected = hit.collider.gameObject;
                }
            }
        }

        void OnDrawGizmos()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            
            if (enemySelected != null)
            {
                Gizmos.color = transparentGreen;
                Gizmos.DrawSphere(enemySelected.transform.position + new Vector3(0, 1, 0), 0.5f);
            }
        }

        IEnumerator ShakeCamera(float time, int frequencyGain)
        {
            CinemachineVirtualCamera cinemachineVirtual = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
            cinemachineVirtual.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = frequencyGain;
            yield return new WaitForSeconds(time);
            cinemachineVirtual.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0.3f;
            ParticleHit.SetActive(false);
        }

        float punchedRecived = 0;
        public void ReciveHit()
        {
            stunned = true;
            StartCoroutine(ShakeCamera(0.6f, 20));
            playerMovingFree = false;
            canPunch = false;
            _animator.SetFloat("PunchedRecived", punchedRecived);
            StartCoroutine(BeingPunched());

            IEnumerator BeingPunched()
            {
                //Animation Puñetazo
                _animator.SetTrigger("RecivePunch");
                yield return new WaitForSeconds(1);
                stunned = false;
                playerMovingFree = true;
                canPunch = true;
                punchedRecived += 1;
                if (punchedRecived > 3) punchedRecived = 0;
            }
        }
    }
}