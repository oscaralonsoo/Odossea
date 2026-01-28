using System.Collections;
using UnityEngine;

public class Crusher : MonoBehaviour
{
    [Header("Movement")]
    public float downSpeed = 10f;
    public float upSpeed = 3f;

    [Header("Timing")]
    public float waitTimeAtBottom = 1.5f;
    public float waitTimeAtTop = 1.0f;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private bool waiting = false;
    private bool returning = false;
    private bool waitingAtTop = false;
    private float waitTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        // ?? Bajando
        if (!waiting && !returning && !waitingAtTop)
        {
            rb.linearVelocity = new Vector2(0f, -downSpeed);
        }

        // ?? Esperando abajo (Static)
        else if (waiting)
        {
            waitTimer -= Time.fixedDeltaTime;

            if (waitTimer <= 0f)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                waiting = false;
                returning = true;
            }
        }

        // ?? Subiendo
        else if (returning)
        {
            float newY = Mathf.MoveTowards(
                rb.position.y,
                startPosition.y,
                upSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(new Vector2(rb.position.x, newY));

            if (Mathf.Abs(rb.position.y - startPosition.y) < 0.01f)
            {
                rb.MovePosition(startPosition);
                rb.linearVelocity = Vector2.zero;
                returning = false;
                waitingAtTop = true;
                waitTimer = waitTimeAtTop;
            }
        }

        // ?? Esperando arriba
        else if (waitingAtTop)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                waitingAtTop = false;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!waiting && !returning && !waitingAtTop &&
            (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
             collision.gameObject.layer == LayerMask.NameToLayer("Default")))
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
            waiting = true;
            waitTimer = waitTimeAtBottom;
        }

        if (!waiting && !returning && !waitingAtTop &&
             (collision.gameObject.layer == LayerMask.NameToLayer("Bones")))
        {
            StartCoroutine(GoUpLate());
        }
    }

    IEnumerator GoUpLate()
    {
        yield return new WaitForSeconds(0.3f);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        waiting = true;
        waitTimer = waitTimeAtBottom;
    }
}
