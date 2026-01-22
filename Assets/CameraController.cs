using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    [Header("Movement")]
    public float smoothTime = 0.15f;
    public Vector2 padding = new Vector2(2f, 1f); // Espacio extra para que no toquen el borde

    [Header("Zoom")]
    public float minZoom = 5f;
    public float maxZoom = 12f;
    public float zoomSpeed = 5f;

    [Header("Pixel Perfect")]
    public float pixelsPerUnit = 16f;

    private Camera cam;
    private Vector3 velocity;
    private Vector3 rawPosition; // Posición "real" sin redondear

    void Start()
    {
        cam = GetComponent<Camera>();
        rawPosition = transform.position;
    }

    void LateUpdate()
    {
        if (!player1 || !player2) return;

        HandleMovement();
        HandleZoom();
        ApplyPixelPerfectPosition();
    }

    void HandleMovement()
    {
        // 1. Encontrar el punto medio
        Vector3 midpoint = (player1.position + player2.position) / 2f;

        // 2. Suavizar la posición lógica (rawPosition)
        rawPosition = Vector3.SmoothDamp(rawPosition, midpoint, ref velocity, smoothTime);
    }

    void HandleZoom()
    {
        float distanceX = Mathf.Abs(player1.position.x - player2.position.x) + padding.x;
        float distanceY = Mathf.Abs(player1.position.y - player2.position.y) + padding.y;

        float targetZoomHeight = Mathf.Max(distanceY / 2f, (distanceX / cam.aspect) / 2f);

        float targetZoom = Mathf.Clamp(targetZoomHeight, minZoom, maxZoom);

        // Interpolamos suavemente
        float zoom = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);

        // Redondear zoom para que sea pixel-perfect
        cam.orthographicSize = Mathf.Round(zoom * pixelsPerUnit) / pixelsPerUnit;
    }


    void ApplyPixelPerfectPosition()
    {
        // Redondeamos la rawPosition a la rejilla de píxeles para la cámara
        float x = Mathf.Round(rawPosition.x * pixelsPerUnit) / pixelsPerUnit;
        float y = Mathf.Round(rawPosition.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = new Vector3(x, y, -10f);
    }
}