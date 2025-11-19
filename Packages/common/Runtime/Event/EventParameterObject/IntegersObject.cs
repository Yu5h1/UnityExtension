using UnityEngine;
using UnityEngine.Serialization;


namespace Yu5h1Lib
{
	public class IntegersObject : InlineParamterObject
    {
		[Dropdown("")]
		public int[] value;

		public int Random() => value.RandomElement();		
    }

}