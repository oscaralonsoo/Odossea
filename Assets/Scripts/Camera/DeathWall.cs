using UnityEngine;

public class DeathWall : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {

        PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.Die();
        }
    }
}
