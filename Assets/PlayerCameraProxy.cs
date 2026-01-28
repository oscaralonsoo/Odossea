using UnityEngine;

public class PlayerCameraProxy : MonoBehaviour
{
    public PlayerController player;

    void Start()
    {
        transform.position = player.GetCenterPosition();
    }

    void Update()
    {
        if (!player.isDead)
        {
            transform.position = player.GetCenterPosition();

        } 
        else
        {
            transform.position = player.GetCenterLastPosition();
        }
    }

    
}
