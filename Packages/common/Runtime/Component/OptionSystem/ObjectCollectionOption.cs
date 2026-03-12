using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
	public class ObjectCollectionOption : OptionSet<ObjectCollection>
	{
		public void	SetActive(int index)
		{
			for (int i = 0; i < Count; i++)
			{
				var item = Items[i];
				var enableed = i == index;

                foreach (var obj in item.value)
                {
					switch (obj)
					{
						case GameObject gobj:
                            gobj.SetActive(enableed);
							break;
                        case Behaviour behaviour:
                            behaviour.enabled = enableed;
                            break;
                    }
                }
			}
		}
	} 
}
