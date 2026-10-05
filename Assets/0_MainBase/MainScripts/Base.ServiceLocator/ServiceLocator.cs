using System;
using System.Collections.Generic;
using Base.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Base.ServiceLocator
{
    /// <summary>
    /// Service Locator (singleton). Dùng để register và resolve các game service theo interface.
    /// </summary>
    public class ServiceLocator : Singleton<ServiceLocator>
    {
        [ShowInInspector, TableList]
        private readonly Dictionary<Type, IService> _services = new();

        private readonly Dictionary<Type, List<Action<IService>>> _pendingCallbacks = new();

        protected override void OnAwake()
        {
            
        }

        #region === REGISTER / Unregister  ===
        /// <summary>
        /// Đăng ký implementation cho <typeparamref name="TService"/>.
        /// Nếu đã tồn tại sẽ ghi đè và log warning.
        /// </summary>
        public void Register<TService>(TService impl) where TService : IService
        {
            if (impl == null)
            {
                Debug.LogWarning($"Register<{typeof(TService).Name}> — impl null, skipped.");
                return;
            }

            var type = typeof(TService);

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Register<{type.Name}> — đã có implementation, ghi đè bằng {impl.GetType().Name}.");
            }

            _services[type] = impl;
            Debug.Log($"Register<{type.Name}> — {impl.GetType().Name.Color("cyan")}.");
            FlushPendingCallbacks(type, impl);
        }
        /// <summary>
        /// Đăng ký implementation theo runtime Type. Dùng nội bộ bởi ServiceRegister.
        /// Throws <see cref="ArgumentException"/> nếu <paramref name="serviceType"/> không phải interface của <paramref name="impl"/>.
        /// </summary>
        public void Register(Type serviceType, IService impl)
        {
            if (impl == null)
            {
                Debug.LogWarning($"Register({serviceType.Name}) — impl null, skipped.");
                return;
            }

            if (!serviceType.IsAssignableFrom(impl.GetType()))
            {
                Debug.LogWarning($"Register({serviceType.Name}) — {impl.GetType().Name} không implement {serviceType.Name}, skipped.");
                return;
            }

            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Register({serviceType.Name}) — đã có implementation, ghi đè bằng {impl.GetType().Name}.");
            }

            _services[serviceType] = impl;
            Debug.Log($"Register: {serviceType.Name.Color("cyan")} — {impl.GetType().Name.Color("yellow")}.");
            FlushPendingCallbacks(serviceType, impl);
        }
        private void FlushPendingCallbacks(Type type, IService impl)
        {
            if (!_pendingCallbacks.TryGetValue(type, out var list)) return;

            _pendingCallbacks.Remove(type);

            foreach (var callback in list)
            {
                callback(impl);
            }
        }
        /// <summary>Hủy đăng ký <typeparamref name="TService"/> khỏi locator.</summary>
        public void Unregister<TService>() where TService : IService
        {
            var type = typeof(TService);

            if (!_services.Remove(type))
            {
                Debug.LogWarning($"Unregister<{type.Name}> — không tìm thấy service để xóa.");
            }
        }
        
        #endregion

        #region === GET / TRYGET ===
        // ── Get ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy implementation của <typeparamref name="TService"/>.
        /// Log warning và trả về default nếu chưa register.
        /// </summary>
        public TService Get<TService>() where TService : IService
        {
            var type = typeof(TService);

            if (_services.TryGetValue(type, out var service))
            {
                return (TService)service;
            }

            Debug.LogWarning($"Get<{type.Name}> — chưa register service này. Trả về default.");
            return default;
        }
        /// <summary>
        /// Lấy implementation qua callback. Nếu đã register → gọi ngay.
        /// Nếu chưa → queue lại, tự gọi khi service được register.
        /// </summary>
        public void Get<TService>(Action<TService> callback) where TService : IService
        {
            if (callback == null) return;

            var type = typeof(TService);

            if (_services.TryGetValue(type, out var service))
            {
                callback((TService)service);
                return;
            }

            if (!_pendingCallbacks.TryGetValue(type, out var list))
            {
                list = new List<Action<IService>>();
                _pendingCallbacks[type] = list;
            }

            list.Add(s => callback((TService)s));
        }
        /// <summary>
        /// Thử lấy implementation của <typeparamref name="TService"/>.
        /// Trả về true nếu tìm thấy.
        /// </summary>
        public bool TryGet<TService>(out TService service) where TService : IService
        {
            var type = typeof(TService);

            if (_services.TryGetValue(type, out var found))
            {
                service = (TService)found;
                return true;
            }

            service = default;
            return false;
        }
        // ── Utilities ────────────────────────────────────────────────────────

        /// <summary>Kiểm tra xem <typeparamref name="TService"/> đã được register chưa.</summary>
        public bool IsRegistered<TService>() where TService : IService
        {
            return _services.ContainsKey(typeof(TService));
        }

        /// <summary>Xóa toàn bộ service đã register.</summary>
        public void ClearAll()
        {
            _services.Clear();
            Debug.Log("All services cleared.".Color("orange"));
        }

        /// <summary>Trả về snapshot danh sách service hiện tại để debug/Editor hiển thị.</summary>
        public IReadOnlyDictionary<Type, IService> GetDebugSnapshot()
        {
            return _services;
        }
        #endregion
    }
}