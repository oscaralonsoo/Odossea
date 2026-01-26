using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bouncer : MonoBehaviour
{
    public float bounceForce = 10f;
    public float bounceDelay = 0.05f;

    [Header("Squash Animation")]
    public float squashScale = 0.85f;
    public float squashTime = 0.08f;

    private Vector3 originalScale;
    private bool isSquashing;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Bone") || collision.transform.CompareTag("CubeTrigger"))
        {
            if (!isSquashing)
                StartCoroutine(Squash());

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

    private IEnumerator Squash()
    {
        isSquashing = true;

        Vector3 targetScale = originalScale * squashScale;

        float t = 0f;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / squashTime);
            yield return null;
        }

        t = 0f;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / squashTime);
            yield return null;
        }

        transform.localScale = originalScale;
        isSquashing = false;
    }

    private IEnumerator ApplyBounceDelayed(Rigidbody2D rb)
    {
        yield return new WaitForSeconds(bounceDelay);

        if (rb != null)
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
    }
}
