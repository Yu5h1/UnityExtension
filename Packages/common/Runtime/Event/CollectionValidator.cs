using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class CollectionValidator<T> : Validator where T : Object
    {
        public enum Mode { All, Any }

        public T[] targets;
        [Inline(true)] public Logic logic;
        public Mode mode;

        protected abstract object GetValue(T target);

        protected override bool Evaluate()
        {
            if (targets .IsEmpty() || logic == null)
                return false;

            return mode == Mode.All
                ? targets.All(t => t != null && logic.Evaluate(GetValue(t)))
                : targets.Any(t => t != null && logic.Evaluate(GetValue(t)));
        }
    }
}
