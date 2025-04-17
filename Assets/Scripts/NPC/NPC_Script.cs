using StarterAssets;
using UnityEngine;
using DG.Tweening;
using UnityEditor.Experimental.GraphView;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class NPC_Script : MonoBehaviour
{
    GameObject player;
    ThirdPersonController player_script;
    Animator selfAnimator;
    CharacterController characterController;
    [SerializeField] GameObject HitImage;

    [Space(10)]
    [Header("Values")]
    public int lives = 5;
    public bool isDead = false;
    private float moveSpeed = 2;
    private Vector3 moveDirection;

    internal int whichPunchReciving;
    bool fightMode = false;
    bool isMoving = false;
    internal bool stunned = false;
    internal bool isWaiting = true;
    bool persuingPlayer = false;
    bool getBack = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        player_script = player.GetComponent<ThirdPersonController>();
        selfAnimator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();
        lives = 5;
        isDead = false;
        stunned = false;
        HitImage.SetActive(false);
        StartCoroutine(StartMovingCoroutine());
    }

    public void ChangeLives(int damage)
    {
        isMoving = false;
        selfAnimator.SetFloat("ImpactAnim", whichPunchReciving);
        selfAnimator.SetTrigger("GetPunch");
        stunned = true;
        StartCoroutine(GetUp());

        if (persuingPlayer)
        {
            persuingPlayer = false;
            GameManager.instance.DonePersuing();
            HitImage.SetActive(false);
        }

        lives -= damage;
        if (lives <= 0 || player_script.lastKill)
        {
            Debug.Log("DENTRO");
            lives = 0;
            GameManager.instance.ChangeHitCount(2);
            isDead = true;
            selfAnimator.SetBool("Dead", true);
            tag = "Untagged";
        }
        else
            GameManager.instance.ChangeHitCount(1);


        IEnumerator GetUp()
            {
                yield return new WaitForSeconds(2);
                stunned = false;
            }
    }

    private void LateUpdate()
    {
        if (isDead == false && fightMode == true)
        {
            MoveEnemy(moveDirection);
            HitImage.transform.LookAt(GameObject.FindGameObjectWithTag("MainCamera").transform.position);
        }
        else if(isDead)
        {
            HitImage.SetActive(false);
            GetComponent<CharacterController>().enabled = false;
        }

        if(persuingPlayer) if (Vector3.Distance(transform.position, player.transform.position) < 2.5f) HitImage.SetActive(true);
    }

    public void EnterFightMode()
    {
        if (fightMode == false)
        {
            fightMode = true; isMoving = true;
            selfAnimator.SetBool("FightingIddle", true);
            selfAnimator.SetFloat("FightMove", 1);
        }
    }

    IEnumerator StartMovingCoroutine()
    {
        //Waits until the enemy is not assigned to no action like attacking or retreating
        yield return new WaitUntil(() => isWaiting == true);

        if(persuingPlayer)
        {
            moveDirection = Vector3.forward;
            isMoving = true;
        }
        else if(getBack)
        {
            moveDirection = -Vector3.forward;
            isMoving = true;
        }
        else
        {
            int randomChance = Random.Range(0, 2);

            if (randomChance == 1)
            {
                int randomDir = Random.Range(0, 2);
                moveDirection = randomDir == 1 ? Vector3.right : Vector3.left;
                isMoving = true;
            }
            else
            {
                StopMoving();
            }
        }

            yield return new WaitForSeconds(2);

        StartCoroutine(StartMovingCoroutine());
    }

    private void MoveEnemy(Vector3 direction)
    {
        transform.DOLookAt(player.transform.position, 1f);

        moveSpeed = 1;

        if (direction == Vector3.forward)
            moveSpeed = 5;
        if (direction == -Vector3.forward)
            moveSpeed = 2;


        //Establecer valores animator
        selfAnimator.SetFloat("FightMove", (characterController.velocity.normalized.magnitude * direction.z) / (5 / moveSpeed), .2f, Time.deltaTime);
        selfAnimator.SetBool("Lateral", (direction == Vector3.right || direction == Vector3.left));
        selfAnimator.SetFloat("LateralValue", direction.normalized.x, .2f, Time.deltaTime);

        if (!isMoving)
            return;

        Vector3 dir = (player.transform.position - transform.position).normalized;
        Vector3 pDir = Quaternion.AngleAxis(90, Vector3.up) * dir; //Vector perpendicular to direction
        Vector3 movedir = Vector3.zero;

        Vector3 finalDirection = Vector3.zero;

        if (direction == Vector3.forward)
            finalDirection = dir;
        if (direction == Vector3.right || direction == Vector3.left)
            finalDirection = (pDir * direction.normalized.x);
        if (direction == -Vector3.forward)
            finalDirection = -transform.forward;
        if (direction == Vector3.right || direction == Vector3.left)
            moveSpeed /= 1.5f;

        movedir += finalDirection * moveSpeed * Time.deltaTime;

        characterController.Move(movedir);

        if (Vector3.Distance(transform.position, player.transform.position) < 1f && stunned == false && persuingPlayer == true)
        {
            if (GameManager.instance.CanPunchToPlayer(this)) Attack();
            StopMoving();
        }
    }

    public void StopMoving()
    {
        isMoving = false;
        moveDirection = Vector3.zero;
        if (characterController.enabled)
            characterController.Move(moveDirection);
    }

    public void PersuePlayer()
    {
        if(getBack || stunned)
        {
            persuingPlayer = false;
            GameManager.instance.DonePersuing();
            HitImage.SetActive(false);
        }
        else
        {
            persuingPlayer = true;
            player_script.chargingEnemy = this.gameObject;
        }
    }

    public void Attack()
    {
        selfAnimator.SetTrigger("ThrowPunch");
        persuingPlayer = false;
        GameManager.instance.DonePersuing();
        HitImage.SetActive(false);
        player_script.chargingEnemy = null;
        StartCoroutine(TimeGettingBack());

        IEnumerator TimeGettingBack()
        {
            yield return new WaitForSeconds(0.5f);
            if (!stunned) getBack = true;
            yield return new WaitForSeconds(2);
            getBack = false;
        }
    }

    public void GettingJumped()
    {
        isWaiting = false;
        stunned = true;
        StopMoving();
        if (persuingPlayer)
        {
            persuingPlayer = false;
            GameManager.instance.DonePersuing();
            HitImage.SetActive(false);
        }
        StartCoroutine(PostJump());

        IEnumerator PostJump()
        {
            yield return new WaitForSeconds(2);
            isWaiting = true;
            isMoving = true;
        }
    }
}