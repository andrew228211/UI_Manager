using System.Collections.Generic;
using System.Linq;

namespace Base.ToolSDK.Editor
{
    /// <summary>Kiểm tra phụ thuộc khi cài và khi gỡ, và phát hiện vòng phụ thuộc.</summary>
    public class RequiresRule : ICompatibilityRule
    {
        public string Id => "1-requires";

        public void Evaluate(CompatibilityContext ctx, CompatibilityReport report)
        {
            // Khi cài: module cần module khác.
            foreach (var m in ctx.ToInstall)
            {
                foreach (var reqId in m.Requires)
                {
                    if (!SdkRegistry.TryGet(reqId, out var req))
                    {
                        report.Add(IssueLevel.Error, IssueScope.Install, m.Id,
                            $"'{m.DisplayName}' cần '{reqId}' nhưng không có module nào như vậy.");
                    }
                    else if (ctx.AutoAdded.Contains(req))
                    {
                        report.Add(IssueLevel.Info, IssueScope.Install, m.Id,
                            $"'{m.DisplayName}' cần '{req.DisplayName}' → sẽ tự thêm vào kế hoạch.");
                    }
                }

                if (IsInCycle(m))
                {
                    report.Add(IssueLevel.Error, IssueScope.Install, m.Id,
                        $"'{m.DisplayName}' nằm trong vòng phụ thuộc (Requires lẫn nhau). Hãy sửa khai báo Requires.");
                }
            }

            // Khi gỡ: còn module khác (sau khi gỡ) đang cần module sắp gỡ.
            foreach (var removed in ctx.ToRemove)
            {
                foreach (var other in ctx.AfterRemove.Where(f => f.Requires.Contains(removed.Id)))
                {
                    report.Add(IssueLevel.Error, IssueScope.Remove, removed.Id,
                        $"Không thể gỡ '{removed.DisplayName}' vì '{other.DisplayName}' đang cần nó. Hãy gỡ '{other.DisplayName}' trước hoặc gỡ cùng lúc.");
                }
            }

            // Gợi ý: module cha từng được tự thêm có thể không còn ai cần sau khi gỡ.
            var hinted = new HashSet<string>();
            foreach (var removed in ctx.ToRemove)
            {
                foreach (var reqId in removed.Requires)
                {
                    if (!SdkRegistry.TryGet(reqId, out var req)) continue;
                    if (!ctx.AfterRemove.Contains(req)) continue;
                    if (ctx.AfterRemove.Any(o => o.Requires.Contains(req.Id))) continue;
                    if (!hinted.Add(req.Id)) continue;

                    report.Add(IssueLevel.Info, IssueScope.Remove, req.Id,
                        $"Sau khi gỡ, không còn module nào cần '{req.DisplayName}'. Có thể gỡ thêm nếu không dùng.");
                }
            }
        }

        /// <summary>Đi theo Requires từ module này, nếu quay lại chính nó thì có vòng.</summary>
        static bool IsInCycle(ISdkModule start)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<ISdkModule>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var reqId in current.Requires)
                {
                    if (reqId == start.Id) return true;
                    if (!visited.Add(reqId)) continue;
                    if (SdkRegistry.TryGet(reqId, out var req)) stack.Push(req);
                }
            }

            return false;
        }
    }
}
