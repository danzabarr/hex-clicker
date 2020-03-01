using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HexClicker.UI.Notifications
{
    [RequireComponent(typeof(RectTransform))]
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance { get; private set; }
    
        [SerializeField] private Notification notificationPrefab;
        [SerializeField] private Vector2 padding;
        [SerializeField] private float spacing;
        [SerializeField] private int maxNotificationsDisplayed;

        private int _count;
        private readonly List<Notification> _notifications = new List<Notification>();

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
        }

        public void Remove(Notification n)
        {
            _notifications.Remove(n);
            Layout();
        }

        private Notification Post(Notification.Data data)
        {
            Notification n = Instantiate(notificationPrefab, transform);
            data.id = _count;
            n.SetNotification(data);
            _notifications.Add(n);
            _count++;
            n.transform.localPosition = new Vector3(padding.x, (n.transform as RectTransform).rect.height + 200);

            LayoutRebuilder.ForceRebuildLayoutImmediate(n.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(n.transform as RectTransform);
            Layout();

            return n;
        }

        private void Layout()
        {
            float x = padding.x;
            float y = -padding.y;

            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                Notification n = _notifications[i];
                RectTransform t = n.transform as RectTransform;

                n.Layout(new Vector3(x, y), false);

                if (i >= _notifications.Count - maxNotificationsDisplayed)
                {
                    n.gameObject.SetActive(true);
                    n.StartFadeIn();
                    y -= t.rect.height + spacing;
                }
                else if (n.gameObject.activeSelf)
                {
                    n.StartFadeOut(false, true);
                }
            }
        }

        private static string RandomText()
        {
            string s = "\n";
            int lines = Random.Range(1, 5);
            for (int i = 0; i < lines; i++)
            {

                s += "Line line line\n";
            }

            return s;
        }

        public void ClearAll()
        {
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                if (!_notifications[i].gameObject.activeSelf)
                {
                    _notifications[i].gameObject.SetActive(true); 
                }               
                _notifications[i].Close();
            }
            
        }


        [ContextMenu("Test Post")]
        public void TestPost()
        {
            Post(new Notification.Data()
            {
                title = "TEST",
                body = "Notification #" + _count + RandomText(),
            });
        }

        public Notification Post(string title, string body)
        {
            return Post(new Notification.Data()
            {
                title = title,
                body = body
            });
        }

        public Notification Post(string title, string body, Vector3 location)
        {
            return Post(new Notification.Data()
            {
                title = title,
                body = body,
                location = location,
                hasLocation = true
            });
        }
    }
}
