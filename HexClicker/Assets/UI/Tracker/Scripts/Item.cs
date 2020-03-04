using System;
using TMPro;
using UnityEngine;
using HexClicker.Quests;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HexClicker.UI.QuestTracker
{
    public class Item : MonoBehaviour
    {
        public TextMeshProUGUI questTitle;
        public Button questGoalPrefab;

        public void PostQuest(Quest quest)
        {
            questTitle.text = quest.name;
            Quest.Goal[] goals = quest.goals;

            for (int i = 0; i < goals.Length; i++)
            {
                string s = goals[i].body;
                Button goal = Instantiate(questGoalPrefab, transform);
                if (goals[i].hasCounter)
                {
                    s +=  goals[i].counter;
                    
                }

                if (goals[i].hasTimer)
                {
                    s +=   goals[i].timer.FormatElapsedTotalCent;
                   
                }

                if (goals[i].hasLocation)
                {
                    goal.interactable = true;
                }

                TextMeshProUGUI t = goal.GetComponentInChildren<TextMeshProUGUI>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(t.transform as RectTransform);
                t.text = s;
            }
        }
    }
}
