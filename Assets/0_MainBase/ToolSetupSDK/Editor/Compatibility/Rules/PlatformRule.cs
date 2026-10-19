using UnityEditor;

namespace Base.ToolSDK.Editor
{
    /// <summary>So nền tảng module hỗ trợ với build target đang bật.</summary>
    public class PlatformRule : ICompatibilityRule
    {
        public string Id => "3-platform";

        public void Evaluate(CompatibilityContext ctx, CompatibilityReport report)
        {
            SdkPlatform current;
            switch (ctx.Target)
            {
                case BuildTarget.Android: current = SdkPlatform.Android; break;
                case BuildTarget.iOS: current = SdkPlatform.iOS; break;
                default: current = SdkPlatform.None; break;
            }

            foreach (var m in ctx.ToInstall)
            {
                if (current == SdkPlatform.None)
                {
                    // Đang ở Windows/Mac...: chưa kiểm tra được, chỉ nhắc.
                    if (m.Platforms != SdkPlatform.All)
                    {
                        report.Add(IssueLevel.Warning, IssueScope.Install, m.Id,
                            $"'{m.DisplayName}' chỉ hỗ trợ {m.Platforms}. Build target hiện tại là {ctx.Target}, nên chưa kiểm tra được; hãy chuyển sang đúng nền tảng khi build.");
                    }
                    continue;
                }

                if ((m.Platforms & current) == 0)
                {
                    report.Add(IssueLevel.Error, IssueScope.Install, m.Id,
                        $"'{m.DisplayName}' chỉ hỗ trợ {m.Platforms}, nhưng build target hiện tại là {ctx.Target}.");
                }
            }
        }
    }
}
