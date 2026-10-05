using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Base.ServiceLocator
{
    /// <summary>
    /// Service register — nơi đăng ký các service của game.
    /// — Mono services: tự động tìm qua <c>GetComponentsInChildren</c> trên cùng GameObject.
    /// — Non-Mono services: thêm trực tiếp từ Inspector qua <c>[SerializeReference]</c>.
    /// </summary>
    [DefaultExecutionOrder(-999)]
    public class ServiceRegister : MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        [Tooltip("Danh sách MonoBehaviour services đã được register (chỉ đọc, cập nhật lúc Play Mode).")]
        private List<IService> _monoServices = new();
        [SerializeReference]
        [Tooltip("Plain C# services (non-MonoBehaviour). Thêm từ Inspector bằng cách right-click → 'Add Reference'.")]
        private List<IService> _nonMonoServices = new();
        private void Awake()
        {
            Register();
        }
        protected virtual void Register()
        {
            RegisterMonoServices();
            RegisterNonMonoServices();
            InitializeAllServices();
            LateInitializeAllServices();
        }

        private void RegisterMonoServices()
        {
            // Tìm tất cả MonoBehaviour implement IService trên GameObject và children
            var found = GetComponentsInChildren<IService>(includeInactive: true);

            _monoServices.Clear();

            foreach (var service in found)
            {
                RegisterByFirstServiceInterface(service);
                _monoServices.Add(service);
            }
        }
        private void RegisterNonMonoServices()
        {
            foreach (var service in _nonMonoServices)
            {
                if (service == null) continue;

                RegisterByFirstServiceInterface(service);
            }
        }
        private static void RegisterByFirstServiceInterface(IService impl)
        {
            var concreteType = impl.GetType();
            var keyType = FindFirstServiceInterface(concreteType) ?? concreteType;

            ServiceLocator.Instance.Register(keyType, impl);
        }
        private void InitializeAllServices()
        {
            foreach (var service in _monoServices)
                service.Initialize();

            foreach (var service in _nonMonoServices)
                service?.Initialize();
        }
        private void LateInitializeAllServices()
        {
            foreach (var service in _monoServices)
                service.LateInitialize();

            foreach (var service in _nonMonoServices)
                service?.LateInitialize();
        }

        private static Type FindFirstServiceInterface(Type type)
        {
            var iServiceType = typeof(IService);

            foreach (var iface in type.GetInterfaces())
            {
                if (iface != iServiceType && iServiceType.IsAssignableFrom(iface))
                    return iface;
            }

            return null;
        }
    }

}
