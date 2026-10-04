using DG.Tweening;
using UnityEngine;

namespace Base.UI.Panel
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Panel : MonoBehaviour
    {
        public string PanelName { get; private set; }
        public bool CanBack { get; private set; }
        [Header("Panel Settings")]
        public CanvasGroup panelCanvasGroup;
        public float openAnimationDuration = 0.4f;
        public float closeAnimationDuration = 0.4f;

        protected virtual void Reset()
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
        }
        public void Init(string panelName, bool canBack)
        {
            PanelName = panelName;
            CanBack = canBack;
        }
        public virtual void Open()
        {
            gameObject.SetActive(true);

            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.DOFade(1f, openAnimationDuration)
                .OnComplete(() => panelCanvasGroup.interactable = true);
        }
        public virtual void Close()
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.DOFade(0f, closeAnimationDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }
        protected virtual void OnCloseCompleted()
        {
            PanelManager.Instance.ReleasePanel(this);
            Destroy(gameObject);
        }
        public virtual void CloseImmediately() => OnCloseCompleted();

        public virtual void OnCloseButton() => Close();
    }
}