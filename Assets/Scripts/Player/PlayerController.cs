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
        public Rigidbody2D rigidbody2D;
        public CircleCollider2D collider2D;
        public DistanceJoint2D distanceJoint;
        public List<RelativeJoint2D> relativeJoints = new List<RelativeJoint2D>();
        public bool grounded = false;
    }

    [Header("Fusion")]
    public PlayerController otherPlayer;
    public GameObject rope;
    public bool IsCrouched = false;
    private bool fusioned = false;

    [Header("Bones")]
    public List<Bone> bones;

    [Header("Movement")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force = 50f;
    [SerializeField] float downForce = 20f;
    [SerializeField] float jumpForce = 300f;
    [SerializeField] float maxCrouchSpeed = 0.1f;
    [SerializeField] float maxGroundSpeed = 10f;
    [SerializeField] float maxAirSpeed = 5f;

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
    private bool crouchRequested;
    private bool shootRequested;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 lastMousePosition;


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

    public void OnCrouch(InputValue value)
    {
        if (value.isPressed)
            crouchRequested = true;
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

    public void Fusion(bool fusion)
    {
        if (otherPlayer == null) return;
        
        if (fusion)
        {
            Bone centerBonePlayer = bones[bones.Count - 1];

            foreach (Bone bone in otherPlayer.bones)
            {
                DistanceJoint2D joint = centerBonePlayer.rigidbody2D.gameObject.AddComponent<DistanceJoint2D>();
                joint.connectedBody = bone.rigidbody2D;
                joint.autoConfigureDistance = false;
                joint.maxDistanceOnly = true;
                joint.distance = 10f;
            }
        }
        else
        {
            Bone centerBonePlayer = bones[bones.Count - 1];

            foreach (DistanceJoint2D joint in centerBonePlayer.rigidbody2D.gameObject.GetComponents<DistanceJoint2D>())
            {
                Destroy(joint);
            }
        }

        rope.SetActive(fusion);
        fusioned = fusion;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) {

            Fusion(!fusioned);
        }

        if (Input.GetKey(KeyCode.S))
        {
            crouchRequested = true;
        }

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
        IsCrouched = groundedCount > 1 && crouchRequested;

        float verticalVelocitySum = 0f;

        foreach (Bone bone in bones)
        {
            Rigidbody2D rb = bone.rigidbody2D;

            if (IsCrouched)
            {
                rb.AddForce(Vector2.down * downForce);
                if (moveX != 0)
                    rb.AddForce(Vector2.right * moveX * force);

                float velX = rb.linearVelocity.x;
                velX = Mathf.Clamp(velX, -maxCrouchSpeed, maxCrouchSpeed);
                rb.linearVelocityX = velX;
            }
            else
            {
                if (bone.grounded && groundedCount < 3)
                    rb.AddForce(Vector2.down * downForce);

                if (moveX != 0)
                {
                    if (bone.grounded)
                        rb.AddForce(Vector2.right * moveX * force);
                    else if (groundedCount == 0)
                    {
                        if (fusioned && otherPlayer.IsCrouched)
                        {
                            Vector2 myCenter = GetCenterPosition();
                            Vector2 otherCenter = otherPlayer.GetCenterPosition();

                            // 2. Vector dirección y su perpendicular
                            Vector2 lookAt = otherCenter - myCenter;

                            // Normalizamos para que la distancia no afecte a la potencia de la fuerza
                            Vector2 perpRight = new Vector2(lookAt.y, -lookAt.x).normalized;

                            // 3. Aplicamos la fuerza orbital a cada hueso
                            rb.AddForce(perpRight.normalized * moveX * force);
                            Debug.DrawRay(GetCenterPosition(), perpRight.normalized);

                            // 4. Limitamos la velocidad global (Magnitud)
                            if (rb.linearVelocity.magnitude > maxAirSpeed)
                            {
                                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxAirSpeed);
                               
                            }
                        }
                        else
                        {
                            rb.AddForce(Vector2.right * moveX * force);
                        }
                    }
                }
               

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
                vel.x = Mathf.Clamp(vel.x, IsGrounded ? -maxGroundSpeed : -maxAirSpeed, IsGrounded ? maxGroundSpeed : maxAirSpeed);
                rb.linearVelocity = vel;
            }
        }

        AverageVerticalVelocity = verticalVelocitySum / bones.Count;
        
        if (shootRequested)
        {
            if (aimDirection != Vector2.zero)
            {
                GetComponent<MassCubeInstantiator>().Shoot(aimDirection);
            }
        }

        crouchRequested = false;
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


