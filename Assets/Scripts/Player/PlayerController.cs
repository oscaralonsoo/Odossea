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
        public CircleCollider2D collider2D;
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

    [Header("Scaling")]
    [SerializeField] float scaleAmout = 0.2f;
    [SerializeField] float scaleSpeed = 5f;
    [SerializeField] int initialGrowNum = 0;
    public int growNum = 1;

    // Animator / estado
    public bool IsGrounded { get; private set; }
    public float AverageVerticalVelocity { get; private set; }

    // Input
    private Vector2 inputDirection;
    private bool jumpRequested;
    private Vector2 lookInput;
    private bool shootRequested;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 lastMousePosition;

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
    public void OnLook(InputValue value)
    {
        Vector2 stick = value.Get<Vector2>();

        if (stick.sqrMagnitude > 0.01f)
        {
            aimDirection = stick.normalized;
        }
    }

    public void OnShoot(InputValue value)
    {
        if (value.isPressed)
            shootRequested = true;
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
            Vector3 nextScale = targetScale;
            nextScale.x += scaleAmout;
            nextScale.y += scaleAmout;
            nextScale.z += scaleAmout;
            targetScale = nextScale;
            growNum++;
        }
    }

    public void Shrink(int shrinkNumber, bool limitMinSize = true)
    {
        for (int i = 0; i < shrinkNumber; i++)
        {
            if (growNum <= 1 && limitMinSize)
                return;

            Vector3 nextScale = targetScale;
            nextScale.x -= scaleAmout;
            nextScale.y -= scaleAmout;
            nextScale.z -= scaleAmout;
            targetScale = nextScale;
            growNum--;
        }
    }
    private void Update()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos != lastMousePosition)
        {
            lastMousePosition = mousePos;

            Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
            world.z = 0f;

            Vector2 dir = (world - transform.position);
            if (dir.sqrMagnitude > 0.001f)
                aimDirection = dir.normalized;
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
            {
                Vector2 v = rb.linearVelocity;
                v.y = 0;
                rb.linearVelocity = v;
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
                
            Vector2 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(vel.x, -maxXSpeed, maxXSpeed);
            rb.linearVelocity = vel;
        }

        AverageVerticalVelocity = verticalVelocitySum / bones.Count;
        jumpRequested = false;

        if (shootRequested)
        {
            if (aimDirection != Vector2.zero)
            {
                GetComponent<MassCubeInstantiator>().Shoot(aimDirection);
            }
            shootRequested = false;
        }
    }

    // ---------- SCALE ----------
    private void InitScale()
    {
        targetScale = transform.localScale;
        Grow(initialGrowNum);

        ApplyScaleInstant();
    }

    private void ApplyScale()
    {
        Vector3 lastScale = transform.localScale;
        Vector3 newScale = Vector3.Lerp(lastScale, targetScale, Time.fixedDeltaTime * scaleSpeed);

        if (newScale.magnitude < 0.1f) return;

        float ratio = newScale.magnitude / lastScale.magnitude;

        foreach (Bone bone in bones)
        {
            bone.rigidbody2D.transform.localScale *= ratio;

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

        float ratio = targetScale.magnitude / lastScale.magnitude;

        foreach (Bone bone in bones)
        {
            bone.rigidbody2D.transform.localScale *= ratio;

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


