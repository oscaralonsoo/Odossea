using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DropableCube : MonoBehaviour
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
    [SerializeField] float maxXSpeed;
    [SerializeField] float maxYSpeed;
    [SerializeField] float scaleFactor;
    [SerializeField] float scaleSpeed;

    Vector3 targetScale = Vector3.one;
    RelativeJoint2D[] relativesJoints2D;
    DistanceJoint2D[] distanceJoints2D;

    public int growNum;

    private void Awake()
    {
        relativesJoints2D = transform.GetComponentsInChildren<RelativeJoint2D>();
        distanceJoints2D = transform.GetComponentsInChildren<DistanceJoint2D>();
        targetScale = transform.localScale;
        growNum = 1;
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
        Vector3 lastScale = transform.localScale;
        Vector3 newScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * scaleSpeed);

        if (newScale.magnitude > 0.1f)
        {
            transform.localScale = newScale;
            float changeRatio = newScale.magnitude / lastScale.magnitude;

            foreach (DistanceJoint2D dj in distanceJoints2D)
            {
                dj.autoConfigureDistance = false;
                dj.distance *= changeRatio;
            }

            foreach (RelativeJoint2D rj in relativesJoints2D)
            {
                rj.autoConfigureOffset = false;
                rj.linearOffset *= changeRatio;
            }
        }

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
    }
}