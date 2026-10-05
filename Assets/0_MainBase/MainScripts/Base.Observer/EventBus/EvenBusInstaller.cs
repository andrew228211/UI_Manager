using VContainer;
namespace Base.Observer.EventBus
{
    public static class EventBusInstaller
    {
        public static void InstallEventBus(this IContainerBuilder builder)
        {
            builder.Register<EventBusService>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}