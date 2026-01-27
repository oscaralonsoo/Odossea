using UnityEngine;

public class DynamicCameraBounds : MonoBehaviour
{
    public BoxCollider2D topWall, bottomWall, leftWall, rightWall;
    private Camera cam;
    public float thickness = 1f;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float height = cam.orthographicSize * 2;
        float width = height * cam.aspect;

        //Top Wall
        topWall.size = new Vector2(width, thickness);
        topWall.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y + height / 2 + thickness / 2, 0);

        //Bot Wall
        bottomWall.size = new Vector2(width, thickness);
        bottomWall.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y - height / 2 - thickness / 2, 0);

        //L Wall
        leftWall.size = new Vector2(thickness, height);
        leftWall.transform.position = new Vector3(cam.transform.position.x - width / 2 - thickness / 2, cam.transform.position.y, 0);

        //R Wall
        rightWall.size = new Vector2(thickness, height);
        rightWall.transform.position = new Vector3(cam.transform.position.x + width / 2 + thickness / 2, cam.transform.position.y, 0);
    }
}
