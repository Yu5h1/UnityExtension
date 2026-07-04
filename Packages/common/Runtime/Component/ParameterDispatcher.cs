using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Type = System.Type;
using Exception = System.Exception;

namespace Yu5h1Lib
{
    public class ParameterDispatcher : MonoBehaviour
    {        
        public Object target;
        public void Apply(Object parameter)
        {
            if (target == null || parameter == null)
                return;
            if (!(parameter is IParameter p))
                return;
            p.ApplyTo(target);
        }
    }
}
