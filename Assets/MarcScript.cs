using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema

public class MarcScript : MonoBehaviour
{
    [Serializable]
    public class Bone
    {
        public Rigidbody2D rigidbody2D;
        public Collider2D collider2D;
        public bool grounded = false;
    }

    [SerializeField] List<Bone> bones;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force;
    [SerializeField] float downForce;
    [SerializeField] float jumpForce;
    [SerializeField] float notGroundedJumpForce;
    [SerializeField] float maxSpeed;

    private Vector2 inputDirection;
    private bool jumpRequested = false;

    // Métodos que llamará el PlayerInput por "Broadcast Messages" o "Player Input Events"
    public void OnMove(InputValue value)
    {
        inputDirection = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        int groundedNum = 0;
        foreach (Bone bone in bones)
        {
            bone.grounded = bone.collider2D.IsTouchingLayers(groundLayer);
            if (bone.grounded) groundedNum++;
        }

        foreach (Bone bone in bones)
        {
            if (bone.grounded && groundedNum < 3) bone.rigidbody2D.AddForce(Vector2.down * downForce);

            if (bone.grounded || groundedNum == 0)
            {
                // Movimiento horizontal usando la variable inputDirection
                if (inputDirection.x != 0)
                {
                    bone.rigidbody2D.AddForce(Vector2.right * inputDirection.x * force);
                }

                if (Mathf.Abs(bone.rigidbody2D.linearVelocity.x) > maxSpeed)
                {
                    float limitedX = Mathf.Sign(bone.rigidbody2D.linearVelocity.x) * maxSpeed;
                    bone.rigidbody2D.linearVelocity = new Vector2(limitedX, bone.rigidbody2D.linearVelocity.y);
                }
            }

            if (groundedNum != 0 && jumpRequested)
            {
                if (bone.grounded) bone.rigidbody2D.AddForce(Vector2.up * jumpForce);
                else bone.rigidbody2D.AddForce(Vector2.up * notGroundedJumpForce);
            }
        }

        jumpRequested = false;
    }
}