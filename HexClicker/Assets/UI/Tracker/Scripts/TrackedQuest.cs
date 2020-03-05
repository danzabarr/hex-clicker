using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HexClicker.UI.QuestTracker
{
    public class TrackedQuest : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI body;

        private string[] objectives;
        
        /// <summary>
        /// Title of the quest
        /// </summary>
        public string Title
        {
            get => title.text;
            set => title.text = value;
        }

        /// <summary>
        /// Get an objective for this quest
        /// </summary>
        public string GetObjective(int index)
        {
            if (index < 0 || index >= objectives.Length)
                return null;
            return objectives[index];
        }

        /// <summary>
        /// Set the objective at a given index
        /// </summary>
        public bool SetObjective(int index, string objective)
        {
            if (index < 0 || index >= objectives.Length)
                return false;
            objectives[index] = objective;
            body.text = string.Join("\n", objectives);
            return true;
        }

        /// <summary>
        /// Set the objectives for this quest
        /// </summary>
        public void SetObjectives(string[] objectives)
        {
            this.objectives = objectives;
            body.text = string.Join("\n", objectives);
        }

        /// <summary>
        /// Set the action to be called when this quest is clicked on in the tracker.
        /// </summary>
        public void SetOnClickAction(UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(action);
        }
    }
}
