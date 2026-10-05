namespace Base.Observer.EventBus
{
    /// <summary>Một listener trong snapshot debug — chỉ dùng để hiển thị trong Editor.</summary>
    public struct EventBusListenerInfo
    {
        public string EventName;
        public string TargetName;
        public string MethodName;
        public bool IsDestroyed;

        /// <summary>Reference tới UnityEngine.Object đã đăng ký (null nếu static hoặc đã bị destroy).</summary>
        public UnityEngine.Object RegisteredObject;

        public override string ToString() =>
            IsDestroyed
                ? $"[DESTROYED] {EventName} ← {TargetName}.{MethodName}"
                : $"{EventName} ← {TargetName}.{MethodName}";
    }
}