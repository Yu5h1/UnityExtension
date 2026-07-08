using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Plugins.Options;
using Yu5h1Lib;
using FloatTweener = DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;
using System.Linq;


[DisallowMultipleComponent]
public class DOFade : TweenBehaviour<Component,float,float,FloatOptions>
{
    public bool IncludeChildren;
    [SerializeField] private string colorPropertyName = "_Color";

    [SerializeField, ReadOnly] private Image[] images;
    [SerializeField, ReadOnly] private SpriteRenderer[] spriteRenderers;
    [SerializeField, ReadOnly] private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock propblock;
    private MaterialPropertyBlock[] propblocks;
    private IColor[] colors = System.Array.Empty<IColor>();
    private bool TweenFromChildren;
    private string ColorPropertyName => colorPropertyName.IsEmpty() ? "_Color" : colorPropertyName;

    public override Component OverrideGetComponent()
    {
        if (IncludeChildren)
        {
            images = gameObject.GetComponentsInChildren<Image>(true);
            spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            colors = gameObject.GetComponentsInChildren<MonoBehaviour>(true).OfType<IColor>().ToArray();
            propblocks = new MaterialPropertyBlock[meshRenderers.Length];
            for (int i = 0; i < propblocks.Length; i++)
            {
                propblocks[i] = new MaterialPropertyBlock();
                SetBlockColor(meshRenderers[i], propblocks[i], Color.white);
            }
        }
        if (transform is RectTransform m)
        {
            if (TryGetComponent(out CanvasGroup canvasGroup))
                return canvasGroup;
            if (m.TryGetComponent(out Image image))
                return image;
        }
        if (TryGetColorComponent(out Component colorComponent))
            return colorComponent;
        else if (TryGetComponent(out SpriteRenderer spriteRenderer))
            return spriteRenderer;
        else if (TryGetComponent(out MeshRenderer mr))
        {
            propblock = new MaterialPropertyBlock();
            if (IncludeChildren)
                SetMeshsColor(GetBlockColor(mr, propblock));
            return mr;
        }
        else if (IncludeChildren)
        {
            Component result = null;
            if (!images.IsEmpty())
                result = images.First();
            else if (!spriteRenderers.IsEmpty())
                result = spriteRenderers.First();
            else if (!meshRenderers.IsEmpty())
            {
                propblock = new MaterialPropertyBlock();
                result = meshRenderers.First();
            }
            else
                result = GetFirstColorComponent();

            if (TweenFromChildren = result)
                return result;
        }
        return null;
    }
    protected override FloatTweener CreateTweenCore() {
        switch (component)
        {
            case CanvasGroup g:
                return g.DOFade(endValue, Duration);
            case IColor color:
                return DOTween.To(GetAlpha, SetAlpha, endValue, Duration).SetTarget(component);
            case Image img:
            case SpriteRenderer sr :
            case MeshRenderer mr:
                return DOTween.To(GetAlpha, SetAlpha, endValue, Duration).SetTarget(component);
            default:
                throw new System.NullReferenceException($"{component} DOFade require CanvasGroup, IColor, Image, SpriteRenderer or MeshRenderer");
        }
    }
    private float GetAlpha() {
        switch (component)
        {
            case CanvasGroup g:
                return g.alpha;
            case IColor color:
                return color.alpha;
            case Image img:
                return img.color.a;
            case SpriteRenderer sr:
                return sr.color.a;
            case MeshRenderer mr:
                return GetBlockColor(mr, propblock).a; 
        }
        throw new System.NullReferenceException($"{component} DOFade require CanvasGroup, IColor, Image, SpriteRenderer or MeshRenderer");
    }
    private void SetAlpha(float alpha)
    {
        switch (component)
        {
            case CanvasGroup c:
                c.alpha = alpha;
                break;
            case IColor color:
                color.alpha = alpha;
                break;
            case Image img:
                img.color = img.color.SetAlpha(alpha);
                break;
            case SpriteRenderer renderer:
                renderer.color = renderer.color.SetAlpha(alpha);      
                break;
            case MeshRenderer mr:
                SetBlockColor(mr,propblock, GetBlockColor(mr, propblock).SetAlpha(alpha));
                break;

        }
        if (IncludeChildren)
        {
            if (!(component is CanvasGroup) && !images.IsEmpty())
            {
                for (int i = 0; i < images.Length; i++)
                    images[i].color = images[i].color.SetAlpha(alpha);
            }
            if (!spriteRenderers.IsEmpty())
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                    spriteRenderers[i].color = spriteRenderers[i].color.SetAlpha(alpha);
            }
            if (!meshRenderers.IsEmpty())
            {
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    SetBlockColor(meshRenderers[i], propblocks[i],
                        GetBlockColor(meshRenderers[i], propblocks[i]).SetAlpha(alpha));
                }
            }
            if (colors != null && colors.Length > 0)
            {
                for (int i = 0; i < colors.Length; i++)
                    colors[i].alpha = alpha;
            }
        }    
    }
    private Color GetBlockColor(Renderer r,MaterialPropertyBlock block)
    {
        r.GetPropertyBlock(block);
        if (block.isEmpty)
            return TryGetMaterialColor(r, out var materialColor) ? materialColor : Color.white;
        return block.GetColor(ColorPropertyName);
    }
    private void SetBlockColor(Renderer r, MaterialPropertyBlock block, Color c)
    {
        block.SetColor(ColorPropertyName, c);
        r.SetPropertyBlock(block);
    }
    private void SetMeshsColor(Color color)
    {
        if (meshRenderers.IsEmpty() || propblocks.IsEmpty())
            return;
        for (int i = 0; i < propblocks.Length; i++)
            SetBlockColor(meshRenderers[i], propblocks[i], color);
    }
    private bool TryGetMaterialColor(Renderer r, out Color color)
    {
        var material = r.sharedMaterial;
        if (material && material.HasProperty(ColorPropertyName))
        {
            color = material.GetColor(ColorPropertyName);
            return true;
        }

        color = default;
        return false;
    }
    private bool TryGetColorComponent(out Component colorComponent)
    {
        colorComponent = GetComponents<MonoBehaviour>().OfType<IColor>().FirstOrDefault() as Component;
        return colorComponent != null;
    }
    private Component GetFirstColorComponent()
    {
        if (colors == null || colors.Length == 0)
            return null;

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] is Component colorComponent)
                return colorComponent;
        }
        return null;
    }
}
