using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HexClicker.UI.QuestTracker
{
    public class Tracker : MonoBehaviour
    {
        [SerializeField] private TrackedQuest questPrefab;
        private Dictionary<string, TrackedQuest> trackedQuests = new Dictionary<string, TrackedQuest>();

        /// <summary>
        /// Displays a quest in the tracker with the supplied title and objectives.
        /// Clicking the quest in the tracker will call the onClick action.
        /// </summary>
        public bool TrackQuest(string title, UnityAction onClick, string[] objectives)
        {
            if (title == null)
            {
                Debug.LogWarning("Failed to track quest because the title is null.");
                return false;
            }

            if (trackedQuests.ContainsKey(title))
            {
                Debug.LogWarning("Failed to track quest: '" + title + "' because it is already being tracked. Use UpdateQuest to update a quest's objectives.");
                return false;
            }

            TrackedQuest quest = Instantiate(questPrefab, transform);
            quest.Title = title;
            quest.SetObjectives(objectives);
            quest.SetOnClickAction(onClick);
            trackedQuests[title] = quest;
            Layout();
            return true;
        }

        /// <summary>
        /// Remove the quest of the given title from the tracker.
        /// </summary>
        public bool StopTrackingQuest(string title)
        {
            if (trackedQuests.TryGetValue(title, out TrackedQuest quest))
            {
                trackedQuests.Remove(title);
                Destroy(quest.gameObject);
                Layout();
                return true;
            }
            Debug.LogWarning("Failed to stop tracking quest: '" + title + "' because it doesn't exist.");
            return false;
        }

        /// <summary>
        /// If it exists, update the objectives of the quest of the given title.
        /// </summary>
        public bool UpdateQuest(string title, string[] objectives)
        {
            if (trackedQuests.TryGetValue(title, out TrackedQuest quest))
            {
                quest.SetObjectives(objectives);
                Layout();
                return true;
            }
            Debug.LogWarning("Failed to update the quest: '" + title + "', with new objectives: " + string.Join(", ", objectives));
            return false;
        }

        /// <summary>
        /// If it exists, update a single objective of the quest of the given title at a certain index.
        /// </summary>
        public bool UpdateQuest(string title, int index, string objective)
        {
            if (trackedQuests.TryGetValue(title, out TrackedQuest quest)
                && quest.SetObjective(index, objective))
            {
                Layout();
                return true;
            }
            Debug.LogWarning("Failed to update the quest: '" + title + "', with objective: '" + objective + "' at index: " + index);
            return false;
        }

        private void Layout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        #region Testing

        

        private Timer timer = new Timer(0, 60);
        public void Start()
        {

            //  Example usage of TrackQuest(string title, UnityAction onClick, string[] objectives);
            //  
            //  The UnityAction onClick is passed a lambda expression that just logs the title to the console when the button is clicked.
            //  
            //  The objectives are passed in to the tracker pre-formatted.
            //  There are various methods of creating formatted strings at the end of this file. Keep reading.
            //

            string title = "It's a Family Affair";
            TrackQuest(title, () => Debug.Log(title), new string[]
            {
                //You can always include unformatted strings, like the one below.
                "You want to kill everyone in the family, because you're a psychopath.\nDon't let them block your escape routes!\n",
                FormatObjective("Kill Bob"),
                FormatObjectiveFailed("Kill Susan"),
                FormatCounter("Kill the Children", new Counter(2, 2)),
                FormatObjectiveCompleted("Kill Grandma"),
                FormatCounter("Kill the Guinea Pigs", new Counter(1, 5)),
                FormatCounterFailable("Escape Routes Blocked", new Counter(3, 3)),
                FormatCountdownFailable("Time Remaining", new Timer(0, 60))
            });
        }

        public void Update()
        {
            timer += Time.deltaTime;
            UpdateQuest("It's a Family Affair", 7, FormatCountdownFailable("Time Remaining", timer));
        }


        //Adds a quest with a random title when the button is pressed.
        [ContextMenu("Track New")]
        public void TrackNewTest()
        {
            string title = RandomQuestTitle();

            TrackQuest(title, () => Debug.Log(title), new string[]
            {
                FormatCounter("Collect Firewood", new Counter(0, 10)),
                FormatCounter("Chop Onions", new Counter(2, 2)),
                FormatCounter("Feed Cats", new Counter(0, 28)),
            });
        }

        private static string RandomQuestTitle()
        {
            if (QuestTitles.Count <= 0)
                return null;
            int r = Random.Range(0, QuestTitles.Count);
            string title = QuestTitles[r];
            QuestTitles.RemoveAt(r);
            return title;
        }

        private static List<string> QuestTitles = new List<string>
        {
            "How to Train Your Succubus",
            "Save Little Timmy",
            "Someone Stole The School Turtle",
            "Toilet Paper Origami",
            "Outwitting Squirrels",
            "Talking with Trees",
            "Old Tractors and the Men Who Love Them",
            "Wizard Porn",
            "Zombie Raccoons and Killer Bunnies",
            "Summer with the Leprechauns",
            "Shitting in the Woods",
            "Stray Shopping Carts"
        };

        #endregion

        //  The following are some methods of creating formatted strings for the tracker.
        #region Formatting

        /// <summary>
        /// Formats an objective with a checkbox.
        /// </summary>
        public static string FormatObjective(string label)
        {
            return "<sprite name=\"checkbox_empty\"><indent=1.2em>" + label + "</indent>";
        }

        /// <summary>
        /// Formats an objective with a ticked checkbox that displays 'Completed'.
        /// </summary>
        public static string FormatObjectiveCompleted(string label)
        {
            return "<sprite name=\"checkbox_tick\" color=#00FF00><indent=1.2em><color=#00FF00>" + label + " (Completed)</color></indent>";
        }

        /// <summary>
        /// Formats an objective with a crossed checkbox that displays 'Failed'.
        /// </summary>
        public static string FormatObjectiveFailed(string label)
        {
            return "<sprite name=\"checkbox_cross\" color=#FF0000><indent=1.2em><color=#FF0000>" + label + " (Failed)</color></indent>";
        }

        /// <summary>
        /// Formats a counter with a checkbox that displays 'Completed' when the counter is at its max.
        /// </summary>
        public static string FormatCounter(string label, Counter counter)
        {
            if (counter.Maxed)
                return "<sprite name=\"checkbox_tick\" color=#00FF00><indent=1.2em><color=#00FF00>" + label + ": " + counter.Amount + "/" + counter.Max + " (Completed)</color></indent>";
            else
                return "<sprite name=\"checkbox_empty\"><indent=1.2em>" + label + ": " + counter.Amount + "/" + counter.Max + "</indent>";
        }

        /// <summary>
        /// Formats a counter that displays 'Failed' when the counter is at its max.
        /// </summary>
        public static string FormatCounterFailable(string label, Counter counter)
        {
            if (counter.Maxed)
                return "<sprite name=\"checkbox_cross\" color=#FF0000><indent=1.2em><color=#FF0000>" + label + ": " + counter.Amount + "/" + counter.Max + " (Failed)</color></indent>";
            else
                return "<sprite name=\"checkbox_empty\"><indent=1.2em>" + label + ": " + counter.Amount + "/" + counter.Max + "</indent>";
        }

        /// <summary>
        /// Formats a timer that displays 'Completed' when the timer has completely elapsed.
        /// </summary>
        public static string FormatTimerCompletable(string label, Timer timer)
        {
            if (timer.Completed)
                return "<indent=1.2em><color=#00FF00>" + label + ": " + timer.FormatTimer + " (Completed)</color></indent>";
            else 
                return "<indent=1.2em>" + label + ": " + timer.FormatTimer + "</indent>";
        }

        /// <summary>
        /// Formats a timer that displays 'Failed' when the timer has completely elapsed.
        /// </summary>
        public static string FormatTimerFailable(string label, Timer timer)
        {
            if (timer.Completed)
                return "<indent=1.2em><color=#FF0000>" + label + ": " + timer.FormatTimer + " (Failed)</color></indent>";
            else
                return "<indent=1.2em>" + label + ": " + timer.FormatTimer + "</indent>";
        }

        /// <summary>
        /// Formats a countdown that displays 'Completed' when the countdown has ended.
        /// </summary>
        public static string FormatCountdownCompletable(string label, Timer timer)
        {
            if (timer.Completed)
                return "<indent=1.2em><color=#00FF00>" + label + ": " + timer.FormatCountdown + " (Completed)</color></indent>";
            else
                return "<indent=1.2em>" + label + ": " + timer.FormatCountdown + "</indent>";
        }

        /// <summary>
        /// Formats a countdown that displays 'Failed' when the countdown has ended.
        /// </summary>
        public static string FormatCountdownFailable(string label, Timer timer)
        {
            if (timer.Completed)
                return "<indent=1.2em><color=#FF0000>" + label + ": " + timer.FormatCountdown + " (Failed)</color></indent>";
            else
                return "<indent=1.2em>" + label + ": " + timer.FormatCountdown + "</indent>";
        }

        #endregion
    }
}