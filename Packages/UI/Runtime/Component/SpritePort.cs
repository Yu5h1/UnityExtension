using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    public class SpritePort : MonoBehaviour
    {
        [SerializeField] private UnityEvent<Sprite> _ValueChanged;
        public void Set(Sprite sprite) => _ValueChanged?.Invoke(sprite);
    }
}
