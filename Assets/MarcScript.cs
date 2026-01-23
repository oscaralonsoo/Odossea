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
        public bool grounded = false;
    }

    [Header("Bones & Joints")]
    [SerializeField] List<Bone> bones;
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

    [Header("Animator (child)")]
    [SerializeField] private Animator faceAnimator; // arrastra aquí tu Face Animator o se buscará automáticamente

    private Vector3 targetScale = Vector3.one;
    private Vector2 inputDirection;
    private bool jumpRequested = false;

    private bool blockLeftMove = false;
    private bool blockRightMove = false;

    private void Awake()
    {
        relativesJoints2D = transform.GetComponentsInChildren<RelativeJoint2D>();
        distanceJoints2D = transform.GetComponentsInChildren<DistanceJoint2D>();

        if (faceAnimator == null)
            faceAnimator = GetComponentInChildren<Animator>();

        
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
            targetScale = targetScale * scaleFactor;
            growNum++;
        }
    }

    private void FixedUpdate()
    {
        // ------------------- 1️⃣ Escala -------------------
        ApplyScale();

        // ------------------- 2️⃣ Detección de suelo global -------------------
        int groundedNum = 0;
        foreach (Bone bone in bones)
        {
            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded) groundedNum++;
        }

        // ------------------- 3️⃣ FILTRO DE INPUT GLOBAL -------------------
        float finalMoveX = inputDirection.x;
        if (blockLeftMove && finalMoveX < 0) finalMoveX = 0;
        if (blockRightMove && finalMoveX > 0) finalMoveX = 0;

        // ------------------- 4️⃣ Aplicar fuerzas a cada hueso -------------------
        foreach (Bone bone in bones)
        {
            Rigidbody2D rb = bone.rigidbody2D;

            // Gravedad extra si no hay suficientes puntos de apoyo
            if (bone.grounded && groundedNum < 3)
                rb.AddForce(Vector2.down * downForce);

            // Movimiento Horizontal (input filtrado)
            if ((bone.grounded || groundedNum == 0) && finalMoveX != 0)
            {
                rb.AddForce(Vector2.right * finalMoveX * force);
            }

            // Salto
            if (groundedNum != 0 && jumpRequested)
            {
                rb.AddForce(Vector2.up * jumpForce);
            }

            // Limitador de velocidad
            Vector2 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(vel.x, -maxXSpeed, maxXSpeed);
            vel.y = Mathf.Clamp(vel.y, -maxYSpeed, maxYSpeed);
            rb.linearVelocity = vel;
        }

        jumpRequested = false;

        // ------------------- 5️⃣ Actualizar Animator -------------------
        if (faceAnimator != null)
        {
            // Estado global de suelo
            bool isGroundedGlobal = groundedNum > 0;

            // Velocidad vertical promedio
            float avgVerticalVelocity = 0f;
            foreach (var bone in bones)
                avgVerticalVelocity += bone.rigidbody2D.linearVelocity.y;
            avgVerticalVelocity /= bones.Count;

            // Suavizado opcional de la velocidad vertical para animaciones más fluidas
            float smoothVelocity = Mathf.Lerp(faceAnimator.GetFloat("VerticalVelocity"), avgVerticalVelocity, 0.1f);

            faceAnimator.SetBool("isGrounded", isGroundedGlobal);
            faceAnimator.SetFloat("VerticalVelocity", smoothVelocity);
        }
    }

    private void InitScale()
    {
        targetScale = transform.localScale;
        for (int i = 1; i < growNum; i++)
        {
            targetScale = targetScale * scaleFactor;
        }

        Vector3 lastScale = transform.localScale;
        Vector3 newScale = targetScale;
        transform.localScale = newScale;
        float changeRatio = newScale.magnitude / lastScale.magnitude;
        foreach (DistanceJoint2D dj in distanceJoints2D) { dj.autoConfigureDistance = false; dj.distance *= changeRatio; }
        foreach (RelativeJoint2D rj in relativesJoints2D) { rj.autoConfigureOffset = false; rj.linearOffset *= changeRatio; }
    }

    private void ApplyScale()
    {
        Vector3 lastScale = transform.localScale;
        Vector3 newScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * scaleSpeed);

        if (newScale.magnitude > 0.1f)
        {
            transform.localScale = newScale;
            float changeRatio = newScale.magnitude / lastScale.magnitude;
            foreach (DistanceJoint2D dj in distanceJoints2D) { dj.autoConfigureDistance = false; dj.distance *= changeRatio; }
            foreach (RelativeJoint2D rj in relativesJoints2D) { rj.autoConfigureOffset = false; rj.linearOffset *= changeRatio; }
        }
    }
}
