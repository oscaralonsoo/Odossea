using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    [Header("Movement")]
    public float smoothTime = 0.15f;
    public Vector2 padding = new Vector2(2f, 1f);
    public float playerRadius = 0.5f; // Para que los bones no se corten al borde

    [Header("Zoom")]
    public float minZoom = 5f;
    public float maxZoom = 12f;
    public float zoomSpeed = 5f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 16f;

    private Camera cam;
    private Vector3 velocity;
    private Vector3 rawPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        rawPosition = transform.position;
    }

    void LateUpdate()
    {
        if (!player1 || !player2) return;

        // Calculamos bounds de todos los bones
        Bounds bounds1 = GetPlayerBounds(player1);
        Bounds bounds2 = GetPlayerBounds(player2);

        // Calculamos el rectángulo que contiene ambos jugadores
        Vector2 min = Vector2.Min(bounds1.min, bounds2.min);
        Vector2 max = Vector2.Max(bounds1.max, bounds2.max);

        // Añadimos padding
        min -= padding;
        max += padding;

        // Centro de la cámara
        Vector3 targetPos = (min + max) / 2f;

        // Suavizado
        rawPosition = Vector3.SmoothDamp(rawPosition, targetPos, ref velocity, smoothTime);

        // Zoom dinámico según tamaño del rectángulo
        float distanceX = max.x - min.x;
        float distanceY = max.y - min.y;
        float targetZoomHeight = Mathf.Max(distanceY / 2f, distanceX / (2f * cam.aspect));
        float targetZoom = Mathf.Clamp(targetZoomHeight, minZoom, maxZoom);
        cam.orthographicSize = Mathf.Round(Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed) * pixelsPerUnit) / pixelsPerUnit;

        // Pixel-perfect
        ApplyPixelPerfectPosition();
    }


    void FixedUpdate()
    {
        if (!player1 || !player2) return;

        ClampPlayerPhysically(player1);
        ClampPlayerPhysically(player2);
    }

    #region Camera Logic

    void HandleMovement()
    {
        Bounds bounds1 = GetPlayerBounds(player1);
        Bounds bounds2 = GetPlayerBounds(player2);

        // Centro entre ambos jugadores
        Vector3 midpoint = (bounds1.center + bounds2.center) / 2f;

        // Suavizado
        rawPosition = Vector3.SmoothDamp(rawPosition, midpoint, ref velocity, smoothTime);
    }

    void HandleZoom()
    {
        Bounds bounds1 = GetPlayerBounds(player1);
        Bounds bounds2 = GetPlayerBounds(player2);

        // Distancia total incluyendo padding
        float distanceX = Mathf.Max(bounds1.max.x, bounds2.max.x) - Mathf.Min(bounds1.min.x, bounds2.min.x) + padding.x;
        float distanceY = Mathf.Max(bounds1.max.y, bounds2.max.y) - Mathf.Min(bounds1.min.y, bounds2.min.y) + padding.y;

        float targetZoomHeight = Mathf.Max(distanceY / 2f, (distanceX / cam.aspect) / 2f);
        float targetZoom = Mathf.Clamp(targetZoomHeight, minZoom, maxZoom);

        float zoom = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        cam.orthographicSize = Mathf.Round(zoom * pixelsPerUnit) / pixelsPerUnit;
    }

    void ApplyPixelPerfectPosition()
    {
        float x = Mathf.Round(rawPosition.x * pixelsPerUnit) / pixelsPerUnit;
        float y = Mathf.Round(rawPosition.y * pixelsPerUnit) / pixelsPerUnit;
        transform.position = new Vector3(x, y, -10f);
    }

    #endregion

    #region Player Bounds & Clamping

    Bounds GetPlayerBounds(Transform player)
    {
        Rigidbody2D[] bones = player.GetComponentsInChildren<Rigidbody2D>();
        if (bones.Length == 0) return new Bounds(player.position, Vector3.zero);

        Vector2 min = bones[0].position;
        Vector2 max = bones[0].position;

        foreach (var bone in bones)
        {
            Vector2 pos = bone.position;
            min = Vector2.Min(min, pos);
            max = Vector2.Max(max, pos);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

   void ClampPlayerPhysically(Transform player)
{
    Rigidbody2D[] bones = player.GetComponentsInChildren<Rigidbody2D>();
    if (bones.Length == 0) return;

    float camHeight = cam.orthographicSize;
    float camWidth = camHeight * cam.aspect;

    // Calculamos los límites actuales de la cámara
    float minX = transform.position.x - camWidth + playerRadius;
    float maxX = transform.position.x + camWidth - playerRadius;
    float minY = transform.position.y - camHeight + playerRadius;
    float maxY = transform.position.y + camHeight - playerRadius;

    foreach (var rb in bones)
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool modified = false;

        // Solo actuamos si el hueso se sale de los bordes
        if (pos.x < minX) {
            pos.x = minX;
            if (vel.x < 0) vel.x = 0;
            modified = true;
        } else if (pos.x > maxX) {
            pos.x = maxX;
            if (vel.x > 0) vel.x = 0;
            modified = true;
        }

        if (pos.y < minY) {
            pos.y = minY;
            if (vel.y < 0) vel.y = 0;
            modified = true;
        } else if (pos.y > maxY) {
            pos.y = maxY;
            if (vel.y > 0) vel.y = 0;
            modified = true;
        }

        // IMPORTANTE: Solo aplicamos cambios si realmente hubo una colisión con el borde
        if (modified) {
            rb.linearVelocity = vel;
            rb.position = pos; // Usar .position directamente en FixedUpdate es más limpio para "clamping"
        }
    }
}

    #endregion
}
