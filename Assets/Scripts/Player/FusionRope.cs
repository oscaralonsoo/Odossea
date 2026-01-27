using UnityEngine;

public class FusionRope : MonoBehaviour
{
    [SerializeField] Transform[] bonesTransform;
    [SerializeField] Transform otherRope;

    void Update()
    {
        Vector3 centerPosition = Vector3.zero;
        foreach (Transform transform in bonesTransform)
        {
            centerPosition += transform.position;
        }
        if (bonesTransform.Length > 0) centerPosition /= bonesTransform.Length;

        transform.position = centerPosition;
        transform.LookAt(otherRope);
    }
}
