using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    public class RendererCollector : ComponentCollector<Renderer>
    {
        public TRenderer[] Get<TRenderer>() where TRenderer : Renderer => _components.OfType<TRenderer>().ToArray();

        public SpriteRenderer[] SpriteRenderers => Get<SpriteRenderer>();
        public MeshRenderer[] MeshRenderers => Get<MeshRenderer>();
    }
}
