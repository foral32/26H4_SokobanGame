using UnityEngine;
using DG.Tweening;

public interface IFadeable
{
    void SetAlpha(float alpha);
    Tween FadeTo(float targetAlpha, float duration);
}

// Renderer实现（用于3D物体）
public class RendererFadeTarget : IFadeable
{
    private readonly Renderer renderer;
    private readonly Material[] materials;
    private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");
    
    public RendererFadeTarget(Renderer renderer)
    {
        this.renderer = renderer;
        this.materials = renderer.materials;
    }

    public void SetAlpha(float alpha)
    {
        foreach (var material in materials)
        {
            SetMaterialAlpha(material, alpha);
        }
    }

    public Tween FadeTo(float targetAlpha, float duration)
    {
        var sequence = DOTween.Sequence();
        foreach (var material in materials)
        {
            sequence.Join(DOTween.To(() => GetMaterialAlpha(material),
                value => SetMaterialAlpha(material, value),
                targetAlpha, duration));
        }
        return sequence;
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material.HasProperty("_Color"))
        {
            var color = material.color;
            color.a = alpha;
            material.color = color;
        }
        else if (material.HasProperty(AlphaProperty))
        {
            material.SetFloat(AlphaProperty, alpha);
        }
    }

    private float GetMaterialAlpha(Material material)
    {
        if (material.HasProperty("_Color"))
        {
            return material.color.a;
        }
        else if (material.HasProperty(AlphaProperty))
        {
            return material.GetFloat(AlphaProperty);
        }
        return 1f;
    }
}

// CanvasGroup实现（用于UI组）
public class CanvasGroupFadeTarget : IFadeable
{
    private readonly CanvasGroup canvasGroup;

    public CanvasGroupFadeTarget(CanvasGroup canvasGroup)
    {
        this.canvasGroup = canvasGroup;
    }

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }

    public Tween FadeTo(float targetAlpha, float duration)
    {
        return canvasGroup.DOFade(targetAlpha, duration);
    }
}

// Image实现（用于单个UI元素）
public class ImageFadeTarget : IFadeable
{
    private readonly UnityEngine.UI.Image image;

    public ImageFadeTarget(UnityEngine.UI.Image image)
    {
        this.image = image;
    }

    public void SetAlpha(float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public Tween FadeTo(float targetAlpha, float duration)
    {
        return image.DOFade(targetAlpha, duration);
    }
}