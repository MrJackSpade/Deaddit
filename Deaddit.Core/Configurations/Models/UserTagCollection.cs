namespace Deaddit.Core.Configurations.Models
{
    public class UserTagCollection
    {
        private readonly string _filePath;

        private readonly Dictionary<string, string> _tags = new(StringComparer.OrdinalIgnoreCase);

        public UserTagCollection(string filePath)
        {
            _filePath = filePath;
            this.Load();
        }

        public string? GetTag(string username)
        {
            return _tags.TryGetValue(username, out string? tag) ? tag : null;
        }

        public void SetTag(string username, string tag)
        {
            _tags[username] = tag;
            this.Flush();
        }

        public void RemoveTag(string username)
        {
            _tags.Remove(username);
            this.Flush();
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(_filePath))
            {
                int separator = line.IndexOf('\t');

                if (separator > 0)
                {
                    string username = line[..separator];
                    string tag = line[(separator + 1)..];
                    _tags[username] = tag;
                }
            }
        }

        private void Flush()
        {
            string directory = Path.GetDirectoryName(_filePath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(_filePath, _tags.Select(kvp => $"{kvp.Key}\t{kvp.Value}"));
        }
    }
}
