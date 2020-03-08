using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options 
{
    public class OptionSlider : MonoBehaviour
    {
        [SerializeField] private new string name;
        [SerializeField] private Slider slider;

        private void Awake()
        {
            slider.value = PlayerPrefs.GetFloat(name, slider.value);
        }
        
        public void UpdateValue()
        {
            PlayerPrefs.SetFloat(name, slider.value);
        }
    }
}
