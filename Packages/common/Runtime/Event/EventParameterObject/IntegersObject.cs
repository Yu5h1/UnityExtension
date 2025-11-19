using UnityEngine;


namespace Yu5h1Lib
{
	public class IntegersObject : InlineScriptableObject
    {
		[Dropdown("")]
		public int[] integers;

		public int Random() => integers.RandomElement();
    }

}