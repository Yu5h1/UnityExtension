using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
	public class RendererAddon : MonoBehaviour
	{
		[SerializeField]
		private Renderer _renderer;
		[SerializeField]
		private int current;

		public void MoveNext(MaterialSequence materialSequence)
		{
			//materialSequence.MoveNext(_renderer,ref current);
			materialSequence.MoveNext(_renderer);
		}
	} 
}
