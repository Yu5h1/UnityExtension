using UnityEngine;

namespace Yu5h1Lib
{
    [System.Serializable]
	public struct BinaryStateColor
	{
        public Color active;
        public Color inactive;
        public Color Evaluate(bool state) => state ? active : inactive;

        public static readonly BinaryStateColor Default = new BinaryStateColor
            {
                active = Color.white,
                inactive = Color.gray
            };
    } 
}
