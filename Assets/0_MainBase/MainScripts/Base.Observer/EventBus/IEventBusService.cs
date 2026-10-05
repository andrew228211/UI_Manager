using System;
using System.Collections.Generic;

namespace Base.Observer.EventBus
{
    /// <summary>
    /// Pub/sub type-safe dùng struct event.
    /// <para>Bus sống theo scope đăng ký nó: ở scope gốc là bus toàn ứng dụng; ở scope scene
    /// (<c>Lifetime.Scoped</c>) thì mọi listener tự biến mất khi scene unload, không cần Unregister tay.</para>
    /// </summary>
    public interface IEventBusService
    {
        /// <summary>Đăng ký lắng nghe. Listener thêm vào giữa lúc dispatch sẽ nhận event từ lần dispatch sau.</summary>
        void Register<TEvent>(Action<TEvent> listener) where TEvent : IEvent;

        /// <summary>Hủy đăng ký. An toàn khi gọi ngay trong listener đang được dispatch.</summary>
        void Unregister<TEvent>(Action<TEvent> listener) where TEvent : IEvent;

        /// <summary>Gửi event có data tới mọi listener đã đăng ký.</summary>
        void Dispatch<TEvent>(TEvent evt) where TEvent : IEvent;

        /// <summary>Gửi signal event không có data.</summary>
        void Dispatch<TEvent>() where TEvent : struct, IEvent;

        /// <summary>Xóa mọi listener của một event type.</summary>
        void ClearEvent<TEvent>() where TEvent : IEvent;

        /// <summary>Xóa toàn bộ listener.</summary>
        void ClearAll();

        /// <summary>Snapshot listener đang đăng ký — phục vụ cửa sổ debug trong Editor.</summary>
        List<EventBusListenerInfo> GetDebugSnapshot();
    }
}