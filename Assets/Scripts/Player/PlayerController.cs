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
        [NonSerialized] public Vector2 lastPostion = Vector2.zero;
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
    public bool isDead = false;

    [Header("Scaling")]
    [SerializeField] float scaleAmout = 0.2f;
    [SerializeField] int initialGrowNum = 3;
    [SerializeField] int maxGrowNum = 5;
    public int growNum = 1;
    int lastGrowNum = 1;
    float currentScaleSpeed;
    const float defaultScaleSpeed = 5f;
    private Vector2 targetScale;
    private Vector2 startScale;

    public float AverageVerticalVelocity { get; private set; }

    // Input
    private Vector2 inputDirection;
    private bool jumpRequested;
    private bool crouchRequested;
    private bool shootRequested;
    private Vector2 aimDirection = Vector2.right;

    [Header("Camera")]
    [SerializeField] Camera cinemachineCamera;

    private ContactFilter2D groundFilter;

    private void Awake()
    {
        cinemachineCamera = Camera.main;

        foreach (Bone b in bones)
        {
            b.transform.TryGetComponent(out b.rigidbody2D);
            b.transform.TryGetComponent(out b.collider2D);
            b.distanceJoints.AddRange(b.transform.GetComponents<DistanceJoint2D>());
            b.relativeJoints.AddRange(b.transform.GetComponents<RelativeJoint2D>());
        }

        InitScale();

        foreach (Bone b in bones)
        {
            b.lastPostion = b.transform.position;
        }
        lastGrowNum = growNum;

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

        Vector2 dir = mouseWorldPos;
        dir -= GetCenterPosition();

        if (dir.sqrMagnitude > 0.001f)
            aimDirection = dir.normalized;
    }

    private void FixedUpdate()
    {
        ApplyScale();

        if (isDead) return;

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
        startScale = (Vector2)bones[0].transform.localScale;
        targetScale = startScale;
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
            targetScale += Vector2.one * scaleAmout;
            growNum++;
        }
    }

    public void Shrink(int shrinkNumber, float scaleSpeed = defaultScaleSpeed)
    {
        currentScaleSpeed = scaleSpeed;
        for (int i = 0; i < shrinkNumber; i++)
        {
            if (growNum <= 1) return;
            targetScale -= Vector2.one * scaleAmout;
            growNum--;
        }
    }

    private void ApplyScale()
    {
        Vector2 lastScale = (Vector2)bones[0].transform.localScale;
        Vector2 newScale = Vector2.Lerp(lastScale, targetScale, Time.fixedDeltaTime * currentScaleSpeed);
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
        Vector2 lastScale = (Vector2)bones[0].transform.localScale;
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
        if (!isDead) StartCoroutine(Respawn());
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

    public void OnCheckPoint()
    {
        foreach (Bone b in bones)
        {
            b.lastPostion = b.transform.position;
        }
        lastGrowNum = growNum;
    }

    public IEnumerator Respawn()
    {
        GetComponent<MassCubeInstantiator>()
            .DisperseCubes(growNum, 0.3f, GetCenterPosition(), 13f);

        targetScale.x = 0.01f;
        targetScale.y = 0.01f;
        isDead = true;

        foreach (Bone bone in bones)
        {
            bone.rigidbody2D.linearVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(0.4f);

        foreach (Bone bone in bones)
        {
            bone.transform.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(2f);

        targetScale = startScale;
        growNum = 1;
        GrowToNum(lastGrowNum);
        ApplyScaleInstant();

        foreach (Bone bone in bones)
        {
            bone.transform.position = bone.lastPostion;
            bone.transform.gameObject.SetActive(true);
        }

        isDead = false;
    }

    public Vector2 GetCenterPosition()
    {
        Vector2 center = Vector2.zero;
        foreach (Bone bone in bones)
        {
            center += (Vector2)bone.transform.position;
        }
        return center/=bones.Count;
    }

    public void ApplyForce(Vector2 force)
    {
        foreach (Bone bone in bones) 
        {
            Rigidbody2D childRb = bone.rigidbody2D;
            if (childRb != null)
            {
                childRb.AddForce(force, ForceMode2D.Impulse);
            }
        }
    }

    public Vector2 GetCenterLastPosition()
    {
        Vector2 center = Vector2.zero;
        foreach (Bone bone in bones)
        {
            center += (Vector2)bone.lastPostion;
        }
        return center /= bones.Count;
    }
}
