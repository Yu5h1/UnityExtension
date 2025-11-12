
using System;
using Yu5h1Lib.Mathematics;

namespace Yu5h1Lib.Runtime
{
    [Serializable]
    public struct MinMax
    {
        public enum Option { Min, Max }

        public float Min;
        public float Max;

        public float Length => Math.Abs(Max - Min);

        public MinMax(float min,float max)
        {
            Min = min;
            Max = max;
        }
        public float GetNormal(float val) => Length == 0 ? 0f : (val - Min) / Length;
        public bool Contains(float val) => val >= Min && val <= Max;        
        public float Clamp(float val) => val.Clamp(Min, Max);        
    }
}
