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
                return false;

            if (trackedQuests.ContainsKey(title))
                return false;

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
            return false;
        }

        /// <summary>
        /// If it exists, update a single objective of the quest of the given title at a certain index.
        /// </summary>
        public bool UpdateQuest(string title, int index, string objective)
        {
            if (trackedQuests.TryGetValue(title, out TrackedQuest quest))
            {
                quest.SetObjective(index, objective);
                Layout();
                return true;
            }
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
                FormatObjective("Kill Bob", 4),
                FormatObjective("Kill Susan", Color.red, 2),
                FormatCounter("Kill the Children", new Counter(2, 2), Color.green, 1),
                FormatCountDown("Time Remaining", new Timer(0, 60))
            });
        }

        public void Update()
        {
            timer += Time.deltaTime;
            UpdateQuest("It's a Family Affair", 3, FormatCountDown("Time Remaining", timer, -1));
        }


        //Adds a quest with a random title when the button is pressed.
        [ContextMenu("Track New")]
        public void TrackNewTest()
        {
            string title = RandomQuestTitle();

            TrackQuest(title, () => Debug.Log(title), new string[]
            {
                FormatCounter("Collect Firewood", new Counter(0, 10)),
                FormatCounter("Chop Onions", new Counter(0, 2)),
                FormatCounter("Feed Cats", new Counter(0, 28)),
            });
        }
        private static string RandomQuestTitle()
        {
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

        #region Formatting
        //
        //  The following are some methods of creating formatted strings for the tracker.
        //  Strings should indent 1.2em to make them look nice.
        //  The indent can contain a sprite, intended for a checkbox or highlight or some other icon.
        //
        //  <sprite=0><indent=1.2em>...</indent>
        //

        public static string FormatObjective(string label, Color color, int sprite = -1)
        {
            string formattedString = "<indent=1.2em><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + label + "</color></indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatCounter(string label, Counter counter, Color color, int sprite = -1)
        {
            string formattedString = "<indent=1.2em><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + label + ": " + counter.Amount + "/" + counter.Max + "</color></indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatTimer(string label, Timer timer, Color color, int sprite = -1)
        {
            string formattedString = "<indent=1.2em><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + label + ": " + timer.FormatElapsedTotalCent + "</color></indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatCountDown(string label, Timer timer, Color color, int sprite = -1)
        {
            string formattedString = "<indent=1.2em><color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + label + ": " + timer.FormatRemaining + "</color></indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatObjective(string label, int sprite = -1)
        {
            string formattedString = "<indent=1.2em>" + label + "</indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatCounter(string label, Counter counter, int sprite = -1)
        {
            string formattedString = "<indent=1.2em>" + label + ": " + counter.Amount + "/" + counter.Max + "</indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatTimer(string label, Timer timer, int sprite = -1)
        {
            string formattedString = "<indent=1.2em>" + label + ": " + timer.FormatElapsedTotalCent + "</indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        public static string FormatCountDown(string label, Timer timer, int sprite = -1)
        {
            string formattedString = "<indent=1.2em>" + label + ": " + timer.FormatRemaining + "</indent>";
            if (sprite >= 0)
                return "<sprite=" + sprite + ">" + formattedString;
            return formattedString;
        }

        #endregion
    }
}