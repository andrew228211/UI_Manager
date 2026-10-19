using System.Collections.Generic;

namespace Base.ToolSDK.Editor
{
    /// <summary>
    /// Lớp cơ sở: chỉ cần khai báo thông tin + PackageIds + Defines là có đủ
    /// các bước cài và gỡ mặc định. Module phức tạp hơn có thể override
    /// GetInstallState/BuildInstallSteps/BuildRemoveSteps.
    /// Module không có PackageIds lẫn Defines PHẢI override GetInstallState, nếu không luôn là "chưa cài".
    /// </summary>
    public abstract class SdkModuleBase : ISdkModule
    {
        static readonly string[] Empty = new string[0];

        public abstract string Id { get; }
        public abstract string DisplayName { get; }

        public virtual string Group => "Other";
        public virtual string Description => string.Empty;
        public virtual SdkPlatform Platforms => SdkPlatform.All;

        public virtual IReadOnlyList<string> Requires => Empty;
        public virtual IReadOnlyList<string> Conflicts => Empty;
        public virtual IReadOnlyList<string> Provides => Empty;

        /// <summary>Package UPM cần cài (id, có thể kèm @version).</summary>
        protected virtual IReadOnlyList<string> PackageIds => Empty;

        /// <summary>Scripting Define Symbols cần thêm.</summary>
        protected virtual IReadOnlyList<string> Defines => Empty;

        public virtual SdkInstallState GetInstallState()
        {
            if (PackageIds.Count == 0 && Defines.Count == 0) return SdkInstallState.NotInstalled;

            var complete = true; // mọi thứ đã đạt
            var any = false;     // có ít nhất một thứ

            foreach (var id in PackageIds)
            {
                if (PackageUtil.IsInstalled(id)) any = true;
                else
                {
                    complete = false;
                    // Có package nhưng sai phiên bản vẫn tính là có một phần.
                    if (PackageUtil.IsDirect(PackageUtil.NameOf(id))) any = true;
                }
            }

            foreach (var d in Defines)
            {
                if (DefineUtil.IsDefined(d)) any = true;
                else
                {
                    complete = false;
                    if (DefineUtil.IsDefinedAnywhere(d)) any = true;
                }
            }

            if (complete) return SdkInstallState.Installed;
            return any ? SdkInstallState.Partial : SdkInstallState.NotInstalled;
        }

        public virtual IReadOnlyList<StepData> BuildInstallSteps()
        {
            var steps = new List<StepData>();
            foreach (var id in PackageIds) steps.Add(new StepData(StepKind.AddPackage, id));
            foreach (var d in Defines) steps.Add(new StepData(StepKind.AddDefine, d));
            return steps;
        }

        public virtual IReadOnlyList<StepData> BuildRemoveSteps()
        {
            var steps = new List<StepData>();
            foreach (var d in Defines) steps.Add(new StepData(StepKind.RemoveDefine, d));
            foreach (var id in PackageIds) steps.Add(new StepData(StepKind.RemovePackage, id));
            return steps;
        }
    }
}
