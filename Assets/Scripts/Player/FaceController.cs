using UnityEngine;
using System.Collections.Generic;

public class FaceController : MonoBehaviour
{
    [Header("Player Bones")]
    public List<Rigidbody2D> bones;

    [Header("Offset de la cara")]
    public Vector3 localOffset = new Vector3(0, 0.5f, 0);

    [Header("Smooth speed")]
    public float smoothSpeed = 100f;

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
