using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class TagOption : Tags
    {
        public enum FilterMode
        {
            Include = 0,
            Exclude = 1
        }
        public FilterMode mode = FilterMode.Exclude;

        public override bool Compare(GameObject obj)
        {
            var otherTag = root ? obj.transform.root.tag : obj.tag;
            if (IsUntagged)
                return true;
            return mode == FilterMode.Include ?
                tag.Contains(',') ? otherTag.EqualsAny(tags) : tag == otherTag :
                tag.Contains(',') ? !otherTag.EqualsAny(tags) : tag != otherTag;
        }
    }
}