using UnityEngine;

public class SoftGroundGenerator : MonoBehaviour
{
    public GameObject pointPrefab;
    public int pointCount = 20;
    public float spacing = 0.5f;
    public float springFrequency = 5f;
    public float springDamping = 0.8f;

    Rigidbody2D[] points;

    void Start()
    {
        points = new Rigidbody2D[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 pos = (Vector2)transform.position + Vector2.right * spacing * i;

            GameObject p = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            points[i] = rb;

            if (i > 0)
            {
                SpringJoint2D spring = p.AddComponent<SpringJoint2D>();
                spring.connectedBody = points[i - 1];
                spring.autoConfigureDistance = false;
                spring.distance = spacing;
                spring.frequency = springFrequency;
                spring.dampingRatio = springDamping;
            }
        }
    }
}
