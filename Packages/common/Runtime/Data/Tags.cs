using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
	[System.Serializable]
	public class Tags
	{
        [Tooltip("Compare root transform.")]
        public bool root = false;
        [DropDownTag(true)]
        public string tag = "Untagged";
        private string[] _tags;
        public string[] tags
        {
            get
            {
                if (_tags == null)
                    _tags = tag.Split(',');
                return _tags;
            }
        }
        public bool IsUntagged => tag.IsEmpty() || tag.Equals("Untagged");

        public virtual bool Compare(GameObject obj)
        {
            var otherTag = root ? obj.transform.root.tag : obj.tag;
            if (IsUntagged)
                return true;
            return tag.Contains(',') ? otherTag.EqualsAny(tags) : tag == otherTag;
        }

        public IEnumerable<GameObject> FindMatchedGameObjects()
        {
            foreach (var tag in tags)
            {
                foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
                {
                    yield return obj;
                }
            }
        } 
        public override string ToString() => tag;
    } 
}
