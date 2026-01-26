using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class JumpPadBehaviour : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] float launchForce = 15f;
    [SerializeField] Vector2 launchDirection = Vector2.up;

    private HashSet<Rigidbody2D> bodiesOnPad = new HashSet<Rigidbody2D>();
    private bool isActive = false;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Rigidbody2D body = collider.attachedRigidbody;
        if (body == null) return;

        bodiesOnPad.Add(body);

        // SOLO esto depende de isActive
        if (isActive)
            Launch(body);
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        Rigidbody2D body = collider.attachedRigidbody;
        if (body != null)
            bodiesOnPad.Remove(body);
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void Toggle()
    {
        isActive = !isActive;
    }
    public void LaunchAll()
    {
        foreach (var body in bodiesOnPad)
        {
            if (body != null)
                Launch(body);
        }
    }

    private void Launch(Rigidbody2D body)
    {
        body.linearVelocity = Vector2.zero;
        body.AddForce(launchDirection.normalized * launchForce, ForceMode2D.Impulse);
    }
}
