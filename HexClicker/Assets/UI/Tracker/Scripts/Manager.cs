using HexClicker.Quests;
using System;
using System.Collections.Generic;
using HexClicker.UI.Notifications;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace HexClicker.UI.QuestTracker
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance { get; set; }
        public Item questItemPrefab;
        public TextMeshProUGUI titleText;
        [SerializeField] private int maxNumOfDisplayedQuests;
        [SerializeField] private List<Quest> quests;
        private Counter counter;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }

            counter = new Counter(quests.Count, maxNumOfDisplayedQuests);
            titleText.text = "Number of quests: " + counter;
        }


        public void TestQuest(Quest quest)
        {
            if (quests.Count >= maxNumOfDisplayedQuests)
            {
                NotificationSystem.Instance.Post(title: "Too many quests", "There are too many quests active!"); 
                return;
            }
            else
            {
                quests.Add(quest);
                Item newQuest = Instantiate(questItemPrefab, transform);
                newQuest.PostQuest(quest);
            }
        }

    }

   
}
