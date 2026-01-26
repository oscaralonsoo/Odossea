using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Serializable]
    public class Bone
    {
        public Rigidbody2D rigidbody2D;
        public Collider2D collider2D;
        public DistanceJoint2D distanceJoint;
        public List<RelativeJoint2D> relativeJoints = new List<RelativeJoint2D>();
        public bool grounded = false;
    }

    [Header("Bones")]
    public List<Bone> bones;

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

    // Animator / estado
    public bool IsGrounded { get; private set; }
    public float AverageVerticalVelocity { get; private set; }

    // Input
    private Vector2 inputDirection;
    private bool jumpRequested;

    // Restricciones
    private bool blockLeftMove;
    private bool blockRightMove;

    // Escala
    private Vector3 targetScale;

    private void Awake()
    {
        InitScale();
    }

    // -------- INPUT --------
    public void OnMove(InputValue value)
    {
        inputDirection = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

    // -------- RESTRICCIONES --------
    public void SetMovementRestriction(bool left, bool right)
    {
        blockLeftMove = left;
        blockRightMove = right;
    }

    // -------- GROW --------
    public void Grow(int growAmount)
    {
        for (int i = 0; i < growAmount; i++)
        {
            targetScale *= scaleFactor;
            growNum++;
        }
    }

    public void Shrink(int shrinkNumber, bool limitMinSize = true)
    {
        for (int i = 0; i < shrinkNumber; i++)
        {
            if (growNum <= 1 && limitMinSize)
                return;

            targetScale /= scaleFactor;
            growNum--;
        }
    }

    private void FixedUpdate()
    {
        ApplyScale();

        // ---------- Ground Check ----------
        int groundedCount = 0;
        foreach (Bone bone in bones)
        {
            if (!bone.collider2D) continue;

            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded) groundedCount++;
        }

        IsGrounded = groundedCount > 0;

        // ---------- Input filtrado ----------
        float moveX = inputDirection.x;
        if (blockLeftMove && moveX < 0) moveX = 0;
        if (blockRightMove && moveX > 0) moveX = 0;

        float verticalVelocitySum = 0f;

        foreach (Bone bone in bones)
        {
            Rigidbody2D rb = bone.rigidbody2D;

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
            //rb.angularVelocity = 0;

            verticalVelocitySum += vel.y;
        }

        AverageVerticalVelocity = verticalVelocitySum / bones.Count;
        jumpRequested = false;
    }

    // ---------- SCALE ----------
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

        foreach (Bone bone in bones)
        {
            if (bone.distanceJoint)
            {
                bone.distanceJoint.autoConfigureDistance = false;
                bone.distanceJoint.distance *= ratio;
            }

            foreach (RelativeJoint2D rj in bone.relativeJoints)
            {
                if (!rj) continue;
                rj.autoConfigureOffset = false;
                rj.linearOffset *= ratio;
            }
        }
    }

    private void ApplyScaleInstant()
    {
        Vector3 lastScale = transform.localScale;
        transform.localScale = targetScale;

        float ratio = targetScale.magnitude / lastScale.magnitude;

        foreach (Bone bone in bones)
        {
            if (bone.distanceJoint)
            {
                bone.distanceJoint.autoConfigureDistance = false;
                bone.distanceJoint.distance *= ratio;
            }

            foreach (RelativeJoint2D rj in bone.relativeJoints)
            {
                if (!rj) continue;
                rj.autoConfigureOffset = false;
                rj.linearOffset *= ratio;
            }
        }
    }

    public void Die()
    {
        gameObject.GetComponent<MassCubeInstantiator>().DisperseCubes(growNum, 0.3f, transform.position, 13.0f);
        Shrink(10, false);

        //RespawnAtLastGround();
    }
}
