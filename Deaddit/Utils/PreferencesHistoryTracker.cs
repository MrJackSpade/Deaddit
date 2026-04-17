using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Interfaces;
using System.Text.Json;

namespace Deaddit.Utils
{
    public class PreferencesHistoryTracker : IHistoryTracker
    {
        private const int MaxHistorySize = 1000;

        private const string PreferenceKey = "PostHistory";

        private readonly object _lock = new();

        private List<string>? _history;

        public void AddToHistory(ApiPost post, bool fromHistoryPage = false)
        {
            if (fromHistoryPage)
            {
                return;
            }

            lock (_lock)
            {
                this.EnsureLoaded();

                // Remove if already exists (to move it to top)
                _history!.Remove(post.Name);

                // Add to the beginning (most recent)
                _history.Insert(0, post.Name);

                // Trim to max size
                if (_history.Count > MaxHistorySize)
                {
                    _history.RemoveRange(MaxHistorySize, _history.Count - MaxHistorySize);
                }

                this.Save();
            }
        }

        public IReadOnlyList<string> GetHistory()
        {
            lock (_lock)
            {
                this.EnsureLoaded();
                return _history!.AsReadOnly();
            }
        }

        public void ImportHistory(List<string> history)
        {
            lock (_lock)
            {
                _history = history ?? [];
                this.Save();
            }
        }

        private void EnsureLoaded()
        {
            if (_history != null)
            {
                return;
            }

            string json = Preferences.Get(PreferenceKey, "[]");

            try
            {
                _history = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch
            {
                _history = [];
            }
        }

        private void Save()
        {
            string json = JsonSerializer.Serialize(_history);
            Preferences.Set(PreferenceKey, json);
        }
    }
}