using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerController : MonoBehaviour
{
    [Serializable]
    public class Bone
    {
        public Transform transform;
        [NonSerialized] public Rigidbody2D rigidbody2D = null;
        [NonSerialized] public CircleCollider2D collider2D = null;
        [NonSerialized] public List<DistanceJoint2D> distanceJoints = new List<DistanceJoint2D>();
        [NonSerialized] public List<RelativeJoint2D> relativeJoints = new List<RelativeJoint2D>();
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
    [SerializeField] float scaleSpeed = 5f;
    [SerializeField] int initialGrowNum = 0;
    public int growNum = 1;

    // Animator / estado
    public float AverageVerticalVelocity { get; private set; }

    // Input
    private Vector2 inputDirection;
    private bool jumpRequested;
    private bool crouchRequested;
    private bool shootRequested;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 lastMousePosition;


    // Escala
    private Vector3 targetScale;

    private ContactFilter2D groundFilter;

    private void Awake()
    {
        foreach(Bone b in bones)
        {
            b.transform.TryGetComponent<Rigidbody2D>(out b.rigidbody2D);
            b.transform.TryGetComponent<CircleCollider2D>(out b.collider2D);
            foreach (DistanceJoint2D dJoint in b.transform.GetComponents<DistanceJoint2D>()) b.distanceJoints.Add(dJoint);
            foreach (RelativeJoint2D rJoint in b.transform.GetComponents<RelativeJoint2D>()) b.relativeJoints.Add(rJoint);
        }

        InitScale();

        groundFilter.useLayerMask = true;
        groundFilter.SetLayerMask(groundLayer);
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

    public void OnCrouch(InputValue value)
    {
        crouchRequested = value.isPressed;
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

        // ---------- Input filtrado ----------
        float moveX = inputDirection.x;
        float verticalVelocitySum = 0f;

        isGrounded = groundedCount > 0;

        ContactPoint2D[] contacts = new ContactPoint2D[1];

        foreach (Bone bone in bones)
        {
            Rigidbody2D rb = bone.rigidbody2D;

            if (bone.grounded && groundedCount < 3)
            {
                int numContacts = bone.collider2D.GetContacts(groundFilter, contacts);
                if (numContacts > 0)
                {
                    Vector2 groundNormal = contacts[0].normal;

                    if (groundNormal.y > 0.99f)
                    {
                        rb.AddForce(-groundNormal * downForce);
                    }
                }
            }

            if ((bone.grounded || groundedCount == 0) && moveX != 0)
                rb.AddForce(Vector2.right * moveX * force);

            if (isGrounded && jumpRequested)
            {
                Vector2 v = rb.linearVelocity;
                v.y = 0;
                rb.linearVelocity = v;
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }

            Vector2 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(vel.x, isGrounded ? -maxGroundSpeed : -maxAirSpeed, isGrounded ? maxGroundSpeed : maxAirSpeed);
            rb.linearVelocity = vel;
        }

        AverageVerticalVelocity = verticalVelocitySum / bones.Count;
        
        if (shootRequested)
        {
            if (aimDirection != Vector2.zero)
            {
                GetComponent<MassCubeInstantiator>().Shoot(aimDirection);
            }
        }

        jumpRequested = false;
        shootRequested = false;
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

            foreach (DistanceJoint2D dj in bone.distanceJoints)
            {
                if (!dj) continue;
                dj.autoConfigureDistance = false;
                dj.distance *= ratio;
            }

            foreach (RelativeJoint2D rj in bone.relativeJoints)
            {
                if (!rj) continue;
                rj.autoConfigureOffset = false;
                rj.linearOffset *= ratio;
            }
        }
    }

    public Vector2 GetCenterPosition()
    {
        Vector2 center = Vector2.zero;
        if (bones.Count == 0) return (Vector2)transform.position;

        // Sumamos la posición de todos los huesos
        for (int i = 0; i < bones.Count; i++)
        {
            center += (Vector2)bones[i].rigidbody2D.transform.position;
        }

        // Dividimos por el total para sacar el promedio real
        return center / bones.Count;
    }

    private void ApplyScaleInstant()
    {
        Vector3 lastScale = transform.localScale;

        float ratio = targetScale.magnitude / lastScale.magnitude;

        foreach (Bone bone in bones)
        {
            bone.rigidbody2D.transform.localScale *= ratio;

            foreach (DistanceJoint2D dj in bone.distanceJoints)
            {
                if (!dj) continue;
                dj.autoConfigureDistance = false;
                dj.distance *= ratio;
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


