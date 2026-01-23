using UnityEngine;
using System.Collections.Generic;

public class Bouncer : MonoBehaviour
{
    public float bounceForce = 10f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Bone"))
        {
            GameObject player = collision.transform.parent.gameObject;

            foreach (PlayerController.Bone bone in player.GetComponent<PlayerController>().bones)
            {
                ApplyBounce(bone);
            }
        }

    }

    private void ApplyBounce(PlayerController.Bone bone)
    {
        bone.rigidbody2D.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
    }
}
