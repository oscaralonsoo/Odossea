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
        public Transform transform;
        [NonSerialized] public Rigidbody2D rigidbody2D;
        [NonSerialized] public Collider2D collider2D;
        [NonSerialized] public List<DistanceJoint2D> distanceJoints = new();
        [NonSerialized] public List<RelativeJoint2D> relativeJoints = new();
        [NonSerialized] public bool grounded = false;
    }

    [SerializeField] List<Bone> bones;
    
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float force;
    [SerializeField] float downForce;
    [SerializeField] float jumpForce;
    [SerializeField] float maxXSpeed;
    [SerializeField] float maxYSpeed;
    [SerializeField] float scaleAmount;
    [SerializeField] float scaleSpeed;

    Vector3 targetScale = Vector3.one;
    RelativeJoint2D[] relativesJoints2D;
    DistanceJoint2D[] distanceJoints2D;

    public int growNum = 1;
    [NonSerialized] public bool collided;

    private void Awake()
    {
        foreach (Bone b in bones)
        {
            b.transform.TryGetComponent(out b.rigidbody2D);
            b.transform.TryGetComponent(out b.collider2D);
            b.distanceJoints.AddRange(b.transform.GetComponents<DistanceJoint2D>());
            b.relativeJoints.AddRange(b.transform.GetComponents<RelativeJoint2D>());
        }
        targetScale = bones[0].transform.localScale;
        collided = false;
        growNum = 1;
    }

    public void Grow(int growAmount)
    {
        for (int i = 0; i < growAmount; i++)
        {
            targetScale += Vector3.one * scaleAmount;
            growNum++;
        }
    }

    public void Shrink(int shrinkNumber, bool limitMinSize = true)
    {
        for (int i = 0; i < shrinkNumber; i++)
        {
            if (growNum <= 1 && limitMinSize) return;
            targetScale -= Vector3.one * scaleAmount;
            growNum--;
        }
    }

    private void FixedUpdate()
    {
        ApplyScale();

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

    private void ApplyScale()
    {
        Vector2 lastScale = bones[0].transform.localScale; ;
        Vector2 newScale = Vector3.Lerp(lastScale, targetScale, Time.fixedDeltaTime * scaleSpeed);

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

    public void Collided()
    {
        collided = true;
        Destroy(transform.gameObject);
    }
}