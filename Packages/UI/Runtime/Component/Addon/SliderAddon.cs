using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Slider)),AddonFor(typeof(Slider))]
    public class SliderAddon : UI_Adapter<IValuePortAdapter<float>> 
    {
    
    }
}