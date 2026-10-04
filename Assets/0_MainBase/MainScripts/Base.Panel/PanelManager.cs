using System;
using System.Collections.Generic;
using Base.Singleton;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
namespace Base.UI.Panel
{
    public class PanelManager : Singleton<PanelManager>
    {
        [ShowInInspector]
        private readonly List<Panel> _stackPanels = new List<Panel>();
        public T GetPanel<T>(string panelName) where T : Panel
        {
            // Logic to retrieve a panel of type T by its name
            return (T)_stackPanels.Find(p => p.PanelName == panelName);
        }

        protected override void OnAwake()
        {

        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TryCloseCurrentPanel();
        }
        public Type CurrentPanelType => _stackPanels.Count > 0 ? _stackPanels[_stackPanels.Count - 1].GetType() : null;
        public async UniTask<Panel> CreatePanel<T>(string panelName, bool canBack, Action<T> onSetup = null, bool autoOpen = true) where T : Panel
        {
            var startFrame = Time.frameCount;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var panel = (await Addressables.InstantiateAsync(panelName, transform)).GetComponent<T>();
            stopwatch.Stop();
            Debug.Log($"[PanelManager] CreatePanel<{typeof(T).Name}>({panelName}) took {stopwatch.ElapsedMilliseconds} ms, frames: {Time.frameCount - startFrame}");
            panel.Init(panelName, canBack);
            _stackPanels.Add(panel);
            onSetup?.Invoke(panel);
            if (autoOpen)
            {
                panel.Open();
            }
            return panel;
        }
        public async UniTask OpenPanel(string panelName, bool waitOpenCompleted = false)
        {
            // find panel of type by name
            var panel = _stackPanels.Find(panel => panel.PanelName == panelName);
            if (panel == null)
            {
                Debug.LogWarning("[PanelManager] Cannot find panel " + panelName.Color("red"));
                return;
            }

            // play open animation
            panel.Open();

            // wait until close completed
            if (waitOpenCompleted)
                await UniTask.Delay(TimeSpan.FromSeconds(panel.openAnimationDuration));
        }
        public async UniTask ClosePanel(string panelName, bool immediately = false, bool waitCloseCompleted = false)
        {
            // find panel of type by name
            var panel = _stackPanels.Find(panel => panel.PanelName == panelName);
            if (panel == null)
            {
                Debug.LogWarning("[PanelManager] Cannot find panel " + panelName.Color("red"));
                return;
            }

            // play close animation (if not immediately)
            if (immediately)
                panel.CloseImmediately();
            else
                panel.Close();

            // wait until close completed
            if (waitCloseCompleted)
                await UniTask.WaitUntil(() => panel == null);
        }
        public void ReleasePanel(Panel panelClosed)
        {
            Debug.Log("[PanelManager] Released " + panelClosed.PanelName.Color("green"));
            _stackPanels.Remove(panelClosed);
        }

        //Test
        private void TryCloseCurrentPanel()
        {
            if (_stackPanels.Count == 0)
            {
                Debug.LogWarning("[PanelManager] Stack is empty");
                return;
            }

            if (!_stackPanels.Last().CanBack)
            {
                Debug.LogWarning("[PanelManager] Cannot back");
                return;
            }

            Debug.Log("[PanelManager] Close " + CurrentPanelType.Name.Color("cyan"));
            var panelInTop = _stackPanels.Last();
            _stackPanels.Remove(panelInTop);
            panelInTop.OnCloseButton();
        }
    }
}