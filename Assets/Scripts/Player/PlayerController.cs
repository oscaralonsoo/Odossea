using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Serializable]
    public class Bone
    {
        public Transform transform;
        [NonSerialized] public Rigidbody2D rigidbody2D = null;
        [NonSerialized] public CircleCollider2D collider2D = null;
        [NonSerialized] public List<DistanceJoint2D> distanceJoints = new();
        [NonSerialized] public List<RelativeJoint2D> relativeJoints = new();
        [NonSerialized] public bool grounded = false;
    }

    [Header("Bones")]
    public List<Bone> bones;

    [Header("Movement")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force = 50f;
    [SerializeField] float downForce = 20f;
    [SerializeField] float jumpForce = 300f;
    [SerializeField] float maxGroundSpeed = 10f;
    [SerializeField] float maxAirSpeed = 5f;
    public bool isGrounded = false;

    [Header("Scaling")]
    [SerializeField] float scaleAmout = 0.2f;
    [SerializeField] int initialGrowNum = 3;
    [SerializeField] int maxGrowNum = 5;
    public int growNum = 1;
    float currentScaleSpeed;
    const float defaultScaleSpeed = 5f;

    public float AverageVerticalVelocity { get; private set; }

    // Input
    private Vector2 inputDirection;
    private bool jumpRequested;
    private bool crouchRequested;
    private bool shootRequested;
    private Vector2 aimDirection = Vector2.right;

    [Header("Camera")]
    [SerializeField] Camera cinemachineCamera;

    // Escala
    private Vector3 targetScale;
    private ContactFilter2D groundFilter;

    private void Awake()
    {
        foreach (Bone b in bones)
        {
            b.transform.TryGetComponent(out b.rigidbody2D);
            b.transform.TryGetComponent(out b.collider2D);
            b.distanceJoints.AddRange(b.transform.GetComponents<DistanceJoint2D>());
            b.relativeJoints.AddRange(b.transform.GetComponents<RelativeJoint2D>());
        }

        InitScale();

        groundFilter.useLayerMask = true;
        groundFilter.SetLayerMask(groundLayer);
    }

    // -------- INPUT --------
    public void OnMove(InputValue value) => inputDirection = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

    public void OnCrouch(InputValue value) => crouchRequested = value.isPressed;

    public void OnLook(InputValue value)
    {
        Vector2 stick = value.Get<Vector2>();
        if (stick.sqrMagnitude > 0.01f)
            aimDirection = stick.normalized;
    }

    public void OnShoot(InputValue value)
    {
        if (value.isPressed)
            shootRequested = true;
    }

    // -------- AIM (Mouse + Cinemachine) --------
    private void Update()
    {
        if (Mouse.current == null || cinemachineCamera == null)
            return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();

        // Distancia desde la cámara al plano Z = 0
        mouseScreenPos.z = -cinemachineCamera.transform.position.z;

        Vector3 mouseWorldPos = cinemachineCamera.ScreenToWorldPoint(mouseScreenPos);

        Vector2 dir = (mouseWorldPos - transform.position);
        if (dir.sqrMagnitude > 0.001f)
            aimDirection = dir.normalized;
    }

    private void FixedUpdate()
    {
        ApplyScale();

        int groundedCount = 0;
        foreach (Bone bone in bones)
        {
            if (!bone.collider2D) continue;
            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded) groundedCount++;
        }

        isGrounded = groundedCount > 0;
        ContactPoint2D[] contacts = new ContactPoint2D[1];

        foreach (Bone bone in bones)
        {
            Rigidbody2D rb = bone.rigidbody2D;

            if (bone.grounded && groundedCount < 3)
            {
                int numContacts = bone.collider2D.GetContacts(groundFilter, contacts);
                if (numContacts > 0 && contacts[0].normal.y > 0.99f)
                {
                    rb.AddForce(-contacts[0].normal * downForce);
                }
            }

            if ((bone.grounded || groundedCount == 0) && inputDirection.x != 0)
                rb.AddForce(Vector2.right * inputDirection.x * force);

            if (isGrounded && jumpRequested)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }

            Vector2 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(
                vel.x,
                isGrounded ? -maxGroundSpeed : -maxAirSpeed,
                isGrounded ? maxGroundSpeed : maxAirSpeed
            );
            rb.linearVelocity = vel;
        }

        if (shootRequested && aimDirection != Vector2.zero)
        {
            if (growNum > 1)
            {
                Shrink(1);
                GetComponent<MassCubeInstantiator>().Shoot(aimDirection);
            }
        }

        jumpRequested = false;
        shootRequested = false;
    }

    // -------- SCALE --------
    private void InitScale()
    {
        targetScale = transform.localScale;
        GrowToNum(initialGrowNum);
        ApplyScaleInstant();
    }

    public void GrowToNum(int growNum, float scaleSpeed = defaultScaleSpeed)
    {
        Mathf.Clamp(growNum, 1, maxGrowNum);
        if (this.growNum > growNum)
        {
            while (this.growNum > growNum)
            {
                Shrink(1, scaleSpeed);
            }
        }
        else if (this.growNum < growNum) 
        {
            while (this.growNum < growNum)
            {
                Grow(1, scaleSpeed);
            }
        }
    }

    public void Grow(int growAmount, float scaleSpeed = defaultScaleSpeed)
    {
        currentScaleSpeed = scaleSpeed;
        for (int i = 0; i < growAmount; i++)
        {
            if (growNum >= maxGrowNum) return;
            targetScale += Vector3.one * scaleAmout;
            growNum++;
        }
    }

    public void Shrink(int shrinkNumber, float scaleSpeed = defaultScaleSpeed)
    {
        currentScaleSpeed = scaleSpeed;
        for (int i = 0; i < shrinkNumber; i++)
        {
            if (growNum <= 1) return;
            targetScale -= Vector3.one * scaleAmout;
            growNum--;
        }
    }

    private void ApplyScale()
    {
        Vector3 lastScale = transform.localScale;
        Vector3 newScale = Vector3.Lerp(lastScale, targetScale, Time.fixedDeltaTime * currentScaleSpeed);
        if (newScale.magnitude < 0.1f) return;

        float ratio = newScale.magnitude / lastScale.magnitude;

        foreach (Bone bone in bones)
        {
            bone.rigidbody2D.transform.localScale *= ratio;

            foreach (var dj in bone.distanceJoints)
            {
                dj.autoConfigureDistance = false;
                dj.distance *= ratio;
            }

            foreach (var rj in bone.relativeJoints)
            {
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

            foreach (var dj in bone.distanceJoints)
            {
                dj.autoConfigureDistance = false;
                dj.distance *= ratio;
            }

            foreach (var rj in bone.relativeJoints)
            {
                rj.autoConfigureOffset = false;
                rj.linearOffset *= ratio;
            }
        }
    }

    public void Die()
    {
        GetComponent<MassCubeInstantiator>()
            .DisperseCubes(growNum, 0.3f, transform.position, 13f);

        //Shrink(10);
    }

    public IEnumerator PausedGrowToNum(int growNum, float scaleSpeed = defaultScaleSpeed, float pauseTime = 0.3f)
    {
        Mathf.Clamp(growNum, 1, maxGrowNum);
        if (this.growNum > growNum)
        {
            while (this.growNum > growNum)
            {
                yield return new WaitForSeconds(pauseTime);
                Shrink(1, scaleSpeed);
            }
        }
        else if (this.growNum < growNum)
        {
            while (this.growNum < growNum)
            {
                yield return new WaitForSeconds(pauseTime);
                Grow(1, scaleSpeed);
            }
        }
    }
}
