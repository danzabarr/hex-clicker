using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/*[CustomEditor(typeof(ComponentWithAButton))]
public class ComponentWithAButton : Editor
{
    private ComponentWithAButton instace;

    private void OnEnable()
    {
        instace = (ComponentWithAButton) target;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("ButtonLabel"))
        {
            instace.DoSomething();
        }

        DrawDefaultInspector();
    }

    private void DoSomething()
    {
        Debug.Log("Doing Something");
    }
}
[CustomEditor(typeof(ComponentWithAButton))]*/
namespace HexClicker.UI.Options 
{
    public class OptionSlider : MonoBehaviour
    {
        [SerializeField]private new string name;
        [SerializeField] private Slider slider;
        [SerializeField] private UnityEvent onValueChanged;


        private void Awake()
        {
            slider.value = PlayerPrefs.GetFloat(name, slider.value);
        }
        
        public void UpdateValue()
        {
            PlayerPrefs.SetFloat(name, slider.value);
        }

        private void OnEnable()
        {
            Debug.Log(PlayerPrefs.HasKey(name));
            Debug.Log(PlayerPrefs.GetFloat(name));
        }
    }
}
