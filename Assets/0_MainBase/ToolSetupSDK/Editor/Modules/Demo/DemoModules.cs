using System.Collections.Generic;

// ============================================================================
// MODULE DEMO — chỉ để thử tool. Xóa cả thư mục Modules/Demo khi không cần.
//
// Thêm một SDK mới chỉ cần tạo một class như bên dưới: khai báo thông tin,
// PackageIds và Defines. Không cần sửa bất kỳ file nào khác.
// ============================================================================
namespace Base.ToolSDK.Editor.Demo
{
    /// <summary>SDK THẬT: cài package Unity Mobile Notifications (Android + iOS).</summary>
    public class MobileNotificationsModule : SdkModuleBase
    {
        public override string Id => "unity-mobile-notifications";
        public override string DisplayName => "Mobile Notifications (Unity)";
        public override string Group => "Demo - Package thật";
        public override string Description => "Cài package com.unity.mobile.notifications (phiên bản Unity khuyến nghị) và thêm define TOOLSDK_MOBILE_NOTIFICATIONS.";

        protected override IReadOnlyList<string> PackageIds => new[] { "com.unity.mobile.notifications" };
        protected override IReadOnlyList<string> Defines => new[] { "TOOLSDK_MOBILE_NOTIFICATIONS" };
    }

    /// <summary>Giả lập Firebase Core: chỉ thêm define, để thử quan hệ cha-con.</summary>
    public class DemoFirebaseCoreModule : SdkModuleBase
    {
        public override string Id => "demo-firebase-core";
        public override string DisplayName => "Demo Firebase Core";
        public override string Group => "Demo - Giả lập (chỉ thêm define)";
        public override string Description => "Module cha. Chỉ thêm define DEMO_FIREBASE_CORE.";

        protected override IReadOnlyList<string> Defines => new[] { "DEMO_FIREBASE_CORE" };
    }

    /// <summary>Giả lập Firebase Analytics: cần Core → thử tự thêm phụ thuộc.</summary>
    public class DemoFirebaseAnalyticsModule : SdkModuleBase
    {
        public override string Id => "demo-firebase-analytics";
        public override string DisplayName => "Demo Firebase Analytics";
        public override string Group => "Demo - Giả lập (chỉ thêm define)";
        public override string Description => "Cần Demo Firebase Core. Chọn riêng module này sẽ thấy Core được tự thêm vào kế hoạch.";

        public override IReadOnlyList<string> Requires => new[] { "demo-firebase-core" };
        protected override IReadOnlyList<string> Defines => new[] { "DEMO_FIREBASE_ANALYTICS" };
    }

    /// <summary>Giả lập SDK Ads A, xung đột với B → thử luật xung đột.</summary>
    public class DemoAdsAlphaModule : SdkModuleBase
    {
        public override string Id => "demo-ads-alpha";
        public override string DisplayName => "Demo Ads Alpha";
        public override string Group => "Demo - Giả lập (chỉ thêm define)";
        public override string Description => "Xung đột với Demo Ads Beta. Chọn cả hai sẽ thấy lỗi và nút Cài bị khóa.";

        public override IReadOnlyList<string> Conflicts => new[] { "demo-ads-beta" };
        public override IReadOnlyList<string> Provides => new[] { "ad-mediation" };
        protected override IReadOnlyList<string> Defines => new[] { "DEMO_ADS_ALPHA" };
    }

    public class DemoAdsBetaModule : SdkModuleBase
    {
        public override string Id => "demo-ads-beta";
        public override string DisplayName => "Demo Ads Beta";
        public override string Group => "Demo - Giả lập (chỉ thêm define)";
        public override string Description => "Xung đột với Demo Ads Alpha.";

        public override IReadOnlyList<string> Conflicts => new[] { "demo-ads-alpha" };
        public override IReadOnlyList<string> Provides => new[] { "ad-mediation" };
        protected override IReadOnlyList<string> Defines => new[] { "DEMO_ADS_BETA" };
    }

    /// <summary>Chỉ hỗ trợ Android → thử luật nền tảng (chuyển build target sang iOS để thấy lỗi).</summary>
    public class DemoAndroidOnlyModule : SdkModuleBase
    {
        public override string Id => "demo-android-only";
        public override string DisplayName => "Demo Android Only";
        public override string Group => "Demo - Giả lập (chỉ thêm define)";
        public override string Description => "Chỉ hỗ trợ Android. Đổi Build Target sang iOS sẽ báo lỗi; ở Windows/Mac chỉ báo cảnh báo.";
        public override SdkPlatform Platforms => SdkPlatform.Android;

        protected override IReadOnlyList<string> Defines => new[] { "DEMO_ANDROID_ONLY" };
    }
}
