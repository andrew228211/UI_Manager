using System.Linq;

namespace Base.ToolSDK.Editor
{
    /// <summary>Hai module khai báo Conflicts với nhau thì không được cùng tồn tại.</summary>
    public class ConflictRule : ICompatibilityRule
    {
        public string Id => "2-conflict";

        public void Evaluate(CompatibilityContext ctx, CompatibilityReport report)
        {
            // Kiểm tra trên tập SAU KHI CÀI (gồm cả module đang có dù đang được chọn để gỡ,
            // vì gỡ là lần chạy riêng và chưa xảy ra lúc cài).
            var all = ctx.AfterInstall;
            for (var i = 0; i < all.Count; i++)
            {
                for (var j = i + 1; j < all.Count; j++)
                {
                    var a = all[i];
                    var b = all[j];

                    var conflict = Contains(a, b.Id) || Contains(b, a.Id);
                    if (!conflict) continue;

                    // Chỉ báo khi liên quan tới thứ sắp cài, tránh báo lại thứ đã tồn tại sẵn.
                    var aNew = ctx.ToInstall.Contains(a);
                    var bNew = ctx.ToInstall.Contains(b);
                    if (!aNew && !bNew) continue;

                    var message = $"'{a.DisplayName}' xung đột với '{b.DisplayName}'. Không thể cài cùng nhau.";
                    if (aNew != bNew)
                    {
                        var existing = aNew ? b : a;
                        message += $" '{existing.DisplayName}' đang có trong project, hãy gỡ nó trước.";
                    }

                    report.Add(IssueLevel.Error, IssueScope.Install, a.Id, message);
                }
            }
        }

        static bool Contains(ISdkModule module, string id)
        {
            foreach (var c in module.Conflicts)
                if (c == id) return true;
            return false;
        }
    }
}
