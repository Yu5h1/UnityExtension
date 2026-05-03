using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class DOTransform<T1,T2, TPlugOptions> : TweenBehaviour<Transform,T1, T2, TPlugOptions>
    where TPlugOptions : struct, IPlugOptions
    {
        //public bool local = true;
    }


    public abstract class DOTransform<TValue, TPlugOptions> : DOTransform<TValue, TValue, TPlugOptions>
    where TPlugOptions : struct, IPlugOptions
    {}
}
