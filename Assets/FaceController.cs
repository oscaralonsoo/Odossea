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

        // ---- 1️⃣ Calcular posición promedio de los huesos ----
        Vector3 avgPos = Vector3.zero;
        foreach (var bone in bones)
        {
            avgPos += (Vector3)bone.position;
        }
        avgPos /= bones.Count;

        // ---- 2️⃣ Aplicar posición al Face con offset ----
        Vector3 targetPos = avgPos + localOffset;

        // La cara sigue suavemente pero sin rotación
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // Fijar rotación a cero (opcional, por si alguna transformación previa la giró)
        transform.rotation = Quaternion.identity;
    }
}
