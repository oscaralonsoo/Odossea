using UnityEngine;

public class InfiniteParallaxFive : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxStrength = 0.3f;

    [Range(0f, 1f)]
    public float parallaxY = 0.15f;

    public Transform left1;
    public Transform left2;
    public Transform right1;
    public Transform right2;

    private Camera cam;
    private Vector3 startPos;
    private float spriteWidth;

    void Start()
    {
        cam = Camera.main;
        startPos = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        spriteWidth = sr.bounds.size.x;

        UpdatePanels();
    }

    void LateUpdate()
    {
        Vector3 camPos = cam.transform.position;

        float targetX = startPos.x + camPos.x * parallaxStrength;
        float targetY = startPos.y + camPos.y * parallaxY;

        transform.position = new Vector3(targetX, targetY, transform.position.z);

        UpdatePanels();

        float camDelta = camPos.x - transform.position.x;

        if (Mathf.Abs(camDelta) >= spriteWidth)
        {
            startPos.x += Mathf.Sign(camDelta) * spriteWidth;
        }
    }

    void UpdatePanels()
    {
        left1.position = transform.position + Vector3.left * spriteWidth;
        left2.position = transform.position + Vector3.left * spriteWidth * 2f;

        right1.position = transform.position + Vector3.right * spriteWidth;
        right2.position = transform.position + Vector3.right * spriteWidth * 2f;
    }
}
