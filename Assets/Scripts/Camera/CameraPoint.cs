using Unity.Cinemachine;
using UnityEngine;

public class CameraPoint : MonoBehaviour
{
    [Header("Point Config")]
    public float zoomFijo = 10f;

    public float offsetX = 0f;
    public float offsetY = 0f;

    [Header("Is Temporal?")]
    public bool esZonaTemporal = false;
    public Vector2 rangoZoomOriginal = new Vector2(5, 36);

    private Vector2 _framingOffsetOriginal;
    private bool _camaraDentro = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var vcam = other.GetComponent<CinemachineCamera>();

        if (vcam != null)
        {
            _camaraDentro = true;
            ApplySettings(vcam);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var vcam = other.GetComponent<CinemachineCamera>();

        if (vcam != null && esZonaTemporal)
        {
            _camaraDentro = false;
            RestoreSettings(vcam);
        }
    }

    void ApplySettings(CinemachineCamera vcam)
    {
        var framing = vcam.GetComponent<CinemachineGroupFraming>();

        if (framing != null)
        {
            _framingOffsetOriginal = framing.CenterOffset;
            rangoZoomOriginal = framing.OrthoSizeRange;

            float currentOrtho = framing.OrthoSizeRange.y;

            framing.CenterOffset = new Vector2(offsetX, offsetY);
            framing.OrthoSizeRange = new Vector2(zoomFijo, currentOrtho);
        }
    }


    void RestoreSettings(CinemachineCamera vcam)
    {
        var framing = vcam.GetComponent<CinemachineGroupFraming>();

        if (framing != null)
        {

            framing.CenterOffset = _framingOffsetOriginal;

            //Reset Original Zoom Range
            framing.OrthoSizeRange = rangoZoomOriginal;
        }
    }
}