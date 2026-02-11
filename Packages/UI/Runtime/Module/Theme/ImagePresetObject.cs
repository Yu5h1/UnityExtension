using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public class ImagePresetObject : ParameterObject<ImagePreset> { }

    [System.Serializable]
    public class ImagePreset : ComponentPreset<Image>
    {
        public Optional<Sprite> sprite;
        public Optional<Color> color;
        public Optional<bool> raycastTarget;
        public Optional<Vector4> raycastPadding;
        public Optional<bool> maskable;
        public Optional<Image.Type> type;
        public Optional<bool> useSpriteMesh;
        public Optional<bool> preserveAspect;

        public override void ApplyTo(Image img)
        {
            if (sprite.TryGetValue(out Sprite s))
                img.sprite = s;
            if (color.TryGetValue(out Color c))
                img.color = c;
            if (raycastTarget.TryGetValue(out bool rt))
                img.raycastTarget = rt;
            if (raycastPadding.TryGetValue(out Vector4 r))
                img.raycastPadding = r;
            if (maskable.TryGetValue(out bool mask))
                img.maskable = mask;
            if (type.TryGetValue(out Image.Type mt))
                img.type = mt;
            if (useSpriteMesh.TryGetValue(out bool usm))
                img.useSpriteMesh = usm;
            if (preserveAspect.TryGetValue(out bool p))
                img.preserveAspect = p;

        }
    }
}