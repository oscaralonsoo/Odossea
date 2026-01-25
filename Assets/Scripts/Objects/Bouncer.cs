using UnityEngine;
using System.Collections.Generic;

public class Bouncer : MonoBehaviour
{
    public float bounceForce = 10f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Bone") || collision.transform.CompareTag("CubeTrigger"))
        {
            GameObject player = collision.transform.parent.gameObject;
            ApplyBounce(player.GetComponent<Rigidbody2D>());

            foreach (PlayerController.Bone bone in player.GetComponent<PlayerController>().bones)
            {
                ApplyBounce(bone.rigidbody2D);
            }
        }

    }

    private void ApplyBounce(Rigidbody2D rb)
    {
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
    }
}
