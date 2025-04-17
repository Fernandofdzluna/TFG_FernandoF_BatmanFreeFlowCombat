using StarterAssets;
using System.Collections;
using UnityEngine;

public class PunchCall : MonoBehaviour
{
    NPC_Script npcScript;
    ThirdPersonController playerScript;
    public GameObject HitParticlesEffect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HitParticlesEffect.SetActive(false);
        npcScript = GetComponentInParent<NPC_Script>();
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
    }

    public void DoPunchToPlayer()
    {
        if(!playerScript.dodge)
        {
            playerScript.ReciveHit();
            HitParticlesEffect.SetActive(true);
            StartCoroutine(ParticlesEffectOff());
        }
    }

    IEnumerator ParticlesEffectOff()
    {
        yield return new WaitForSeconds(1);
        HitParticlesEffect.SetActive(false);
    }
}
