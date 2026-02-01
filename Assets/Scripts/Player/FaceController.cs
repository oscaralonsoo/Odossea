using UnityEngine;
using System.Collections.Generic;

public class FaceController : MonoBehaviour
{
    public List<Transform> bones;

    private void LateUpdate()
    {
        if (bones == null || bones.Count == 0) return;

        Vector3 center = Vector3.zero;
        int validCount = 0;

        foreach (Transform b in bones)
        {
            if (b == null) continue;
            center += b.position;
            validCount++;
        }

        if (validCount == 0) return;

        center /= validCount;

        transform.position = center;

        transform.rotation = Quaternion.identity;
    }
}