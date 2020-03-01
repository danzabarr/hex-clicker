using System;
using TMPro;
using UnityEngine;
using HexClicker.Quests;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Serialization;

namespace HexClicker.UI.QuestTracker
{
    public class Item : MonoBehaviour
    {
        private Quest _data;

       // [SerializeField] private Dialog questPopUp;
        [SerializeField] private TextMeshProUGUI trackerText;
        //[SerializeField] private TextMeshProUGUI questInfo;

        private float TotalTime { get; set; }
        public float elapsedTime = 0;
        public float RemainingTime => TotalTime - elapsedTime;
        private float FractionTime => elapsedTime / TotalTime;
        public float CentTime => FractionTime * 100f;
        private bool questActive;
        private Timer timer = new Timer();

        void Update()
        {
            if (!questActive) return;
            trackerText.text = _data.name + " : " + FormatTimeMSP(elapsedTime += Time.deltaTime, _data.timeLimit);
            
            // A bunch of this code will be heavily refactored, since atm I'm not instantiating a prefab.
            if (!(elapsedTime >= _data.timeLimit) || _data.status != Quest.Status.Active) return;
            questActive = false;
            _data.FailedQuest();
            trackerText.text = "";
            elapsedTime = 0;
            Manager.Instance.RemoveQuest(_data);
        }
        
        public void QuestAccepted(Quest quest)
        {
            _data = quest;

            TotalTime = _data.timeLimit;
            questActive = true;
        }

        public static string FormatTimeMSP(float elapsed, float total)
        {
            TimeSpan e = System.TimeSpan.FromSeconds(elapsed);
            TimeSpan t = System.TimeSpan.FromSeconds(total);
            return
                $"{(int) e.TotalMinutes:00}:{e.Seconds:00}/{(int) t.TotalMinutes:00}:{t.Seconds:00} ({elapsed / total:p0})";
        }        
    }
    
   
}
