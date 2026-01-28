using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    Rigidbody2D rigidBody;

    [Header("ROUTE")]
    [SerializeField] List<Transform> routePoints;

    [Header("PROPERTIES")]
    [SerializeField] bool forward = true;
    [SerializeField] bool jumpToExteme = false;
    [SerializeField] float moveSpeed;
    [SerializeField] float brakingDistance = 0.1f;
    [SerializeField] float accelerationDistance = 0.1f;

    [Header("DEBUG")]
    [SerializeField] int currentPoint = 0;
    [SerializeField] int destinationPoint = 0;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        transform.position = routePoints[0].position;
        currentPoint = 0;
        destinationPoint = 0;
        CalculateNextPoint();

        if (accelerationDistance < 0.1f) accelerationDistance = 0.1f;
        if (brakingDistance < 0.1f) brakingDistance = 0.1f;
    }

    void FixedUpdate()
    {
        if (routePoints == null || routePoints.Count <= 1) return;

        Vector2 startPos = routePoints[currentPoint].position;
        Vector2 targetPos = routePoints[destinationPoint].position;
        Vector2 currentPos = transform.position;

        Vector2 toTarget = targetPos - currentPos;
        Vector2 fromStart = currentPos - startPos;

        Vector2 moveDirection = (targetPos - startPos).normalized;

        if (Vector2.Distance(transform.position, routePoints[destinationPoint].position) <= 0.1f)
        {
            transform.position = targetPos;
            rigidBody.linearVelocity = Vector2.zero;
            CalculateNextPoint();
        }
        else
        {
            if (rigidBody != null)
            {
                float distFromStart = fromStart.magnitude;
                float accelFactor = Mathf.Clamp01(distFromStart / accelerationDistance);
                accelFactor = Mathf.SmoothStep(0, 1, accelFactor);

                float distToTarget = toTarget.magnitude;
                float brakeFactor = Mathf.Clamp01(distToTarget / brakingDistance);
                brakeFactor = Mathf.SmoothStep(0, 1, brakeFactor);

                float finalSpeed = Mathf.Max(moveSpeed * accelFactor * brakeFactor, 0.2f);

                rigidBody.linearVelocity = moveDirection * finalSpeed;
            }
        }
    }

    void CalculateNextPoint()
    {
        int count = routePoints.Count;

        if (count > 1)
        {
            currentPoint = destinationPoint;

            if (forward) destinationPoint++;
            else destinationPoint--;

            if (jumpToExteme)
            {
                if (destinationPoint >= count) destinationPoint = 0;
                if (destinationPoint < 0) destinationPoint = count - 1;
            }
            else
            {
                if (forward && destinationPoint >= count)
                {
                    forward = false;
                    destinationPoint = count - 2;
                }
                else if (!forward && destinationPoint < 0)
                {
                    forward = true;
                    destinationPoint = 1;
                }
            }
        }
        else
        {
            currentPoint = 0;
            destinationPoint = 0;
        }
    }
}
