using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MarcScript : MonoBehaviour
{
    [Serializable]
    public class Bone
    {
        public Rigidbody2D rigidbody2D;
        public Collider2D collider2D;
        [HideInInspector] public bool grounded;
    }

    [Header("Bones & Joints")]
    [SerializeField] public List<Bone> bones;
    [SerializeField] RelativeJoint2D[] relativesJoints2D;
    [SerializeField] DistanceJoint2D[] distanceJoints2D;

    [Header("Movement")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force = 50f;
    [SerializeField] float downForce = 20f;
    [SerializeField] float jumpForce = 300f;
    [SerializeField] float maxXSpeed = 5f;
    [SerializeField] float maxYSpeed = 10f;

    [Header("Scaling")]
    [SerializeField] float scaleFactor = 1.2f;
    [SerializeField] float scaleSpeed = 5f;
    public int growNum = 1;
    public bool IsGrounded { get; private set; }
    public float AverageVerticalVelocity { get; private set; }

    private Vector3 targetScale = Vector3.one;
    private Vector2 inputDirection;
    private bool jumpRequested;

    private bool blockLeftMove;
    private bool blockRightMove;

    private void Awake()
    {
        relativesJoints2D = GetComponentsInChildren<RelativeJoint2D>();
        distanceJoints2D = GetComponentsInChildren<DistanceJoint2D>();
        InitScale();
    }

    public void OnMove(InputValue value) => inputDirection = value.Get<Vector2>();
    public void OnJump(InputValue value) { if (value.isPressed) jumpRequested = true; }

    public void SetMovementRestriction(bool left, bool right)
    {
        blockLeftMove = left;
        blockRightMove = right;
    }

    public void Grow(int growNumber)
    {
        for (int i = 0; i < growNumber; i++)
        {
            targetScale *= scaleFactor;
            growNum++;
        }
    }

    private void FixedUpdate()
    {
        ApplyScale();

        // ---------- Ground check ----------
        int groundedCount = 0;
        foreach (var bone in bones)
        {
            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded) groundedCount++;
        }

        IsGrounded = groundedCount > 0;

        // ---------- Input filtering ----------
        float moveX = inputDirection.x;
        if (blockLeftMove && moveX < 0) moveX = 0;
        if (blockRightMove && moveX > 0) moveX = 0;

        float verticalVelSum = 0f;

        foreach (var bone in bones)
        {
            var rb = bone.rigidbody2D;

            if (bone.grounded && groundedCount < 3)
                rb.AddForce(Vector2.down * downForce);

            if ((bone.grounded || groundedCount == 0) && moveX != 0)
                rb.AddForce(Vector2.right * moveX * force);

            if (IsGrounded && jumpRequested)
                rb.AddForce(Vector2.up * jumpForce);

            Vector2 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(vel.x, -maxXSpeed, maxXSpeed);
            vel.y = Mathf.Clamp(vel.y, -maxYSpeed, maxYSpeed);
            rb.linearVelocity = vel;

            verticalVelSum += vel.y;
        }

        AverageVerticalVelocity = verticalVelSum / bones.Count;
        jumpRequested = false;
    }

    // ---------- Scaling ----------
    private void InitScale()
    {
        targetScale = transform.localScale;
        for (int i = 1; i < growNum; i++)
            targetScale *= scaleFactor;

        ApplyScaleInstant();
    }

    private void ApplyScale()
    {
        Vector3 lastScale = transform.localScale;
        Vector3 newScale = Vector3.Lerp(lastScale, targetScale, Time.fixedDeltaTime * scaleSpeed);

        if (newScale.magnitude < 0.1f) return;

        transform.localScale = newScale;
        float ratio = newScale.magnitude / lastScale.magnitude;

        foreach (var dj in distanceJoints2D)
        {
            dj.autoConfigureDistance = false;
            dj.distance *= ratio;
        }

        foreach (var rj in relativesJoints2D)
        {
            rj.autoConfigureOffset = false;
            rj.linearOffset *= ratio;
        }
    }

    private void ApplyScaleInstant()
    {
        Vector3 lastScale = transform.localScale;
        transform.localScale = targetScale;

        float ratio = targetScale.magnitude / lastScale.magnitude;

        foreach (var dj in distanceJoints2D)
        {
            dj.autoConfigureDistance = false;
            dj.distance *= ratio;
        }

        foreach (var rj in relativesJoints2D)
        {
            rj.autoConfigureOffset = false;
            rj.linearOffset *= ratio;
        }
    }
}
