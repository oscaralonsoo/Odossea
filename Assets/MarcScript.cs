using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcScript : MonoBehaviour
{
    [Serializable] public class Bone
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

    bool jumpRequested = false;
    int horizontalInput = 0;

    private void Update()
    {
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            horizontalInput = -1;
        }

        if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
        {
            horizontalInput = 1;
        }

        if (Input.GetKeyDown(KeyCode.Space))
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
            if(bone.grounded)
            {
                groundedNum++;
            }
        }

        foreach (Bone bone in bones)
        {
            if (bone.grounded && groundedNum < 3) bone.rigidbody2D.AddForce(Vector2.down * downForce);

            if (bone.grounded || groundedNum == 0)
            {
                //HORIZONTAL
                if (horizontalInput < 0)
                {
                    bone.rigidbody2D.AddForce(Vector2.left * force);
                }
                if (horizontalInput > 0)
                {
                    bone.rigidbody2D.AddForce(Vector2.right * force);
                }

                if (Mathf.Abs(bone.rigidbody2D.linearVelocity.x) > maxSpeed)
                {
                    float limitedX = Mathf.Sign(bone.rigidbody2D.linearVelocity.x) * maxSpeed;
                    bone.rigidbody2D.linearVelocity = new Vector2(limitedX, bone.rigidbody2D.linearVelocity.y);
                    
                }
            }

           
            if (groundedNum != 0)
            {
                if (jumpRequested)
                {
                    if (bone.grounded) bone.rigidbody2D.AddForce(Vector2.up * jumpForce);
                    else bone.rigidbody2D.AddForce(Vector2.up * notGroundedJumpForce);
                }
            }
        }

        jumpRequested = false;
        horizontalInput = 0;
    }
}
