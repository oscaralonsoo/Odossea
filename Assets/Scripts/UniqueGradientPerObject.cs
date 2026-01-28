using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class UniqueGradientPerObject : MonoBehaviour
{
    SpriteRenderer sr;
    MaterialPropertyBlock mpb;

    public Color topColor = Color.white;
    public Color bottomColor = Color.black;
    public float minY = -0.5f;
    public float maxY = 0.5f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        Apply();
    }

    void OnValidate()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        Apply();
    }

    void Apply()
    {
        sr.GetPropertyBlock(mpb);

        mpb.SetColor("_TopColor", topColor);
        mpb.SetColor("_BottomColor", bottomColor);
        mpb.SetFloat("_MinY", minY);
        mpb.SetFloat("_MaxY", maxY);

        sr.SetPropertyBlock(mpb);
    }
}
