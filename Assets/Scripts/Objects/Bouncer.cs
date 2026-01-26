using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

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
        if (collision.transform.CompareTag("Bone"))
        {
            if (!isSquashing)
                StartCoroutine(Squash());

            Rigidbody2D[] rigidbodies = collision.transform.parent.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D rigidbody in rigidbodies)
            {
                if (rigidbody != null)
                    StartCoroutine(ApplyBounceDelayed(rigidbody));
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
