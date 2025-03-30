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

    [SerializeField, ReadOnly] private Image[] images;
    [SerializeField, ReadOnly] private SpriteRenderer[] spriteRenderers;
    [SerializeField, ReadOnly] private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock propblock;
    private MaterialPropertyBlock[] propblocks;
    private bool TweenFromChildren;

    public override Component OverrideGetComponent()
    {
        if (IncludeChildren)
        {
            images = gameObject.GetComponentsInChildren<Image>(true);
            spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>(true);
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
            return m.GetComponent<Image>();
        }
        else if (TryGetComponent(out SpriteRenderer spriteRenderer))
            return spriteRenderer;
        else if (TryGetComponent(out MeshRenderer mr))
        {
            propblock = new MaterialPropertyBlock();
            if (IncludeChildren)
                SetMeshsColor(GetBlockColor(mr, propblock));
            return mr;
        }else if (IncludeChildren)
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
            case Image img:
            case SpriteRenderer sr :
            case MeshRenderer mr:
                return DOTween.To(GetAlpha, SetAlpha, endValue, Duration).SetTarget(component);
            default:
                throw new System.NullReferenceException($"{component} DOFade require CanvasGroup or SpriteRenderer");
        }
    }
    private float GetAlpha() {
        switch (component)
        {
            case CanvasGroup g:
                return g.alpha;
            case Image img:
                return img.color.a;
            case SpriteRenderer sr:
                return sr.color.a;
            case MeshRenderer mr:
                return GetBlockColor(mr, propblock).a; 
        }
        throw new System.NullReferenceException($"{component} DOFade require CanvasGroup ,Image or SpriteRenderer");
    }
    private void SetAlpha(float alpha)
    {
        switch (component)
        {
            case CanvasGroup c:
                c.alpha = alpha;
                break;
            case Image img:
                img.color = img.color.ChangeAlpha(alpha);
                break;
            case SpriteRenderer renderer:
                renderer.color = renderer.color.ChangeAlpha(alpha);      
                break;
            case MeshRenderer mr:
                SetBlockColor(mr,propblock, GetBlockColor(mr, propblock).ChangeAlpha(alpha));
                break;

        }
        if (IncludeChildren)
        {
            if (!(component is CanvasGroup) && !images.IsEmpty())
            {
                for (int i = 0; i < images.Length; i++)
                    images[i].color = images[i].color.ChangeAlpha(alpha);
            }
            if (!spriteRenderers.IsEmpty())
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                    spriteRenderers[i].color = spriteRenderers[i].color.ChangeAlpha(alpha);
            }
            if (!meshRenderers.IsEmpty())
            {
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    SetBlockColor(meshRenderers[i], propblocks[i],
                        GetBlockColor(meshRenderers[i], propblocks[i]).ChangeAlpha(alpha));
                }
            }
        }    
    }
    private Color GetBlockColor(MeshRenderer r,MaterialPropertyBlock block)
    {
        r.GetPropertyBlock(block);
        return block.GetColor("_Color");
    }
    private void SetBlockColor(MeshRenderer r, MaterialPropertyBlock block, Color c)
    {
        block.SetColor("_Color", c);
        r.SetPropertyBlock(block);
    }
    private void SetMeshsColor(Color color)
    {
        if (meshRenderers.IsEmpty() || propblocks.IsEmpty())
            return;
        for (int i = 0; i < propblocks.Length; i++)
            SetBlockColor(meshRenderers[i], propblocks[i], color);
    }
    [ContextMenu("Rewind")]
    public void Rewind()
    {
        tweener.Rewind();
    }
}
