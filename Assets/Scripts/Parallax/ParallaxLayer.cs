using UnityEngine;

public class InfiniteParallaxTriple : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxStrength = 0.3f;

    public Transform left;
    public Transform right;

    private Camera cam;
    private Vector3 startPos;
    private float spriteWidth;

    void Start()
    {
        cam = Camera.main;
        startPos = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        spriteWidth = sr.bounds.size.x;

        // Posicionar vecinos
        left.position = startPos + Vector3.left * spriteWidth;
        right.position = startPos + Vector3.right * spriteWidth;
    }

    void LateUpdate()
    {
        float camX = cam.transform.position.x;

        // Parallax ABSOLUTO
        float targetX = startPos.x + camX * parallaxStrength;
        transform.position = new Vector3(targetX, startPos.y, transform.position.z);

        // Mantener vecinos pegados
        left.position = transform.position + Vector3.left * spriteWidth;
        right.position = transform.position + Vector3.right * spriteWidth;

        // Recolocar bloque entero
        float camDelta = camX - transform.position.x;

        if (Mathf.Abs(camDelta) >= spriteWidth)
        {
            float offset = Mathf.Sign(camDelta) * spriteWidth;
            startPos.x += offset;
        }
    }
}
