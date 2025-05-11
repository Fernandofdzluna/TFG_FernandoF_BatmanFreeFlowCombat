using DG.Tweening;
using StarterAssets;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    ThirdPersonController playerScript;
    GameObject[] enemys;
    public bool anyEnemyPunching = false;

    [SerializeField] GameObject HitsCountText;
    internal int hitCount;

    public GameObject restartingGame;

    private void OnEnable()
    {
        if (instance == null && instance != this)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }

        enemys = GameObject.FindGameObjectsWithTag("NPC");
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
        anyEnemyPunching = false;
        Cursor.visible = false;
        hitCount = 0;
        HitsCountText.GetComponent<TMP_Text>().text = "0";
        HitsCountText.transform.DOShakePosition(100000, 5, 30);
    }

    public void beginFight()
    {
        enemys = GameObject.FindGameObjectsWithTag("NPC");
        for (int i = 0; i < enemys.Length; i++)
        {
            enemys[i].GetComponent<NPC_Script>().EnterFightMode();
        }

        StartCoroutine(FightManager());
    }

    public GameObject selectedEnemy;
    public bool enemyPersuing = false;
    internal GameObject maton1;
    internal GameObject maton2;
    IEnumerator FightManager()
    {
        NoNpcPersuing();

        if (enemyPersuing == false)
        {
            enemys = GameObject.FindGameObjectsWithTag("NPC");
            if (enemys.Length == 0) yield break;

            int randomNum = Random.Range(0, enemys.Length - 1);
            selectedEnemy = enemys[randomNum];

            enemyPersuing = true;
            selectedEnemy.GetComponent<NPC_Script>().PersuePlayer();

            int supportsAsigned = 2;
            for(int i = randomNum + 1; i < enemys.Length; i++)
            {
                if (supportsAsigned > 0)
                {
                    if (supportsAsigned == 2) maton1 = enemys[i];
                    else if (supportsAsigned == 1) maton2 = enemys[i];

                    enemys[i].GetComponent<NPC_Script>().matonesSupport = true; 
                    supportsAsigned -= 1;
                }
                else break;
            }
        }

            yield return new WaitForSeconds(1);
        StartCoroutine(FightManager());
    }

    public void NoNpcPersuing()
    {
        if(enemyPersuing == true)
        {
            if(selectedEnemy != null)
            {
                if(selectedEnemy.GetComponent<NPC_Script>().persuingPlayer == false)
                {
                    if (maton1 != null)
                    {
                        selectedEnemy = maton1;
                        maton1 = null;
                    }
                    else if (maton2 != null)
                    {
                        selectedEnemy = maton2;
                        maton2 = null;
                    }
                    else
                    {
                        selectedEnemy = null;
                        maton1 = null;
                        maton2 = null;
                        enemyPersuing = false;
                    }
                }
            }
            else
            {
                selectedEnemy = null;
                maton1 = null;
                maton2 = null;
                enemyPersuing = false;
            }
        }
    }

    public bool CanPunchToPlayer(NPC_Script npc)
    {
        if (anyEnemyPunching == false && !npc.stunned)
        {
            anyEnemyPunching = true;
            StartCoroutine(waitUntilEnemyCanPunchAgain());
            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerator waitUntilEnemyCanPunchAgain()
    {
        yield return new WaitForSeconds(1);
        anyEnemyPunching = false;
    }

    bool deciding = false;
    public void DonePersuing()
    {
        if(deciding == false)
        {
            deciding = true; 

            if (enemyPersuing && maton1 == null && maton2 == null)
            {
                StartCoroutine(PersuingCooldown());
                deciding = false;
            }
            else if (enemyPersuing && maton1 != null)
            {
                maton1.GetComponent<NPC_Script>().PersuePlayer();
                selectedEnemy = maton1;
                maton1 = null;
                deciding = false;
            }
            else if (enemyPersuing && maton2 != null)
            {
                maton2.GetComponent<NPC_Script>().PersuePlayer();
                selectedEnemy = maton2;
                maton2 = null;
                deciding = false;
            }
            else
            {
                selectedEnemy = null;
                maton1 = null;
                maton2 = null;
                StartCoroutine(PersuingCooldown());
                deciding = false;
            }
        }

        IEnumerator PersuingCooldown()
        {
            yield return new WaitForSeconds(1);
            enemyPersuing = false;
        }
    }

    private void Update()
    {
        if (hitCount > 0)
        {
            HitsCountText.SetActive(true);

            if(time < maxTime)
            {
                time += Time.deltaTime;
            }
            else
            {
                ChangeHitCount(0);
            }
        }
        else if(hitCount <= 0)
        {
            HitsCountText.SetActive(false);
        }
    }

    float time = 0;
    float maxTime = 5;
    public void ChangeHitCount(int toAdd)
    {
        if(toAdd == 0)
        {
            hitCount = 0;
            HitsCountText.GetComponent<TMP_Text>().text = hitCount.ToString();
        }
        else
        {
            hitCount += toAdd;
            HitsCountText.GetComponent<TMP_Text>().text = hitCount.ToString();
        }

        time = 0;
    }

    public IEnumerator FinishGame()
    {
        Debug.Log("Dentro");
        yield return new WaitForSeconds(2);
        restartingGame.SetActive(true);
        yield return new WaitForSeconds(1);
        restartingGame.transform.GetChild(0).GetComponent<Text>().text = "4";
        yield return new WaitForSeconds(1);
        restartingGame.transform.GetChild(0).GetComponent<Text>().text = "3";
        yield return new WaitForSeconds(1);
        restartingGame.transform.GetChild(0).GetComponent<Text>().text = "2";
        yield return new WaitForSeconds(1);
        restartingGame.transform.GetChild(0).GetComponent<Text>().text = "1";
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene("TestScene");
    }
}
