using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Base.Observer.EventBus.Demo
{
    /// <summary>
    /// Demo: Listern nhận <see cref="IEventBusService"/> qua injection
    /// Gán vào GameObject khác với <see cref="EventBusDemoDispatcher"/>, đăng ký ở LifetimeScope
    /// Mở Window -> Base -> Event Bus window để xem các listener đã đăng ký, và các event đã được phát ra
    /// </summary>
    public class EventBusDemoListener : MonoBehaviour, IInitializable
    {
        [Inject] private IEventBusService _eventBus;
        public void Initialize()
        {
            _eventBus.Register<OnGameStartEvent>(OnGameStart);
            _eventBus.Register<OnGameWin>(OnGameWin);
            _eventBus.Register<OnCoinChanged>(OnCoinChanged);
            _eventBus.Register<OnPlayerJump>(OnPlayerJump);
        }
        //[Inject] chạy trước Start lần đầu, nhưng OnEnable còn chạy sau mỗi lần bật tắt Object
        //cần Register/Unregister 
        private void OnEnable()
        {
            if(_eventBus==null) return;
            _eventBus.Register<OnGameStartEvent>(OnGameStart);
            _eventBus.Register<OnGameWin>(OnGameWin);
            _eventBus.Register<OnCoinChanged>(OnCoinChanged);
            _eventBus.Register<OnPlayerJump>(OnPlayerJump);
        }
        private void OnDisable()
        {
            if(_eventBus==null) return;
            _eventBus.Unregister<OnGameStartEvent>(OnGameStart);
            _eventBus.Unregister<OnGameWin>(OnGameWin);
            _eventBus.Unregister<OnCoinChanged>(OnCoinChanged);
            _eventBus.Unregister<OnPlayerJump>(OnPlayerJump);
        }

        //Handles 
        private void OnGameStart(OnGameStartEvent _)
           => Debug.Log($"[{name}] Game Started!");

        private void OnGameWin(OnGameWin _)
            => Debug.Log($"[{name}] Game Won!");

        private void OnCoinChanged(OnCoinChanged e)
            => Debug.Log($"[{name}] Coin: {e.OldValue} → {e.NewValue} (Δ{e.Delta})");

        private void OnPlayerJump(OnPlayerJump e)
            => Debug.Log($"[{name}] Player jumped {e.JumpForce}m");
    }


}