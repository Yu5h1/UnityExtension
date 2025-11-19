using UnityEngine;

namespace Yu5h1Lib
{
	public class ShowDetailAttribute : PropertyAttribute 
	{
		public bool Minimize = false;

		public ShowDetailAttribute() {}

        public ShowDetailAttribute(bool minimize) 
		{
			Minimize = minimize;
        }

    } 
}
