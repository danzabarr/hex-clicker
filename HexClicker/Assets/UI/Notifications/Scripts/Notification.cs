using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HexClicker.UI.Notifications
{
    [RequireComponent(typeof(CanvasFader))]
    public class Notification : MonoBehaviour, IPointerClickHandler 
    {
        public struct Data
        {
            public int id;
            public Vector3 location;
            public string title, body;
            public bool hasLocation;
        }

        private Data _data;

        [SerializeField] private TextMeshProUGUI textTitle;
        [SerializeField] private TextMeshProUGUI textBody;

        private CanvasFader canvasFader;
        private Vector3 _targetPosition;

        public void Layout(Vector3 targetPosition, bool immediate)
        {
            this._targetPosition = targetPosition;
            if (immediate)
                transform.localPosition = targetPosition;
        }

        private void Awake()
        {
            canvasFader = GetComponent<CanvasFader>();
        }

        private void Start()
        {
            canvasFader.Alpha = 0;
            canvasFader.StartFadeIn();
        }

        private void Update()
        {
            const float speed = 10;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetPosition, Time.deltaTime * speed);
        }

        public void Close()
        {
            NotificationSystem.Instance.Remove(this);
            StartFadeOut(true);
        }

        private void GoToLocation()
        {
            if (!_data.hasLocation)
                return;

            Debug.Log("This method should take you to the following location: " + _data.location);
        }

        public void SetNotification(Data data)
        {
            this._data = data;
            textTitle.text = data.title;
            textBody.text = data.body;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                GoToLocation();
            else if (eventData.button == PointerEventData.InputButton.Right) Close();
        }

        public void StartFadeIn() => canvasFader.StartFadeIn();
        public void StartFadeOut(bool destroy = false, bool deactivate = false) => canvasFader.StartFadeOut(destroy, deactivate);
    }
}
