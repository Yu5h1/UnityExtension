using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class TagLayerMask
    {
        //private List<Transform> _transformsToIgnore = new List<Transform>();
        //public IEnumerable<Transform> transformsToIgnore
        //{
        //    get => _transformsToIgnore;
        //    set
        //    {
        //        _transformsToIgnore.Clear();
        //        if (value.IsEmpty())
        //            return;
        //        _transformsToIgnore.AddRange(value);
        //    }
        //}

        //public Transform ignoredTransform
        //{
        //    get => _transformsToIgnore.FirstOrDefault();
        //    set
        //    {
        //        _transformsToIgnore.Clear();
        //        _transformsToIgnore.Add(value);
        //    }
        //}
        [SerializeField]
        private LayerMask _layers;
        public LayerMask layers => _layers;

        public TagOption tagOption;

        public bool Validate(Behaviour behaviour,Component other)
            => behaviour.isActiveAndEnabled && Validate(other);
        public bool Validate(Component other)
            => layers.value != 0 && tagOption.Compare(other.gameObject) && layers.Contains(other.gameObject);
            //!_transformsToIgnore.Contains(other.transform) && tagOption.Compare(other.gameObject) && layers.Contains(other.gameObject);

        public T[] Filter<T>(IEnumerable<T> components) where T : Component
        { 
            var results = new List<T>();
            foreach (var item in components)
                if (Validate(item))
                    results.Add(item);
            return results.ToArray();
        }

    } 
}
