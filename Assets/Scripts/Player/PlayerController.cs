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
        [HideInInspector] public bool grounded;
    }

    [Header("Bones & Joints")]
    [SerializeField] public List<Bone> bones;

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

    private Vector3 lastGroundedPosition;

    private void Awake()
    {
        InitScale();
    }

    public void OnMove(InputValue value) => inputDirection = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

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

        int groundedCount = 0;
        foreach (var bone in bones)
        {
            if (bone.collider2D == null)
            {
                bone.grounded = false;
                continue;
            }

            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded)
                groundedCount++;
        }

        IsGrounded = groundedCount > 0;

        if (groundedCount > 0)
            lastGroundedPosition = transform.position;

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
        Vector3 newScale = Vector3.Lerp(
            lastScale,
            targetScale,
            Time.fixedDeltaTime * scaleSpeed
        );

        if (newScale.magnitude < 0.1f)
            return;

        transform.localScale = newScale;
        float ratio = newScale.magnitude / lastScale.magnitude;

        foreach (var bone in bones)
        {
            if (bone.distanceJoint != null)
            {
                bone.distanceJoint.autoConfigureDistance = false;
                bone.distanceJoint.distance *= ratio;
            }

            foreach (var rj in bone.relativeJoints)
            {
                if (rj == null) continue;
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

        foreach (var bone in bones)
        {
            if (bone.distanceJoint != null)
            {
                bone.distanceJoint.autoConfigureDistance = false;
                bone.distanceJoint.distance *= ratio;
            }

            foreach (var rj in bone.relativeJoints)
            {
                if (rj == null) continue;
                rj.autoConfigureOffset = false;
                rj.linearOffset *= ratio;
            }
        }
    }

    public void Die()
    {
        gameObject.GetComponent<MassCubeInstantiator>().DisperseCubes(growNum, 0.3f, transform.position, 13.0f);
        Shrink(10, false);

        RespawnAtLastGround();
    }

    public void RespawnAtLastGround()
    {
        transform.position = lastGroundedPosition;

        foreach (var bone in bones)
        {
            if (bone.rigidbody2D != null)
                bone.rigidbody2D.linearVelocity = Vector2.zero;
        }
    }
}
