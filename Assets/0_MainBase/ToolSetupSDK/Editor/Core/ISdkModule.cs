using System;
using System.Collections.Generic;

namespace Base.ToolSDK.Editor
{
    [Flags]
    public enum SdkPlatform
    {
        None = 0,
        Android = 1,
        iOS = 2,
        All = Android | iOS
    }

    /// <summary>Trạng thái của một module trong project.</summary>
    public enum SdkInstallState
    {
        /// <summary>Không có package/define nào của module.</summary>
        NotInstalled,

        /// <summary>Có một phần (cài lỗi/bị hủy giữa chừng, hoặc package sai phiên bản). Cài tiếp hoặc gỡ đều được.</summary>
        Partial,

        /// <summary>Đủ mọi package và define.</summary>
        Installed
    }

    /// <summary>
    /// Hợp đồng của một SDK. Muốn thêm SDK mới chỉ cần tạo class kế thừa SdkModuleBase,
    /// SdkRegistry sẽ tự tìm thấy.
    /// </summary>
    public interface ISdkModule
    {
        string Id { get; }
        string DisplayName { get; }
        string Group { get; }
        string Description { get; }
        SdkPlatform Platforms { get; }

        /// <summary>Id các module bắt buộc phải có.</summary>
        IReadOnlyList<string> Requires { get; }

        /// <summary>Id các module không được cài cùng.</summary>
        IReadOnlyList<string> Conflicts { get; }

        /// <summary>Khả năng module cung cấp (ad-mediation, analytics...).</summary>
        IReadOnlyList<string> Provides { get; }

        SdkInstallState GetInstallState();
        IReadOnlyList<StepData> BuildInstallSteps();
        IReadOnlyList<StepData> BuildRemoveSteps();
    }

    public static class SdkModuleExtensions
    {
        /// <summary>Đã cài đủ (không tính cài dở).</summary>
        public static bool IsInstalled(this ISdkModule module)
        {
            return module.GetInstallState() == SdkInstallState.Installed;
        }
    }
}
