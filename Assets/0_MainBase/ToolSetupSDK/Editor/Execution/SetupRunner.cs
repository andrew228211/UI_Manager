using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.ToolSDK.Editor
{
    [Serializable]
    public class SetupState
    {
        public bool running;
        public string title;
        public int index;
        public bool started;
        public long startedTicks;
        public string error;
        public List<StepData> steps = new List<StepData>();
        public List<string> log = new List<string>();
    }

    /// <summary>
    /// Chạy lần lượt các bước. Trạng thái lưu vào SessionState (sống qua domain reload) và
    /// file trong Library/ (còn lại sau khi tắt Unity). Sau compile runner tự chạy tiếp từ bước đang dở.
    /// Nếu Unity bị đóng giữa chừng, lần mở sau trạng thái được nạp lại là "bị gián đoạn"
    /// (không tự chạy tiếp); người dùng chạy lại plan để hoàn tất.
    /// </summary>
    [InitializeOnLoad]
    public static class SetupRunner
    {
        const string Key = "Base.ToolSDK.SetupState";
        static readonly TimeSpan StepTimeout = TimeSpan.FromMinutes(5);

        static SetupState _state;
        static ISetupStep _current;

        static string FilePath =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Library", "ToolSDK", "SetupState.json");

        static SetupRunner()
        {
            _state = Load();
            if (_state.running)
            {
                // Sau reload tính lại thời gian chờ từ bây giờ: thời gian compile không tính vào bước.
                _state.startedTicks = DateTime.UtcNow.Ticks;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            Save();
        }

        public static bool IsRunning => _state.running;
        public static SetupState State => _state;

        public static bool Start(string title, IList<StepData> steps)
        {
            if (_state.running || steps == null || steps.Count == 0) return false;

            _state = new SetupState { running = true, title = title };
            _state.steps.AddRange(steps);
            _current = null;
            Log("Bắt đầu: " + title + " (" + steps.Count + " bước)");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return true;
        }

        /// <summary>
        /// Dừng trước bước kế tiếp. Request Package Manager đang chạy dở vẫn tiếp tục trong Unity
        /// và không được hoàn tác, nên project có thể ở trạng thái cài dở.
        /// </summary>
        public static void Cancel()
        {
            if (!_state.running) return;
            Finish("Đã hủy bởi người dùng. Project có thể đang ở trạng thái cài dở.");
        }

        static void Tick()
        {
            if (!_state.running)
            {
                EditorApplication.update -= Tick;
                return;
            }

            // Đợi Unity compile / import xong rồi mới làm tiếp.
            // Thời gian này không tính vào thời gian chờ của bước.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (_state.started) _state.startedTicks = DateTime.UtcNow.Ticks;
                return;
            }

            if (_state.index >= _state.steps.Count)
            {
                Finish(null);
                return;
            }

            if (_current == null) _current = SetupStepFactory.Create(_state.steps[_state.index]);

            if (!_state.started)
            {
                if (_current.IsDone())
                {
                    Log("Bỏ qua (đã có sẵn): " + _current.Label);
                    Advance();
                    return;
                }

                // Lưu "đã bắt đầu" TRƯỚC khi chạy Begin: nếu Begin kích hoạt reload
                // thì lần chạy tiếp sẽ theo dõi kết quả thay vì chạy Begin lần hai.
                _state.started = true;
                _state.startedTicks = DateTime.UtcNow.Ticks;
                Log("Đang chạy: " + _current.Label);

                try
                {
                    _current.Begin();
                }
                catch (Exception e)
                {
                    Finish("Lỗi ở bước '" + _current.Label + "': " + e.Message);
                }
                return;
            }

            StepPoll poll;
            try
            {
                poll = _current.Poll();
            }
            catch (Exception e)
            {
                Finish("Lỗi ở bước '" + _current.Label + "': " + e.Message);
                return;
            }

            switch (poll)
            {
                case StepPoll.Done:
                    Log("Xong: " + _current.Label);
                    Advance();
                    break;
                case StepPoll.Failed:
                    Finish("Lỗi ở bước '" + _current.Label + "': " + _current.Error);
                    break;
                default:
                    if (DateTime.UtcNow - new DateTime(_state.startedTicks, DateTimeKind.Utc) > StepTimeout)
                        Finish("Quá thời gian chờ ở bước '" + _current.Label + "'");
                    break;
            }
        }

        static void Advance()
        {
            _state.index++;
            _state.started = false;
            _current = null;
            Save();
        }

        static void Finish(string error)
        {
            _state.running = false;
            _state.error = error;
            Log(string.IsNullOrEmpty(error) ? "HOÀN TẤT: " + _state.title : "DỪNG: " + error);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning("[ToolSDK] " + error);
            _current = null;
            EditorApplication.update -= Tick;
            Save();
        }

        static void Log(string line)
        {
            _state.log.Add(line);
            if (_state.log.Count > 200) _state.log.RemoveAt(0);
            Save();
        }

        static void Save()
        {
            var json = JsonUtility.ToJson(_state);
            SessionState.SetString(Key, json);

            try
            {
                var path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
            }
            catch
            {
                // Không ghi được file thì vẫn còn SessionState; không làm hỏng quá trình chạy.
            }
        }

        static SetupState Load()
        {
            // 1. Đang trong cùng phiên Unity (vừa domain reload): dùng tiếp để runner chạy tiếp.
            var json = SessionState.GetString(Key, string.Empty);
            var fromSession = Parse(json);
            if (fromSession != null) return fromSession;

            // 2. Unity vừa được mở lại: đọc file, nhưng KHÔNG tự chạy tiếp.
            try
            {
                var path = FilePath;
                if (File.Exists(path))
                {
                    var fromFile = Parse(File.ReadAllText(path));
                    if (fromFile != null)
                    {
                        if (fromFile.running)
                        {
                            fromFile.running = false;
                            fromFile.started = false;
                            fromFile.error = "Bị gián đoạn khi Unity đóng ở bước "
                                             + Mathf.Min(fromFile.index + 1, fromFile.steps.Count) + "/" + fromFile.steps.Count
                                             + ". Hãy kiểm tra trạng thái SDK và chạy lại.";
                            fromFile.log.Add("DỪNG: " + fromFile.error);
                        }
                        return fromFile;
                    }
                }
            }
            catch
            {
                // File hỏng thì bỏ qua.
            }

            return new SetupState();
        }

        static SetupState Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<SetupState>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
