using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Base.ToolSDK.Editor
{
    public enum IssueLevel { Info, Warning, Error }
    public enum IssueScope { Install, Remove }

    public class CompatibilityIssue
    {
        public IssueLevel Level;
        public IssueScope Scope;
        public string ModuleId;
        public string Message;
    }

    public class CompatibilityReport
    {
        public readonly List<CompatibilityIssue> Issues = new List<CompatibilityIssue>();

        public void Add(IssueLevel level, IssueScope scope, string moduleId, string message)
        {
            Issues.Add(new CompatibilityIssue
            {
                Level = level,
                Scope = scope,
                ModuleId = moduleId,
                Message = message
            });
        }

        public bool HasError(IssueScope scope)
        {
            return Issues.Any(i => i.Scope == scope && i.Level == IssueLevel.Error);
        }
    }

    /// <summary>
    /// Cài và gỡ là hai lần chạy riêng, nên mỗi bên được kiểm tra trên một tập riêng:
    /// AfterInstall cho luật cài, AfterRemove cho luật gỡ.
    /// </summary>
    public class CompatibilityContext
    {
        public readonly IReadOnlyList<ISdkModule> ToInstall;
        public readonly IReadOnlyList<ISdkModule> AutoAdded;
        public readonly IReadOnlyList<ISdkModule> ToRemove;
        public readonly BuildTarget Target;

        /// <summary>Module đang có trong project (đã cài hoặc cài dở).</summary>
        public readonly IReadOnlyList<ISdkModule> Existing;

        /// <summary>
        /// Tập tồn tại sau khi CÀI: đang có + sắp cài. KHÔNG trừ module sắp gỡ,
        /// vì gỡ là lần chạy riêng và chưa xảy ra lúc cài.
        /// </summary>
        public readonly IReadOnlyList<ISdkModule> AfterInstall;

        /// <summary>Tập tồn tại sau khi GỠ: đang có trừ sắp gỡ (không tính module sắp cài).</summary>
        public readonly IReadOnlyList<ISdkModule> AfterRemove;

        public CompatibilityContext(
            IEnumerable<ISdkModule> toInstall,
            IEnumerable<ISdkModule> autoAdded,
            IEnumerable<ISdkModule> toRemove,
            BuildTarget target)
        {
            ToInstall = toInstall.ToList();
            AutoAdded = autoAdded.ToList();
            ToRemove = toRemove.ToList();
            Target = target;

            Existing = SdkRegistry.All
                .Where(m => m.GetInstallState() != SdkInstallState.NotInstalled)
                .ToList();
            AfterInstall = SdkRegistry.All
                .Where(m => Existing.Contains(m) || ToInstall.Contains(m))
                .ToList();
            AfterRemove = Existing.Where(m => !ToRemove.Contains(m)).ToList();
        }
    }

    /// <summary>Một luật tương thích. Thêm luật mới = thêm một class, tự được nhận.</summary>
    public interface ICompatibilityRule
    {
        string Id { get; }
        void Evaluate(CompatibilityContext ctx, CompatibilityReport report);
    }

    public static class CompatibilityEngine
    {
        static List<ICompatibilityRule> _rules;

        static List<ICompatibilityRule> Rules
        {
            get
            {
                if (_rules != null) return _rules;
                _rules = new List<ICompatibilityRule>();
                foreach (var type in TypeCache.GetTypesDerivedFrom<ICompatibilityRule>())
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                    _rules.Add((ICompatibilityRule)Activator.CreateInstance(type));
                }
                _rules = _rules.OrderBy(r => r.Id).ToList();
                return _rules;
            }
        }

        public static CompatibilityReport Evaluate(CompatibilityContext ctx)
        {
            var report = new CompatibilityReport();
            foreach (var rule in Rules) rule.Evaluate(ctx, report);
            return report;
        }
    }
}
