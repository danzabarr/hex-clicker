using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options 
{
    public class OptionSlider : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Slider slider;

        private void Awake()
        {
            slider.value = PlayerPrefs.GetFloat(key, slider.value);
        }
        
        public void UpdateValue()
        {
            PlayerPrefs.SetFloat(key, slider.value);
        }
    }
}
