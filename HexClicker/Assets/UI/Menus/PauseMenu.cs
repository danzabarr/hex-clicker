using UnityEngine;
using UnityEngine.Serialization;

namespace HexClicker.UI.Menus
{
    public class PauseMenu : MonoBehaviour
    {
        public GameObject pauseMenu;
        [SerializeField] private CanvasFader canvasFader;

        public void InGamePauseClick()
        {
            pauseMenu.SetActive(true);
            canvasFader.StartFadeIn();
            Time.timeScale = 0;
        }

        public void ReturnClick()
        {
            canvasFader.StartFadeOut(false, true);
            Time.timeScale = 1;
        }

        public void SaveClick()
        {
            Debug.Log("Game is being saved!");
        }

        public void LoadClick()
        {
            Debug.Log("This will display a series of save files!");
        }

        public void SettingsClick()
        {
            Debug.Log("This will take you to a settings menu!");
        }

        public void MainMenuClick()
        {
            Debug.Log("This will either take you to the main menu or make a pop up");
        }

        public void QuitGameClick()
        {
            Debug.Log("This will either quit the application or generate a pop-up");
        }
    }
}
