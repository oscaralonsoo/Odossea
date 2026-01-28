using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bone")) return;
        PlayerController player = null;
        player = collision.transform.parent.GetComponent<PlayerController>();

        if (player == null) return;

        player.OnCheckPoint();
    }
}
