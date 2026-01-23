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
    public float playerRadius = 0.5f; // Para que no se corte el cuerpo al borde

    [Header("Zoom")]
    public float minZoom = 5f;
    public float maxZoom = 12f;
    public float zoomSpeed = 5f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 16f;

    private Camera cam;
    private Vector3 velocity;
    private Vector3 rawPosition;

    private Rigidbody2D rb1;
    private Rigidbody2D rb2;

    void Start()
    {
        cam = GetComponent<Camera>();
        rawPosition = transform.position;

        // Caché de Rigidbodies para evitar deformaciones en los huesos
        if (player1) rb1 = player1.GetComponent<Rigidbody2D>();
        if (player2) rb2 = player2.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!player1 || !player2) return;

        HandleMovement();
        HandleZoom();
        ApplyPixelPerfectPosition();
    }

    void FixedUpdate()
    {
        if (!player1 || !player2) return;

        // Restringimos la física después de que la cámara ha decidido su posición
        ClampPlayerPhysically(rb1);
        ClampPlayerPhysically(rb2);
    }

    void HandleMovement()
    {
        // Punto medio entre ambos jugadores
        Vector3 midpoint = (player1.position + player2.position) / 2f;

        // Suavizado de la posición "cruda" antes del pixel perfect
        rawPosition = Vector3.SmoothDamp(rawPosition, midpoint, ref velocity, smoothTime);
    }

    void HandleZoom()
    {
        float distanceX = Mathf.Abs(player1.position.x - player2.position.x) + padding.x;
        float distanceY = Mathf.Abs(player1.position.y - player2.position.y) + padding.y;

        // Calcular zoom basado en el aspecto de la cámara
        float targetZoomHeight = Mathf.Max(distanceY / 2f, (distanceX / cam.aspect) / 2f);
        float targetZoom = Mathf.Clamp(targetZoomHeight, minZoom, maxZoom);

        // Interpolación suave del tamaño ortográfico
        float zoom = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        cam.orthographicSize = Mathf.Round(zoom * pixelsPerUnit) / pixelsPerUnit;
    }

    void ClampPlayerPhysically(Rigidbody2D rb)
    {
        if (rb == null) return;

        // Calculamos los límites del Viewport en coordenadas de mundo
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // Definimos los bordes usando la posición actual de la cámara
        float minX = transform.position.x - camWidth + playerRadius;
        float maxX = transform.position.x + camWidth - playerRadius;
        float minY = transform.position.y - camHeight + playerRadius;
        float maxY = transform.position.y + camHeight - playerRadius;

        Vector2 currentPos = rb.position;
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(currentPos.x, minX, maxX),
            Mathf.Clamp(currentPos.y, minY, maxY)
        );

        // Si el jugador está intentando salir, lo movemos físicamente
        if (currentPos != clampedPos)
        {
            // MovePosition es la clave para no romper los huesos (Skinning)
            rb.MovePosition(clampedPos);

            // Opcional: Matar la velocidad si choca contra el borde para evitar rebotes visuales
            if (Mathf.Abs(currentPos.x - clampedPos.x) > 0.01f)
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void ApplyPixelPerfectPosition()
    {
        // Redondeo a la rejilla de píxeles para evitar jitter visual en el fondo
        float x = Mathf.Round(rawPosition.x * pixelsPerUnit) / pixelsPerUnit;
        float y = Mathf.Round(rawPosition.y * pixelsPerUnit) / pixelsPerUnit;
        transform.position = new Vector3(x, y, -10f);
    }
}