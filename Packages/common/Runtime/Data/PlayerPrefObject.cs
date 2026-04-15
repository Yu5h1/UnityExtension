using UnityEngine;

namespace Yu5h1Lib
{
	public abstract class PlayerPrefObject<T> : ScriptableObject where T : ObservableValue
    {
		public T data;
        public void Init() => data.Init();
    } 
}
