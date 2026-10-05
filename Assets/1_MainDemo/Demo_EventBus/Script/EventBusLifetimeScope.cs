using VContainer;
using VContainer.Unity;

namespace Base.Observer.EventBus.Demo
{
    public class EventBusLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            EventBusInstaller.InstallEventBus(builder);
            builder.RegisterComponentInHierarchy<EventBusDemoDispatcher>();
            builder.RegisterComponentInHierarchy<EventBusDemoListener>();
        }
    }
}

