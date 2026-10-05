using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Base.Observer.EventBus.Demo
{
    /// <summary>
    /// Demo: dispatch event qua <see cref="IEventBusService"/> được inject.
    /// Gán vào một GameObject, đăng ký bằng <c>RegisterComponentInHierarchy</c>,
    /// rồi bấm các mục trong context menu của component (Play Mode).
    /// </summary>
    public class EventBusDemoDispatcher : MonoBehaviour
    {
        [Inject] private IEventBusService _eventBus;

        private bool HasEventBus()
        {
            if (_eventBus != null)
            {
                return true;
            }

            Debug.LogError(
                $"[{name}] IEventBusService is null. Make sure this object is under an active EventBusLifetimeScope and dispatch in Play Mode.",
                this);
            return false;
        }

        [Button("Dispatch OnGameStart")]
        public void DispatchGameStart()
        {
            if (!HasEventBus()) return;
            _eventBus.Dispatch<OnGameStartEvent>();
        }

        [Button("Dispatch OnGameWin")]
        public void DispatchGameWin()
        {
            if (!HasEventBus()) return;
            _eventBus.Dispatch<OnGameWin>();
        }

        [Button("Dispatch OnCoinChanged")]
        public void DispatchCoinChanged()
        {
            if (!HasEventBus()) return;
            _eventBus.Dispatch(new OnCoinChanged { OldValue = 50, NewValue = 150 });
        }

        [Button("Dispatch OnPlayerJump")]
        public void DispatchPlayerJump()
        {
            if (!HasEventBus()) return;
            _eventBus.Dispatch(new OnPlayerJump { JumpForce = 3.5f });
        }
    }

}
