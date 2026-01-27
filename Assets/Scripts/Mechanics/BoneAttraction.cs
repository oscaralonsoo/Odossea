//using UnityEngine;
//using UnityEngine.InputSystem;
//using System.Collections.Generic;

//public class BoneAttraction : MonoBehaviour
//{
//    [Header("Players")]
//    [SerializeField] PlayerController player;
//    [SerializeField] PlayerController otherPlayer;

//    [Header("Attraction")]
//    [SerializeField] float boneAttractionSpeed = 10f;

//    [Header("Relative Joint Settings")]
//    [SerializeField] float relativeJointMaxForce = 130f;

//    private bool attractionEnabled = false;

//    private Dictionary<PlayerController.Bone, float> originalDistances = new Dictionary<PlayerController.Bone, float>();
//    private Dictionary<RelativeJoint2D, float> originalRelativeJointForces = new Dictionary<RelativeJoint2D, float>();

//    private void FixedUpdate()
//    {
//        if (player == null || otherPlayer == null) return;

//        int groundedCount = 0;
//        foreach (PlayerController.Bone bone in player.bones)
//        {
//            if (bone.grounded) groundedCount++;
//        }

//        if (Keyboard.current.lKey.isPressed && groundedCount == 2 || attractionEnabled && Keyboard.current.lKey.isPressed)
//        {
//            attractionEnabled = true;

//            foreach (PlayerController.Bone bone in player.bones)
//            {
//                if (bone.grounded)
//                {
//                    ApplyAttach(bone);
//                }
//                else
//                {
//                    ApplyVelocityAttraction(bone);
//                }
//            }

//            AdjustDistanceJoints();
//            SetRelativeJointMaxForce(relativeJointMaxForce);
//        }
//        else
//        {
//            ResetBonesToDynamic();
//            RestoreDistanceJoints();
//            RestoreRelativeJointMaxForce();
//        }

//        if (!Keyboard.current.lKey.isPressed)
//            attractionEnabled = false;
//    }

//    #region Bone Movement

//    private void ApplyVelocityAttraction(PlayerController.Bone bone)
//    {
//        PlayerController.Bone closestBone = null;
//        float minDistance = float.MaxValue;

//        foreach (PlayerController.Bone otherBone in otherPlayer.bones)
//        {
//            float dist = Vector2.Distance(bone.rigidbody2D.position, otherBone.rigidbody2D.position);
//            if (dist < minDistance)
//            {
//                minDistance = dist;
//                closestBone = otherBone;
//            }
//        }

//        if (closestBone != null)
//        {
//            Vector2 direction = (closestBone.rigidbody2D.position - bone.rigidbody2D.position).normalized;
//            bone.rigidbody2D.linearVelocity = direction * boneAttractionSpeed;
//        }
//    }

//    private void ApplyAttach(PlayerController.Bone bone)
//    {
//        if (bone.rigidbody2D != null)
//        {
//            bone.rigidbody2D.bodyType = RigidbodyType2D.Static;
//        }
//    }

//    private void ResetBonesToDynamic()
//    {
//        foreach (PlayerController.Bone bone in player.bones)
//        {
//            if (bone.rigidbody2D != null && bone.rigidbody2D.bodyType == RigidbodyType2D.Static)
//            {
//                bone.rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
//                bone.rigidbody2D.linearVelocity = Vector2.zero;
//                bone.rigidbody2D.angularVelocity = 0f;
//            }
//        }
//    }

//    #endregion

//    #region DistanceJoint2D

//    private void AdjustDistanceJoints()
//    {
//        foreach (PlayerController.Bone bone in player.bones)
//        {
//            if (!bone.grounded && bone.distanceJoint != null)
//            {
//                if (!originalDistances.ContainsKey(bone))
//                {
//                    originalDistances[bone] = bone.distanceJoint.distance;
//                }

//                bone.distanceJoint.distance = originalDistances[bone] * 0.5f;
//            }
//        }
//    }

//    private void RestoreDistanceJoints()
//    {
//        foreach (var kvp in originalDistances)
//        {
//            if (kvp.Key.distanceJoint != null)
//            {
//                kvp.Key.distanceJoint.distance = kvp.Value;
//            }
//        }

//        originalDistances.Clear();
//    }

//    #endregion

//    #region RelativeJoint2D

//    private void SetRelativeJointMaxForce(float maxForce)
//    {
//        foreach (PlayerController.Bone bone in player.bones)
//        {
//            if (bone.relativeJoints != null)
//            {
//                foreach (RelativeJoint2D joint in bone.relativeJoints)
//                {
//                    if (!originalRelativeJointForces.ContainsKey(joint))
//                        originalRelativeJointForces[joint] = joint.maxForce;

//                    joint.maxForce = maxForce;
//                }
//            }
//        }
//    }

//    private void RestoreRelativeJointMaxForce()
//    {
//        foreach (var kvp in originalRelativeJointForces)
//        {
//            if (kvp.Key != null)
//            {
//                kvp.Key.maxForce = kvp.Value;
//            }
//        }

//        originalRelativeJointForces.Clear();
//    }

//    #endregion
//}
