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
        if (bones == null || bones.Count == 0) return;

        // Posición promedio de los huesos
        Vector3 avgPos = Vector3.zero;

        foreach (var bone in bones)
        {
            avgPos += (Vector3)bone.position;
        }

        avgPos /= bones.Count;

        // Offset base
        Vector3 targetPos = avgPos + localOffset;

        // Suavizado
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );

        // Bloquear rotación
        transform.rotation = Quaternion.identity;
    }
}
