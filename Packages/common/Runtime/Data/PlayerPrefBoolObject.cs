using UnityEngine;

namespace Yu5h1Lib
{
    public class PlayerPrefBoolObject : PlayerPrefObject<ObservablePref<bool>>
    {
        public bool Value
        {
            get => data.Value;
            set => data.Value = value;
        }
    } 
}
