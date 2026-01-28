using UnityEngine;

public class CameraProxy : MonoBehaviour
{
    public PlayerController player;

    void Start()
    {

    }

    void Update()
    {
        if (player.gameObject.activeInHierarchy)
        {
            transform.position = player.GetCenterPosition();

        } else
        {
            transform.position = player.GetCenterLastPosition();
        }
    }

    
}
