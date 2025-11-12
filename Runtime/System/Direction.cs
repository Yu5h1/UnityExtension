using Flags = System.FlagsAttribute;
using UnityEngine;

namespace Yu5h1Lib
{
    [Flags]
    public enum Direction2D : int
    {
        none         = 0,
        up           = 1 << 0,
        down         = 1 << 1,
        left         = 1 << 2,
        right        = 1 << 3,
    }
    [Flags]
    public enum Direction : int
    {
        none        = 0,
        up          = Direction2D.up,
        down        = Direction2D.down,
        left        = Direction2D.left,
        right       = Direction2D.right,
        forward     = 1 << 4,
        backward    = 1 << 5,
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class DirectionExtension{
        public static Vector3 ToVector3(this Direction direction)
        {
            switch (direction)
            {
                case Direction.forward: return Vector3.forward;
                case Direction.forward | Direction.left: return Vector3.forward + Vector3.left;
                case Direction.forward | Direction.right: return Vector3.forward + Vector3.right;
                case Direction.forward | Direction.up: return Vector3.forward + Vector3.up;
                case Direction.forward | Direction.down: return Vector3.forward + Vector3.down;
                case Direction.forward | Direction.left | Direction.up: return Vector3.forward + Vector3.left + Vector3.up;
                case Direction.forward | Direction.left | Direction.down: return Vector3.forward + Vector3.left + Vector3.down;
                case Direction.forward | Direction.right | Direction.up: return Vector3.forward + Vector3.right + Vector3.up;
                case Direction.forward | Direction.right | Direction.down: return Vector3.forward + Vector3.right + Vector3.down;
                case Direction.backward: return Vector3.back;
                case Direction.backward | Direction.left: return Vector3.back + Vector3.left;
                case Direction.backward | Direction.right: return Vector3.back + Vector3.right;
                case Direction.backward | Direction.up: return Vector3.back + Vector3.up;
                case Direction.backward | Direction.down: return Vector3.back + Vector3.down;
                case Direction.backward | Direction.left | Direction.up: return Vector3.back + Vector3.left + Vector3.up;
                case Direction.backward | Direction.left | Direction.down: return Vector3.back + Vector3.left + Vector3.down;
                case Direction.backward | Direction.right | Direction.up: return Vector3.back + Vector3.right + Vector3.up;
                case Direction.backward | Direction.right | Direction.down: return Vector3.back + Vector3.right + Vector3.down;
                case Direction.left: return Vector3.left;
                case Direction.left | Direction.up: return Vector3.left + Vector3.up;
                case Direction.left | Direction.down: return Vector3.left + Vector3.down;
                case Direction.right: return Vector3.right + Vector3.down;
                case Direction.right | Direction.up: return Vector3.right + Vector3.up;
                case Direction.right | Direction.down: return Vector3.right + Vector3.down;
                case Direction.up: return Vector3.up;
                case Direction.down: return Vector3.down;
            }
            return Vector3.zero;
        }
        //⇇ ⇉ ⇈ ⇊ , ⇆ ⇄ ⇅ ⇵
        public static string ToSymbol(this Direction dir)
        {
            switch (dir)
            {
                case Direction.forward: return "↑";
                case Direction.forward | Direction.left: return "↖";
                case Direction.forward | Direction.right: return "↗";
                case Direction.forward | Direction.up: return "⇑";
                case Direction.forward | Direction.down: return "▲";
                case Direction.forward | Direction.left | Direction.up: return "⇖";
                case Direction.forward | Direction.left | Direction.down: return "⇖";
                case Direction.forward | Direction.right | Direction.up: return "⇗";
                case Direction.forward | Direction.right | Direction.down: return "⇗";
                case Direction.backward: return "↓";
                case Direction.backward | Direction.left: return "↙";
                case Direction.backward | Direction.right: return "↘";
                case Direction.backward | Direction.up: return "⇓";
                case Direction.backward | Direction.down: return "▼";
                case Direction.backward | Direction.left | Direction.up: return "⇙";
                case Direction.backward | Direction.left | Direction.down: return "⇙";
                case Direction.backward | Direction.right | Direction.up: return "⇘";
                case Direction.backward | Direction.right | Direction.down: return "⇘";
                case Direction.left: return "←";
                case Direction.left | Direction.up: return "⇐";
                case Direction.left | Direction.down: return "↼";
                case Direction.right: return "→";
                case Direction.right | Direction.up: return "⇒";
                case Direction.right | Direction.down: return "⇀";
                case Direction.up: return "⇧";
                case Direction.down: return "⇩";
            }
            return "⃝";
        }
        public static Direction ToDirectionApproximately(this Vector3 dir)
        {
            Vector3 result = Vector3.zero;
            float distance = Vector3.Distance(Vector3.up, Vector3.forward);
            var dirVectors = new Vector3[] {
                                                Vector3.forward,
                                                Vector3.back,
                                                Vector3.left,
                                                Vector3.right,
                                                Vector3.up,
                                                Vector3.down,
                                            };
            foreach (var orientation in dirVectors)
            {
                float curdis = Vector3.Distance(dir, orientation);
                if (curdis < distance)
                {
                    result = orientation;
                    distance = curdis;
                }
            }
            return result.ToDirection();
        }
    }
}
