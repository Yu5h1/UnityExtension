using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib;

[RequireComponent(typeof(Slider))]
public class SliderAide : MonoBehaviour
{
    [SerializeField,ReadOnly]
    private Slider slider;

    private void Reset()
        => TryGetComponent(out slider);
    private void Start()
    {
        this.GetComponent(ref slider);
    }
    public void UpdateText(Text text)
    {
        if ($"{name} Slider does not exist".printWarningIf(!slider))
            return;
        text.text = (slider.value * 100.0f).ToString("0.#") + "%";
    }
}
