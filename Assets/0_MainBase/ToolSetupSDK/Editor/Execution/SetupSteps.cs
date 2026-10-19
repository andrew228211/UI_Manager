using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Base.ToolSDK.Editor
{
    public enum StepKind
    {
        AddPackage,
        RemovePackage,
        AddDefine,
        RemoveDefine
    }

    /// <summary>Dữ liệu thuần của một bước, serialize được để sống qua domain reload.</summary>
    [Serializable]
    public class StepData
    {
        public StepKind kind;
        public string arg;

        public StepData() { }

        public StepData(StepKind kind, string arg)
        {
            this.kind = kind;
            this.arg = arg;
        }
    }

    public enum StepPoll
    {
        Running,
        Done,
        Failed
    }

    public interface ISetupStep
    {
        string Label { get; }
        string Error { get; }

        /// <summary>Bước đã đạt kết quả mong muốn chưa (để chạy lại không lỗi).</summary>
        bool IsDone();

        void Begin();
        StepPoll Poll();
    }

    public static class SetupStepFactory
    {
        public static ISetupStep Create(StepData data)
        {
            switch (data.kind)
            {
                case StepKind.AddPackage: return new AddPackageStep(data.arg);
                case StepKind.RemovePackage: return new RemovePackageStep(data.arg);
                case StepKind.AddDefine: return new AddDefineStep(data.arg);
                case StepKind.RemoveDefine: return new RemoveDefineStep(data.arg);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    abstract class PackageStep : ISetupStep
    {
        // Sau domain reload đối tượng request bị mất. Đợi một lúc rồi gửi lại đúng 1 lần
        // thay vì chờ tới hết thời gian chờ của bước.
        static readonly TimeSpan OrphanGrace = TimeSpan.FromSeconds(15);

        // Request báo thành công nhưng project chưa phản ánh: đợi tối đa chừng này.
        static readonly TimeSpan SettleGrace = TimeSpan.FromSeconds(20);

        protected readonly string Id;
        Request _request;
        DateTime _orphanSince;
        DateTime _completedAt;
        bool _resent;

        protected PackageStep(string id) { Id = id; }

        public abstract string Label { get; }
        public string Error { get; private set; }
        public abstract bool IsDone();
        protected abstract Request Send();

        public void Begin()
        {
            _request = Send();
            _orphanSince = default;
            _completedAt = default;
        }

        public StepPoll Poll()
        {
            var now = DateTime.UtcNow;

            if (_request == null)
            {
                PackageUtil.Invalidate();
                if (IsDone()) return StepPoll.Done;

                if (_orphanSince == default)
                {
                    _orphanSince = now;
                    return StepPoll.Running;
                }

                if (now - _orphanSince > OrphanGrace)
                {
                    if (_resent)
                    {
                        Error = "Package Manager không phản hồi sau khi Unity tải lại script.";
                        return StepPoll.Failed;
                    }

                    _resent = true;
                    Begin();
                }

                return StepPoll.Running;
            }

            if (!_request.IsCompleted) return StepPoll.Running;

            PackageUtil.Invalidate();

            if (_request.Status == StatusCode.Failure)
            {
                Error = _request.Error != null ? _request.Error.message : "Package Manager báo lỗi";
                return StepPoll.Failed;
            }

            if (IsDone()) return StepPoll.Done;

            if (_completedAt == default)
            {
                _completedAt = now;
            }
            else if (now - _completedAt > SettleGrace)
            {
                Error = "Package Manager báo thành công nhưng project chưa có kết quả mong muốn.";
                return StepPoll.Failed;
            }

            return StepPoll.Running;
        }
    }

    class AddPackageStep : PackageStep
    {
        public AddPackageStep(string id) : base(id) { }
        public override string Label => "Thêm package " + Id;
        public override bool IsDone() => PackageUtil.IsInstalled(Id);
        protected override Request Send() => Client.Add(Id);
    }

    class RemovePackageStep : PackageStep
    {
        public RemovePackageStep(string id) : base(id) { }
        public override string Label => "Gỡ package " + PackageUtil.NameOf(Id);

        // Chỉ cần không còn là dependency trực tiếp. Package còn lại do package khác kéo theo là bình thường.
        public override bool IsDone() => !PackageUtil.IsDirect(PackageUtil.NameOf(Id));

        protected override Request Send() => Client.Remove(PackageUtil.NameOf(Id));
    }

    abstract class DefineStep : ISetupStep
    {
        protected readonly string Symbol;
        protected DefineStep(string symbol) { Symbol = symbol; }

        public abstract string Label { get; }
        public string Error => null;
        public abstract bool IsDone();
        public abstract void Begin();

        // Áp lại nếu chưa đạt: an toàn vì thao tác define là idempotent, và cứu trường hợp
        // domain reload xảy ra trước khi Begin kịp làm gì.
        public StepPoll Poll()
        {
            if (IsDone()) return StepPoll.Done;
            Begin();
            return StepPoll.Running;
        }
    }

    class AddDefineStep : DefineStep
    {
        public AddDefineStep(string symbol) : base(symbol) { }
        public override string Label => "Thêm define " + Symbol;
        public override bool IsDone() => DefineUtil.IsDefined(Symbol);
        public override void Begin() => DefineUtil.Add(Symbol);
    }

    class RemoveDefineStep : DefineStep
    {
        public RemoveDefineStep(string symbol) : base(symbol) { }
        public override string Label => "Gỡ define " + Symbol;
        public override bool IsDone() => !DefineUtil.IsDefinedAnywhere(Symbol);
        public override void Begin() => DefineUtil.Remove(Symbol);
    }
}
