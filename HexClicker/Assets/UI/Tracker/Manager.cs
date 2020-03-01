using HexClicker.Quests;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace HexClicker.UI.QuestTracker
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance { get; set; }

        [SerializeField] private Dialog questDialog;
        [SerializeField] private Item trackerItemPrefab; //ATM it isn't using an actual prefab
        [SerializeField] private List<Quest> quests = new List<Quest>();
        [SerializeField] private TextMeshProUGUI numberOfQuests;

        [SerializeField] private int maxNumberOfQuests = 1;
      

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
            UpdateNumberActiveQuests();
        }

        // A bunch of this code will be heavily refactored, since atm I'm not instantiating a prefab.
        public void DisplayQuestDialog(Quest quest)
        {
            if (quests.Count >= maxNumberOfQuests)
            {
                Notifications.NotificationSystem.Instance.Post("Max Number of Quest Reached",
                    "You've reached the max number of quests, either complete one or cancel one to receive more.");
                return;
            }
                
            quest.status = Quest.Status.Requested;

            questDialog.Display("Quest: " + quest.name, quest.body, false,

                "Reject", () =>
                {
                    quest.RejectedQuest();
                    Time.timeScale = 1;
                },

                "Accept", () =>
                {
                    Time.timeScale = 1;
                    quest.AcceptedQuest();
                    quests.Add(quest);
                    UpdateNumberActiveQuests();
                    trackerItemPrefab.QuestAccepted(quest);
                }
                
            );

            Time.timeScale = 0;
        }

        private void UpdateNumberActiveQuests()
        {
            if (numberOfQuests != null)
            {
                numberOfQuests.text = "Number of active quests: " + quests.Count + "/" + maxNumberOfQuests;
            }
            else
            {
                Debug.Log("There is a problem with the quest tracker's number of active quests counter!");
            }
        }

        public void RemoveQuest(Quest quest)
        {
            quests.Remove(quest);
            UpdateNumberActiveQuests();
        }
        
        /* public void AcceptQuest(Quest quest)
         {
             quests.Add(quest);
             HexClicker.UI.Notifications.NotificationSystem.Instance.Post("Quest Accepted", "You've started a new quest: " + quest.name);
         }*/
    }

   
}
