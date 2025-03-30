using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    public class TweenLoadAsync : LoadAsyncBehaviour
    {
        public TweenBehaviour[] tweeners { get; private set; }

        protected override void OnInitializing()
        {
        }

        protected void OnEnable()
        {
            //tweeners = GetComponentsInChildren<TweenBehaviour>();
            //foreach (var t in tweeners)
            //{
            //    t.Kill();
            //    t.Init();
            //    t.tweener.SetUpdate(true).SetAutoKill(false);
            //    t.tweener.PlayForward();
            //}

          
        }
        public override void OnProcessing(float percentage)
        {
            
        }


    }
}
