using UnityEngine;
namespace Base.ServiceLocator
{
    /// <summary>
    /// Base contract for services registered in <see cref="ServiceLocator"/>. All services should implement this interface.
    /// <para>Services are initialized in two phases:</para>
    /// <para><see cref="Initialize"/>: one-time setup for this service only.</para>
    /// <para><see cref="LateInitialize"/>: after all services have initialized — safe to resolve others.</para>
    /// </summary>
    public interface IService
    {
        void Initialize();
        void LateInitialize();
    }
}
