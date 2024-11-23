
using System;

namespace Yu5h1Lib.Runtime
{
    [Serializable]
    public struct MinMax
    {
        public float Min;
        public float Max;

        public float Length => Math.Abs(Max - Min);

        public MinMax(float min,float max)
        {
            Min = min;
            Max = max;
        }
        
    }
}
