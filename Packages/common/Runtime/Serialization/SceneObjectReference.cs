using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib
{
    public class SceneObjectReference
    {
        public enum FindMethod
        {
            None,
            Name,
            Tag
        }
        [SerializeField] Object _object;
        public string hierarchyPath;
        public string objectName;
        public string tag;
        public FindMethod findMethod;

        public Object GetObject() => findMethod switch
        {
            FindMethod.None => _object,
            FindMethod.Name => _object ?? (_object = SceneManager.GetActiveScene().Find<Transform>(hierarchyPath)),
            FindMethod.Tag => _object ?? (_object = GameObject.FindGameObjectWithTag(tag)),
            _ => throw new System. ArgumentOutOfRangeException(nameof(FindMethod), $"Not expected FindMethod value: {findMethod}")
        };

        public void Refresh()
        {
            _object = null;
        }

        public static implicit operator Object(SceneObjectReference s) => s.GetObject();

    } 
}
