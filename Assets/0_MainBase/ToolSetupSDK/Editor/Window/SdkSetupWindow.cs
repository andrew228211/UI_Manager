using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Base.ToolSDK.Editor
{
    /// <summary>Cửa sổ chính (UI Toolkit, dựng bằng code). Menu: Tools/ToolSDK/Setup SDK.</summary>
    public class SdkSetupWindow : EditorWindow
    {
        [MenuItem("Tools/ToolSDK/Setup SDK")]
        public static void Open()
        {
            var window = GetWindow<SdkSetupWindow>();
            window.titleContent = new GUIContent("ToolSDK");
            window.minSize = new Vector2(540, 560);
        }

        // Lưu lựa chọn để không mất khi domain reload.
        [SerializeField] List<string> _selected = new List<string>();

        // Nhớ lần làm mới trước runner có đang chạy không, để phát hiện lúc vừa chạy xong.
        // Lưu qua domain reload vì runner có thể kết thúc ngay sau một lần compile.
        [SerializeField] bool _wasRunning;

        readonly Dictionary<string, Label> _statusLabels = new Dictionary<string, Label>();
        readonly Dictionary<string, Toggle> _toggles = new Dictionary<string, Toggle>();

        Label _targetLabel;
        Label _compatLabel;
        Label _planLabel;
        Label _logLabel;
        Button _installButton;
        Button _removeButton;
        Button _cancelButton;

        // Kết quả tính toán gần nhất, dùng chung cho hiển thị và khi bấm nút.
        class Evaluation
        {
            public List<ISdkModule> ToInstall = new List<ISdkModule>();
            public List<ISdkModule> ToRemove = new List<ISdkModule>();
            public List<StepData> InstallSteps = new List<StepData>();
            public List<StepData> RemoveSteps = new List<StepData>();
            public List<string> RemoveNotes = new List<string>();
            public CompatibilityReport Report;
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            var title = new Label("ToolSDK — Setup SDK");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;
            root.Add(title);

            _targetLabel = new Label();
            _targetLabel.style.marginBottom = 6;
            root.Add(_targetLabel);

            // Danh sách SDK, nhóm theo Group.
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 150;
            root.Add(scroll);

            string lastGroup = null;
            foreach (var module in SdkRegistry.All)
            {
                if (module.Group != lastGroup)
                {
                    lastGroup = module.Group;
                    var groupLabel = new Label(module.Group);
                    groupLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    groupLabel.style.marginTop = 8;
                    scroll.Add(groupLabel);
                }
                scroll.Add(CreateRow(module));
            }

            _compatLabel = AddSection(root, "Kiểm tra tương thích");
            _planLabel = AddSection(root, "Kế hoạch (xem trước)");

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 6;
            _installButton = new Button(OnInstallClicked) { text = "Cài các SDK đã chọn" };
            _removeButton = new Button(OnRemoveClicked) { text = "Gỡ các SDK đã chọn" };
            _cancelButton = new Button(SetupRunner.Cancel) { text = "Hủy tiến trình" };
            buttons.Add(_installButton);
            buttons.Add(_removeButton);
            buttons.Add(_cancelButton);
            root.Add(buttons);

            _logLabel = AddSection(root, "Nhật ký");

            // Cập nhật định kỳ để thấy tiến trình chạy ngầm.
            root.schedule.Execute(Refresh).Every(700);
            Refresh();
        }

        VisualElement CreateRow(ISdkModule module)
        {
            var row = new VisualElement();
            row.style.marginBottom = 4;

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;

            var toggle = new Toggle { value = _selected.Contains(module.Id) };
            toggle.RegisterValueChangedCallback(e =>
            {
                if (e.newValue)
                {
                    if (!_selected.Contains(module.Id)) _selected.Add(module.Id);
                }
                else
                {
                    _selected.Remove(module.Id);
                }
                Refresh();
            });
            _toggles[module.Id] = toggle;
            top.Add(toggle);

            var name = new Label(module.DisplayName);
            name.style.flexGrow = 1;
            top.Add(name);

            var status = new Label();
            status.style.minWidth = 70;
            status.style.unityTextAlign = TextAnchor.MiddleRight;
            _statusLabels[module.Id] = status;
            top.Add(status);

            row.Add(top);

            if (!string.IsNullOrEmpty(module.Description))
            {
                var desc = new Label(module.Description);
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.marginLeft = 22;
                desc.style.opacity = 0.7f;
                row.Add(desc);
            }

            return row;
        }

        static Label AddSection(VisualElement parent, string heading)
        {
            var head = new Label(heading);
            head.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.style.marginTop = 8;
            parent.Add(head);

            var body = new Label();
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.paddingLeft = 6;
            body.style.paddingRight = 6;
            body.style.paddingTop = 4;
            body.style.paddingBottom = 4;
            body.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.15f));
            body.style.minHeight = 36;
            parent.Add(body);
            return body;
        }

        // Bỏ chọn tất cả (dùng sau khi chạy xong, tránh module vừa cài bị tính sang kế hoạch Gỡ).
        void ClearSelection()
        {
            _selected.Clear();
            foreach (var toggle in _toggles.Values) toggle.SetValueWithoutNotify(false);
        }

        Evaluation Evaluate()
        {
            var eval = new Evaluation();
            var selected = SdkRegistry.All.Where(m => _selected.Contains(m.Id)).ToList();

            // Chưa cài hoặc cài dở → có thể cài (tiếp). Đã cài hoặc cài dở → có thể gỡ.
            // Module cài dở xuất hiện ở cả hai, người dùng chọn nút Cài hoặc Gỡ.
            eval.ToInstall = selected.Where(m => m.GetInstallState() != SdkInstallState.Installed).ToList();
            eval.ToRemove = selected.Where(m => m.GetInstallState() != SdkInstallState.NotInstalled).ToList();

            var expanded = Planner.ExpandInstall(eval.ToInstall, out var autoAdded);
            var removeOrder = Planner.OrderForRemove(eval.ToRemove);

            var ctx = new CompatibilityContext(expanded, autoAdded, eval.ToRemove, EditorUserBuildSettings.activeBuildTarget);
            eval.Report = CompatibilityEngine.Evaluate(ctx);

            eval.InstallSteps = Planner.BuildInstallSteps(expanded);
            eval.RemoveSteps = Planner.BuildRemoveSteps(removeOrder, ctx.AfterRemove, out eval.RemoveNotes);
            return eval;
        }

        void Refresh()
        {
            if (_compatLabel == null) return;

            // Vừa chạy xong thành công → bỏ chọn. Nếu lỗi/hủy thì giữ nguyên để thử lại.
            var running = SetupRunner.IsRunning;
            if (_wasRunning && !running && string.IsNullOrEmpty(SetupRunner.State.error))
                ClearSelection();
            _wasRunning = running;

            _targetLabel.text = "Build target hiện tại: " + EditorUserBuildSettings.activeBuildTarget;

            foreach (var module in SdkRegistry.All)
            {
                if (!_statusLabels.TryGetValue(module.Id, out var label)) continue;
                switch (module.GetInstallState())
                {
                    case SdkInstallState.Installed:
                        label.text = "Đã cài";
                        label.style.color = new StyleColor(new Color(0.4f, 0.85f, 0.4f));
                        break;
                    case SdkInstallState.Partial:
                        label.text = "Cài dở";
                        label.style.color = new StyleColor(new Color(0.95f, 0.7f, 0.25f));
                        break;
                    default:
                        label.text = "Chưa cài";
                        label.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                        break;
                }
            }

            var eval = Evaluate();

            _compatLabel.text = FormatReport(eval.Report);
            _planLabel.text = FormatPlan(eval);
            _logLabel.text = FormatLog();

            _installButton.SetEnabled(!running && eval.InstallSteps.Count > 0 && !eval.Report.HasError(IssueScope.Install));
            _removeButton.SetEnabled(!running && eval.RemoveSteps.Count > 0 && !eval.Report.HasError(IssueScope.Remove));
            _cancelButton.SetEnabled(running);
        }

        static string FormatReport(CompatibilityReport report)
        {
            if (report.Issues.Count == 0) return "Không có vấn đề.";

            var sb = new StringBuilder();
            foreach (var i in report.Issues.OrderByDescending(x => x.Level))
            {
                var tag = i.Level == IssueLevel.Error ? "[LỖI]" : i.Level == IssueLevel.Warning ? "[CẢNH BÁO]" : "[INFO]";
                sb.AppendLine(tag + " " + i.Message);
            }
            return sb.ToString().TrimEnd();
        }

        static string StepText(StepData data)
        {
            var step = SetupStepFactory.Create(data);
            return step.Label + (step.IsDone() ? "  (đã có sẵn, sẽ bỏ qua)" : string.Empty);
        }

        static string FormatPlan(Evaluation eval)
        {
            if (eval.InstallSteps.Count == 0 && eval.RemoveSteps.Count == 0 && eval.RemoveNotes.Count == 0)
                return "Chưa có gì để làm. Tick chọn SDK ở trên.";

            var sb = new StringBuilder();
            if (eval.InstallSteps.Count > 0)
            {
                sb.AppendLine("Khi bấm Cài:");
                foreach (var s in eval.InstallSteps) sb.AppendLine("  • " + StepText(s));
            }
            if (eval.RemoveSteps.Count > 0 || eval.RemoveNotes.Count > 0)
            {
                sb.AppendLine("Khi bấm Gỡ:");
                foreach (var s in eval.RemoveSteps) sb.AppendLine("  • " + StepText(s));
                foreach (var n in eval.RemoveNotes) sb.AppendLine("  • " + n);
            }
            return sb.ToString().TrimEnd();
        }

        static string FormatLog()
        {
            var state = SetupRunner.State;
            var sb = new StringBuilder();

            if (state.running)
                sb.AppendLine("Đang chạy '" + state.title + "': bước " + Mathf.Min(state.index + 1, state.steps.Count) + "/" + state.steps.Count);

            var start = Mathf.Max(0, state.log.Count - 15);
            for (var i = start; i < state.log.Count; i++) sb.AppendLine(state.log[i]);

            return sb.Length == 0 ? "(chưa có)" : sb.ToString().TrimEnd();
        }

        void OnInstallClicked()
        {
            var eval = Evaluate();
            if (eval.Report.HasError(IssueScope.Install) || eval.InstallSteps.Count == 0) return;
            if (!Confirm("Cài SDK", eval.InstallSteps, Warnings(eval, IssueScope.Install))) return;
            SetupRunner.Start("Cài SDK", eval.InstallSteps);
            Refresh();
        }

        void OnRemoveClicked()
        {
            var eval = Evaluate();
            if (eval.Report.HasError(IssueScope.Remove) || eval.RemoveSteps.Count == 0) return;
            if (!Confirm("Gỡ SDK", eval.RemoveSteps, Warnings(eval, IssueScope.Remove))) return;
            SetupRunner.Start("Gỡ SDK", eval.RemoveSteps);
            Refresh();
        }

        static List<string> Warnings(Evaluation eval, IssueScope scope)
        {
            return eval.Report.Issues
                .Where(i => i.Level == IssueLevel.Warning && i.Scope == scope)
                .Select(i => i.Message)
                .Distinct()
                .ToList();
        }

        static bool Confirm(string title, List<StepData> steps, List<string> warnings)
        {
            var sb = new StringBuilder();

            if (warnings.Count > 0)
            {
                sb.AppendLine("CẢNH BÁO:");
                foreach (var w in warnings) sb.AppendLine("! " + w);
                sb.AppendLine();
            }

            sb.AppendLine("Sẽ thực hiện:");
            foreach (var s in steps) sb.AppendLine("• " + StepText(s));
            sb.AppendLine("\nUnity có thể compile lại script trong lúc chạy; tool sẽ tự chạy tiếp.");
            return EditorUtility.DisplayDialog(title, sb.ToString(), "Chạy", "Hủy");
        }
    }
}
