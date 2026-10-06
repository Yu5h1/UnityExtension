// Native integration tests shared by Unity Editor and Player test runners.
using System;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class MotionNativeTests
    {
        [Test] public void SharedReferenceFixtures()
        {
            var asset = Resources.Load<TextAsset>("MotionValidation/fixtures");
            Assert.IsNotNull(asset);
            Assert.AreEqual(779,MotionFixtureReplay.Run(JsonUtility.FromJson<MotionFixtureDocument>(asset.text)));
        }
        [Test] public void LifecycleAndInstanceIsolation()
        {
            using (var first = new MotionThrow2D())
            using (var second = new MotionThrow2D())
            {
                first.Launch(new Vector2(50,50),new Vector2(100,0));
                second.Launch(new Vector2(100,100),new Vector2(0,100));
                first.Stop();
                Assert.IsTrue(second.State.IsFlying);
                first.Dispose(); first.Dispose();
                Assert.Throws<ObjectDisposedException>(()=>first.Stop());
                Assert.Throws<ObjectDisposedException>(()=> { var value=first.State; });
            }
            using (var first = new MotionScatter2D())
            using (var second = new MotionScatter2D())
            {
                first.SetItems(new[]{1,2}); second.SetItems(new[]{1});
                first.Resolve(new Rect(0,0,1000,800)); second.Resolve(new Rect(0,0,1000,800));
                var saved=second.Positions[0].Position;
                first.Reset(); first.Resolve(new Rect(0,0,300,300));
                Assert.AreEqual(saved,second.Positions[0].Position);
                Assert.Throws<ArgumentException>(()=>first.SetItems(new[]{1,1}));
                Assert.AreEqual(2,first.Positions.Length);
                Assert.IsFalse(first.Pull(99,Vector2.zero,Vector2.one,100,5,.01f));
                first.Dispose(); first.Dispose();
                Assert.Throws<ObjectDisposedException>(()=>first.Reset());
                Assert.Throws<ObjectDisposedException>(()=> { var value=first.Positions; });
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MotionScatter2D(0));
        }
    }
}
