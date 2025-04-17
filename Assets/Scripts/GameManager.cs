using DG.Tweening;
using StarterAssets;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    ThirdPersonController playerScript;
    GameObject[] enemys;
    public bool anyEnemyPunching = false;

    [SerializeField] GameObject HitsCountText;
    internal int hitCount;

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
    IEnumerator FightManager()
    {
        enemys = GameObject.FindGameObjectsWithTag("NPC");
        if (enemys.Length == 0) yield break;

        selectedEnemy = enemys[Random.Range(0, enemys.Length - 1)];

        if (enemyPersuing == false)
        {
            enemyPersuing = true;
            selectedEnemy.GetComponent<NPC_Script>().PersuePlayer();
        }

        yield return new WaitForSeconds(1);
        StartCoroutine(FightManager());
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

    public void DonePersuing()
    {
        if(enemyPersuing) StartCoroutine(PersuingCooldown());

        IEnumerator PersuingCooldown()
        {
            yield return new WaitForSeconds(2);
            enemyPersuing = false;
        }
    }

    private void Update()
    {
        if (hitCount > 0)
        {
            HitsCountText.SetActive(true);
        }
        else if(hitCount <= 0)
        {
            HitsCountText.SetActive(false);
        }
    }

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
    }
}
