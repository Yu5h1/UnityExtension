using UnityEngine;


namespace Yu5h1Lib
{
	public class IntegersObject : ScriptableObject
	{
		[Dropdown("")]
		public int[] integers;

		public int Random() => integers.RandomElement();
    }

}