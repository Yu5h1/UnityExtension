using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.MVC
{
    public abstract class View : MonoBehaviour
    {
        public abstract string GetFieldName();
        public abstract string GetValue();
    } 
}
