using StarterAssets;
using UnityEngine;

public class DoAttackHit : MonoBehaviour
{
    public ThirdPersonController PlayerScript;

    public void Hit()
    {
        PlayerScript.DoAttackHit();
    }
}
