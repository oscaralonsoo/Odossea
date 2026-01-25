using System.Collections;
using UnityEngine;

public class MassCubeInstantiator : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float launchForce = 200f;
    [SerializeField] private float ignoreCollisionTime = 0.3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (gameObject.GetComponent<PlayerController>().growNum > 1)
            {
                LaunchCube();
                gameObject.GetComponent<PlayerController>().Shrink(1);
            }
        }
    }

    void LaunchCube()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - transform.position).normalized;

        GameObject cubeMass = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        Rigidbody2D rb = cubeMass.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction * launchForce, ForceMode2D.Impulse);
        }

        for (int i = 0; i < cubeMass.transform.childCount; i++)
        {
            Rigidbody2D childRb = cubeMass.transform.GetChild(i).GetComponent<Rigidbody2D>();
            if (childRb != null)
                childRb.AddForce(direction * launchForce, ForceMode2D.Impulse);
        }

        StartCoroutine(IgnoreCollisionTemporarily(cubeMass));
    }

    IEnumerator IgnoreCollisionTemporarily(GameObject cubeMass)
    {
        var playerController = GetComponent<PlayerController>();

        foreach (var playerBone in playerController.bones)
        {
            for (int i = 0; i < cubeMass.transform.childCount; i++)
            {
                Collider2D cubeCol = cubeMass.transform.GetChild(i).GetComponent<Collider2D>();
                Physics2D.IgnoreCollision(cubeCol, playerBone.collider2D, true);
            }
        }

        yield return new WaitForSeconds(ignoreCollisionTime);

        foreach (var playerBone in playerController.bones)
        {
            for (int i = 0; i < cubeMass.transform.childCount; i++)
            {
                Collider2D cubeCol = cubeMass.transform.GetChild(i).GetComponent<Collider2D>();
                if (cubeCol != null && playerBone.collider2D != null)
                {
                    Physics2D.IgnoreCollision(cubeCol, playerBone.collider2D, false);
                }
            }
        }
    }
}
