using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
namespace Base.UI.Panel
{
    public class PopupPanel : Panel
    {
        [Header("Popup Settings")]
        public bool hasBackground = true;
        [ShowIf("@hasBackground")] Image backgroundImage;
        [ShowIf("@hasBackground")] float targetAlpha = 0.5f;

        public CanvasGroup popupCanvas;
        public Transform popupTransform;
        protected virtual void OnValidate()
        {
            if (hasBackground && backgroundImage != null)
                backgroundImage.Fade(targetAlpha);
        }

        public override void Open()
        {
            gameObject.SetActive(true);

            if (hasBackground)
            {
                backgroundImage.Fade(0f);
                backgroundImage.DOFade(targetAlpha, openAnimationDuration);
            }

            popupCanvas.alpha = 0;
            popupCanvas.interactable = false;
            popupCanvas.DOFade(1f, openAnimationDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => popupCanvas.interactable = true);

            popupTransform.localScale = Vector3.one * 0.5f;
            popupTransform.DOScale(1f, openAnimationDuration)
                .SetEase(Ease.OutBack);
        }
        protected override void Reset()
        {
            base.Reset();
            backgroundImage = transform.Find("Background").GetComponent<Image>();
            popupCanvas = transform.Find("Popup").GetComponent<CanvasGroup>();
            popupTransform = popupCanvas.transform;
        }
        public override void Close()
        {
            if (hasBackground)
            {
                backgroundImage.DOKill();
                backgroundImage.DOFade(0f, closeAnimationDuration)
                    .SetEase(Ease.OutCubic);
            }

            popupCanvas.interactable = false;
            popupCanvas.DOKill();
            popupCanvas.DOFade(0f, closeAnimationDuration)
                .SetEase(Ease.OutCubic);

            popupTransform.DOKill();
            popupTransform.DOScale(0.5f, closeAnimationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(OnCloseCompleted);
        }
    }
}