using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Component added to instantiated part GameObjects tracking the source PartAsset and attachment state.
    /// </summary>
    [DisallowMultipleComponent]
    public class AttachedPart : MonoBehaviour
    {
        public PartAsset part;
        public string targetBone;
        public bool isMirrored;

        /// <summary>
        ///   Detaches and destroys this part instance.
        /// </summary>
        public void Detach()
        {
            PartAttachment.Detach(gameObject);
        }
    }
}
