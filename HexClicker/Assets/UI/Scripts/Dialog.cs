using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HexClicker.UI
{
    [RequireComponent(typeof(CanvasFader))]
    public class Dialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;

        [SerializeField] private Button cancelButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button acceptButton;

        [SerializeField] private TextMeshProUGUI cancelButtonText;
        [SerializeField] private TextMeshProUGUI declineButtonText;
        [SerializeField] private TextMeshProUGUI acceptButtonText;

        private CanvasFader canvasFader;

        private void Awake()
        {
            canvasFader = GetComponent<CanvasFader>();
        }

        public string Title => titleText.text;
        public string Body => bodyText.text;
        public void Open() => canvasFader.StartFadeIn();
        public void Close(bool destroy = false, bool deactivate = false) => canvasFader.StartFadeOut(destroy, deactivate);
        public void Display(string title, string body, bool destroyOnClose, string buttonText, UnityAction action)
        {
            gameObject.SetActive(true);
            canvasFader.Alpha = 0;

            acceptButton.gameObject.SetActive(false);
            declineButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);

            titleText.text = title;
            bodyText.text = body;

            cancelButtonText.tag = buttonText;

            cancelButton.onClick.RemoveAllListeners();

            cancelButton.onClick.AddListener(action);
            cancelButton.onClick.AddListener(() => Close(destroyOnClose, true));

            Open();
        }

        public void Display(string title, string body, bool destroyOnClose,
            string declineText, UnityAction declineAction,
            string acceptText, UnityAction acceptAction)
        {
            gameObject.SetActive(true);
            canvasFader.Alpha = 0;

            acceptButton.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(false);

            titleText.text = title;
            bodyText.text = body;

            declineButtonText.text = declineText;
            acceptButtonText.text = acceptText;

            declineButton.onClick.RemoveAllListeners();
            acceptButton.onClick.RemoveAllListeners();

            declineButton.onClick.AddListener(declineAction);
            acceptButton.onClick.AddListener(acceptAction);

            declineButton.onClick.AddListener(() => Close(destroyOnClose, true));
            acceptButton.onClick.AddListener(() => Close(destroyOnClose, true));

            Open();
        }

        public void Display(string title, string body, bool destroyOnClose,
            string cancelText, UnityAction cancelAction,
            string declineText, UnityAction declineAction,
            string acceptText, UnityAction acceptAction)
        {
            gameObject.SetActive(true);
            canvasFader.Alpha = 0;

            acceptButton.gameObject.SetActive(true);
            declineButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);

            titleText.text = title;
            bodyText.text = body;

            cancelButtonText.text = cancelText;
            declineButtonText.text = declineText;
            acceptButtonText.text = acceptText;

            cancelButton.onClick.RemoveAllListeners();
            declineButton.onClick.RemoveAllListeners();
            acceptButton.onClick.RemoveAllListeners();

            cancelButton.onClick.AddListener(cancelAction);
            declineButton.onClick.AddListener(declineAction);
            acceptButton.onClick.AddListener(acceptAction);

            cancelButton.onClick.AddListener(() => Close(destroyOnClose, true));
            declineButton.onClick.AddListener(() => Close(destroyOnClose, true));
            acceptButton.onClick.AddListener(() => Close(destroyOnClose, true));

            Open();
        }

    }
}
