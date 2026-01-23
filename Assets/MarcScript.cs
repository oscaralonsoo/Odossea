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

    [SerializeField] List<Bone> bones;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force;
    [SerializeField] float downForce;
    [SerializeField] float jumpForce;
    [SerializeField] float maxXSpeed = 10;
    [SerializeField] float maxYSpeed =15;

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
            if (bone.grounded && groundedNum < 3)
                bone.rigidbody2D.AddForce(Vector2.down * downForce);

            if (bone.grounded || groundedNum == 0)
            {
                if (inputDirection.x != 0)
                {
                    bone.rigidbody2D.AddForce(Vector2.right * inputDirection.x * force);
                }
            }


            if (groundedNum != 0)
            {
                if (jumpRequested)
                {
                    bone.rigidbody2D.AddForce(Vector2.up * jumpForce);
                }
            }

            if (Mathf.Abs(bone.rigidbody2D.linearVelocity.x) > maxXSpeed)
            {
                float limitedX = Mathf.Sign(bone.rigidbody2D.linearVelocity.x) * maxXSpeed;
                bone.rigidbody2D.linearVelocity = new Vector2(limitedX, bone.rigidbody2D.linearVelocity.y);
            }

            if (Mathf.Abs(bone.rigidbody2D.linearVelocity.y) > maxYSpeed)
            {
                float limitedY = Mathf.Sign(bone.rigidbody2D.linearVelocity.y) * maxYSpeed;
                bone.rigidbody2D.linearVelocity = new Vector2(bone.rigidbody2D.linearVelocity.x, limitedY);
            }
        }

        jumpRequested = false;
    }
}