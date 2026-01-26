using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bouncer : MonoBehaviour
{
    public float bounceForce = 10f;
    public float bounceDelay = 0.05f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Bone") || collision.transform.CompareTag("CubeTrigger"))
        {
            GameObject player = collision.transform.parent.gameObject;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                StartCoroutine(ApplyBounceDelayed(playerRb));

            foreach (PlayerController.Bone bone in player.GetComponent<PlayerController>().bones)
            {
                if (bone.rigidbody2D != null)
                    StartCoroutine(ApplyBounceDelayed(bone.rigidbody2D));
            }
        }
    }

    private IEnumerator ApplyBounceDelayed(Rigidbody2D rb)
    {
        yield return new WaitForSeconds(bounceDelay);

        if (rb != null)
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
    }
}
