using UnityEngine;

public class KillZone : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("CubeTrigger"))
        {
            GameObject player = collision.transform.parent.gameObject;
            player.GetComponent<PlayerController>().Die();
        }
    }

}

/*if (collision.transform.CompareTag("Bone") || collision.transform.CompareTag("CubeTrigger"))
{
    GameObject player = collision.transform.parent.gameObject;
    ApplyBounce(player.GetComponent<Rigidbody2D>());

    foreach (PlayerController.Bone bone in player.GetComponent<PlayerController>().bones)
    {
        ApplyBounce(bone.rigidbody2D);
    }
}*/