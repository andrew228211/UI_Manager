namespace Base.Observer.EventBus.Demo
{
    // --Custom event chỉ dùng trong demo
    public struct OnGameStartEvent : IEvent
    {
      
    }
    public struct OnGameWin : IEvent
    {
        
    }
    public struct OnCoinChanged : IEvent
    {
        public int OldValue;
        public int NewValue;
        public int Delta => NewValue - OldValue;
    }
    public struct OnPlayerJump : IEvent
    {
        public float JumpForce;
    }
}