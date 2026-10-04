using Base.UI.Panel;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Demo
{
    public class HomePanel : Panel
    {
        [Title("Home Panel")]
        [SerializeField] private Button btnlay;
        [SerializeField] private Button btnSetting;
        private void Awake()
        {
            btnlay.onClick.AddListener(OnClickPlayButton);
            btnSetting.onClick.AddListener(OnClickSettingButton);
        }
        private void OnDestroy()
        {
            btnlay.onClick.RemoveListener(OnClickPlayButton);
            btnSetting.onClick.RemoveListener(OnClickSettingButton);
        }
        public void OnClickPlayButton()
        {
            DarkTransition.Instance.TransitionAsync(async () =>
        {
            // close immediately home panel
            await PanelManager.Instance.ClosePanel(
                panelName: "HomePanel",
                immediately: true
            );

            // create play panel when screen is full dark
            await PanelManager.Instance.CreatePanel<PlayPanel>(
                panelName: "PlayPanel",
                canBack: false,
                onSetup: panel => panel.Setup()
            );
        });
        }
        public void OnClickSettingButton()
        {
            PanelManager.Instance.CreatePanel<SettingPanel>(
           panelName: "SettingPanel", canBack: true 
       ).Forget();
        }
    }
}
