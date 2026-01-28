using UnityEngine;

public class PlayerCustomizer : MonoBehaviour
{
    [Header("Settings")]
    public SpriteRenderer playerRenderer;       
    public SpriteRenderer previewRenderer;

    public Material[] availableMaterials;

    private int currentIndex = 0;
    private Material originalMaterial;

    void Start()
    {
        if (playerRenderer != null)
            originalMaterial = playerRenderer.material;

        if (previewRenderer != null)
            previewRenderer.material = originalMaterial;
    }

    public void NextMaterial()
    {
        if (availableMaterials.Length == 0) return;

        currentIndex++;
        if (currentIndex >= availableMaterials.Length)
            currentIndex = 0;

        UpdatePreview();
    }

    public void PreviousMaterial()
    {
        if (availableMaterials.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = availableMaterials.Length - 1;

        UpdatePreview();
    }

    void UpdatePreview()
    {
        if (previewRenderer != null)
        {

            previewRenderer.material = availableMaterials[currentIndex];

            previewRenderer.material.color = new Color(1.2f, 1.2f, 1.2f, 1f); 
        }
    }

    public void ApplyCurrent()
    {
        if (playerRenderer != null)
        {
            playerRenderer.material = availableMaterials[currentIndex];
            foreach (DropableCube cube in playerRenderer.GetComponentInParent<PlayerController>().dropableCubes)
            {
                cube.GetComponentInChildren<SpriteRenderer>().material = playerRenderer.material;
            }
        }
            originalMaterial = playerRenderer.material;
    }

    public void ResetMaterial()
    {
        currentIndex = 0;

        if (playerRenderer != null)
            playerRenderer.material = originalMaterial;

        if (previewRenderer != null)
            previewRenderer.material = originalMaterial;
    }

    public void SetMaterial(int index)
    {
        if (index < 0 || index >= availableMaterials.Length) return;

        currentIndex = index;
        UpdatePreview();
    }
}
