using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MassCubeInstantiator : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float launchForce = 200f;
    [SerializeField] private float spreadAngle = 45f;
    [SerializeField] private float ignoreCollisionTime = 0.3f;
    [SerializeField] private float recoilForce = 50f;

    private void Update()
    {
        
    }
    public void Shoot(Vector2 direction)
    {
        var player = GetComponent<PlayerController>();

        if (player.growNum <= 1)
            return;

        if (direction == Vector2.zero)
            return;

        LaunchCube(direction, false, transform.position, launchForce);
        ApplyRecoilForce(direction);
        player.Shrink(1);
    }



    public void LaunchCubeToMouse()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - transform.position).normalized;

        LaunchCube(direction, disableCollider: false, transform.position, launchForce);

        ApplyRecoilForce(direction);
    }

    private void ApplyRecoilForce(Vector2 launchDirection)
    {
        Vector2 recoilDirection = -launchDirection;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Rigidbody2D childRb = transform.GetChild(i).GetComponent<Rigidbody2D>();
            if (childRb != null)
            {
                childRb.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);
            }
        }
    }


    public void DisperseCubes(int amount, float duration, Vector3 spawnPosition, float maxForce = -1f)
    {
        StartCoroutine(DisperseCubesCoroutine(amount, duration, spawnPosition, maxForce));
    }

    private IEnumerator DisperseCubesCoroutine(int amount, float duration, Vector3 spawnPosition, float maxForce)
    {
        if (amount <= 0) yield break;

        float interval = duration / amount;

        for (int i = 0; i < amount; i++)
        {
            float angle = Random.Range(-spreadAngle, spreadAngle);
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;

            float force = maxForce > 0 ? Random.Range(7f, maxForce) : Random.Range(7f, launchForce);

            LaunchCube(direction, disableCollider: true, spawnPosition, force);

            yield return new WaitForSeconds(interval);
        }
    }

    private void LaunchCube(Vector2 direction, bool disableCollider, Vector3 spawnPosition, float forceOverride)
    {
        GameObject cubeMass = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (disableCollider)
            SetCollidersEnabled(cubeMass, false);

        ApplyForceToMass(cubeMass, direction, forceOverride);

        if (!disableCollider)
            StartCoroutine(IgnoreCollisionTemporarily(cubeMass));
    }

    private void ApplyForceToMass(GameObject cubeMass, Vector2 direction, float force)
    {
        Rigidbody2D rb = cubeMass.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(direction * force, ForceMode2D.Impulse);

        for (int i = 0; i < cubeMass.transform.childCount; i++)
        {
            Rigidbody2D childRb = cubeMass.transform.GetChild(i).GetComponent<Rigidbody2D>();
            if (childRb != null)
                childRb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    private void SetCollidersEnabled(GameObject cubeMass, bool enabled)
    {
        Collider2D[] colliders = cubeMass.GetComponentsInChildren<Collider2D>(true);

        foreach (var col in colliders)
        {
            col.enabled = enabled;
        }
    }

    IEnumerator IgnoreCollisionTemporarily(GameObject cubeMass)
    {
        if (cubeMass == null) yield break;
        var playerController = GetComponent<PlayerController>();
        foreach (var playerBone in playerController.bones)
        {
            Collider2D[] colliders = cubeMass.GetComponentsInChildren<Collider2D>();

            foreach (var cubeCol in colliders)
            {
                if (cubeCol != null && playerBone.collider2D != null)
                    Physics2D.IgnoreCollision(cubeCol, playerBone.collider2D, true);
            }
        }

        yield return new WaitForSeconds(ignoreCollisionTime);

        if (cubeMass == null) yield break; 

        foreach (var playerBone in playerController.bones)
        {
            Collider2D[] colliders = cubeMass.GetComponentsInChildren<Collider2D>();

            foreach (var cubeCol in colliders)
            {
                if (cubeCol != null && playerBone.collider2D != null)
                    Physics2D.IgnoreCollision(cubeCol, playerBone.collider2D, false);
            }
        }
    }
}
