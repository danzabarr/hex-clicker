using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quest")]
    public class Quest : ScriptableObject // This quest script is a place holder for a full fledged out quest system
    {
        public enum Status
        {
            Requested,
            Rejected,
            Active,
            Completed,
            Failed
        }

        public enum Type
        {
            Gathering,
            Fetching,
            Fighting,
            Building,
            Growing,
            Boss
        }
        

        public int id;
        public Vector3 location;
        public float timeLimit;
        public string body;
        public bool hasLocation;
        public bool isTimeDependent;
        public Status status;
        public Type type;
        
        
        public string reward; //This string is currently a place holder for an eventual reward system

        public void FailedQuest()
        {
            
            UI.Notifications.NotificationSystem.Instance.Post("Quest Failed", "You've failed the following quest: " + name);
            status = Status.Failed;
            //Destroy(this);
        }

        public void CompletedQuest()
        {
            status = Status.Completed;
            Debug.Log(reward);
           // Destroy(this);
        }

        public void AcceptedQuest()
        {
            UI.Notifications.NotificationSystem.Instance.Post("Quest Accepted", "You've started a new quest: " + name);
            status = Status.Active;
        }

        public void RejectedQuest()
        {
            status = Status.Rejected;
            UI.Notifications.NotificationSystem.Instance.Post("Quest Rejected", "You've rejected the following quest: " + name);
            //Destroy(this);
        }
    }
}
