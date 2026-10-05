namespace Base.Observer.EventBus
{
    /// <summary>
    /// Marker cho mọi event của game. Nên khai báo bằng <c>struct</c> để dispatch không sinh rác.
    /// </summary>
    public interface IEvent
    {
    }
}