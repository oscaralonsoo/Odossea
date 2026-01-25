using UnityEngine;
using UnityEngine.InputSystem;

public class FusionPlayers : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private PlayerController player1;
    [SerializeField] private PlayerController player2;

    private bool fusionActive = false;

    void Update()
    {
        if (BothPlayersReady())
        {
            if (!fusionActive)
            {
                fusionActive = true;
                StartFusion();
            }
        }
/*        else
        {
            if (fusionActive)
            {
                fusionActive = false;
                StopFusion();
            }
        }*/
    }

    private bool BothPlayersReady()
    {
        return Keyboard.current.lKey.isPressed;
    }

    private void StartFusion()
    {
        Debug.Log("¡Fusión activada!");
    }

    private void StopFusion()
    {
        Debug.Log("Fusión desactivada.");
    }
}
