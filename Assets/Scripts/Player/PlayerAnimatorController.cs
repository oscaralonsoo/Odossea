using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;

    [Header("Smoothing")]
    [SerializeField] private float verticalSmooth = 0.1f;

    private float smoothedVerticalVelocity;

    private void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();

        if (!playerController)
            playerController = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (!playerController || !animator) return;

        smoothedVerticalVelocity = Mathf.Lerp(
            smoothedVerticalVelocity,
            playerController.AverageVerticalVelocity,
            verticalSmooth
        );
        animator.SetBool("isGrounded", playerController.IsGrounded);
        animator.SetFloat("VerticalVelocity", smoothedVerticalVelocity);
    }
}
