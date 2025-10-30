using UnityEngine;


namespace Yu5h1Lib
{
	public class PlayerPrefBoolControl : MonoBehaviour
	{
		public ObservableBoolPref data;
		public bool Value
        {
			get => data.Value;
			set => data.Value = value;
        }
        private void Start()
        {
            data.Init();
        }
    } 
}
