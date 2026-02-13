using UnityEngine;

namespace Yu5h1Lib
{
    public class CollectionConstraintAttribute : PropertyAttribute
    {
        public bool Unique { get; }
        public bool NotNull { get; }
        public int MinCount { get; }
        public int MaxCount { get; }

        public CollectionConstraintAttribute(
            bool unique = false,
            bool notNull = false,
            int minCount = 0,
            int maxCount = int.MaxValue)
        {
            Unique = unique;
            NotNull = notNull;
            MinCount = minCount;
            MaxCount = maxCount;
        }
    }
}