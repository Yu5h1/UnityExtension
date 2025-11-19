using UnityEngine;

namespace Yu5h1Lib
{
	public abstract class InlineParamterObject : InlineScriptableObject { }


    public abstract class InlineParamterObject<T> : InlineParamterObject
    { 
         public T value;
    }
}
