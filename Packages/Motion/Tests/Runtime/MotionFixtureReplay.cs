// Replays Motion-owned C# reference fixtures through the real native plugin; no numerical solver.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    [Serializable] public sealed class MotionFixtureDocument { public MotionFixtureSuite throw2d, scatter2d; }
    [Serializable] public sealed class MotionFixtureSuite { public string sourceSha256; public MotionFixtureCase[] cases; }
    [Serializable] public sealed class MotionFixtureCase { public string name; public MotionFixtureAction[] actions; }
    [Serializable] public sealed class MotionFixtureAction { public string op; public string[] args; public float[] expected; }

    /// <summary>Runs the same numerical contract in Editor, standalone Mono and IL2CPP Players.</summary>
    public static class MotionFixtureReplay
    {
        public static int Run(MotionFixtureDocument document)
        {
            int states = 0;
            foreach (var test in document.throw2d.cases)
            {
                MotionThrow2D body = null;
                try
                {
                    foreach (var action in test.actions)
                    {
                        float[] v = Array.ConvertAll(action.args, Number);
                        switch (action.op)
                        {
                            case "new": body?.Dispose(); body = new MotionThrow2D(); break;
                            case "launch": body.Launch(new Vector2(v[0],v[1]),new Vector2(v[2],v[3]),v[4]); break;
                            case "stop": body.Stop(); break;
                            case "tick":
                                body.Tick(v[0], new Rect(v[1],v[2],v[3],v[4]),v[5],v[9] == 0 ? (MotionThrowSettings?)null :
                                    new MotionThrowSettings { edgeBounce=v[6],drag=v[7],restingSpeed=v[8] }); break;
                            default: throw new InvalidOperationException("Unknown throw fixture action " + action.op);
                        }
                        var s = body.State;
                        Compare(test.name, new[] {s.Position.x,s.Position.y,s.Velocity.x,s.Velocity.y,s.Remaining,s.IsFlying?1:0,s.HasStruck?1:0}, action.expected, true);
                        states++;
                    }
                }
                finally { body?.Dispose(); }
            }
            foreach (var test in document.scatter2d.cases)
            {
                MotionScatter2D layout = null;
                try
                {
                    foreach (var action in test.actions)
                    {
                        float[] v = Array.ConvertAll(action.args, Number);
                        int result = -1;
                        switch (action.op)
                        {
                            case "new": layout?.Dispose(); layout = new MotionScatter2D(v[0]); break;
                            case "items": layout.SetItems(Array.ConvertAll(action.args, s => int.Parse(s,CultureInfo.InvariantCulture))); break;
                            case "obstacles":
                                var rects = new List<Rect>();
                                for (int i=0;i<v.Length;i+=4) rects.Add(new Rect(v[i],v[i+1],v[i+2],v[i+3]));
                                layout.SetObstacles(rects); break;
                            case "reset": layout.Reset(); break;
                            case "commit": layout.Commit(); break;
                            case "spread": layout.SetSpread(v[0]); break;
                            case "resolve": layout.Resolve(new Rect(v[0],v[1],v[2],v[3]),v[4],Origin(v,5)); break;
                            case "pull":
                            case "pull-live":
                                Vector2? current = new Vector2(v[1],v[2]);
                                if (action.op == "pull-live")
                                {
                                    current = null;
                                    foreach (var item in layout.Positions) if (item.Id == (int)v[0]) current = item.Position;
                                }
                                result = current.HasValue && layout.Pull((int)v[0],current.Value,new Vector2(v[3],v[4]),v[5],v[6],v[7],Origin(v,8)) ? 1 : 0;
                                break;
                            default: throw new InvalidOperationException("Unknown scatter fixture action " + action.op);
                        }
                        var actual = new List<float> {result};
                        foreach (var item in layout.Positions)
                        {
                            actual.Add(item.Position.HasValue ? 1 : 0);
                            actual.Add(item.Position?.x ?? 0); actual.Add(item.Position?.y ?? 0);
                        }
                        Compare(test.name,actual.ToArray(),action.expected,false);
                        states++;
                    }
                }
                finally { layout?.Dispose(); }
            }
            return states;
        }
        private static float Number(string value) => float.Parse(value,CultureInfo.InvariantCulture);
        private static Vector2? Origin(float[] v,int start) => v[start] == 0 ? (Vector2?)null : new Vector2(v[start+1],v[start+2]);
        private static void Compare(string name,float[] actual,float[] expected,bool flight)
        {
            if (actual.Length != expected.Length) throw new InvalidOperationException(name+": state length differs");
            for (int i=0;i<actual.Length;i++)
            {
                bool exact = flight ? i>=5 : i==0 || i%3==1;
                float tolerance = exact ? 0 : flight && i==4 ? 1e-5f : 1e-3f+Math.Abs(expected[i])*1e-5f;
                if (float.IsNaN(actual[i]) || float.IsInfinity(actual[i]) || Math.Abs(actual[i]-expected[i])>tolerance)
                    throw new InvalidOperationException($"{name}[{i}]: {actual[i]:R} != {expected[i]:R}");
            }
        }
    }
}
