using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using System.Diagnostics;
using System.Text;

namespace Deaddit.Utils
{
    public class PreferencesVisitTracker : IVisitTracker
    {
        private static readonly DateTime BaseDate = new(2000, 1, 1);

        private readonly HashSet<string> _loaded = [];

        private readonly Dictionary<string, HashSet<string>> _visited = [];

        private readonly Dictionary<string, List<Visit>> _visits = [];

        public static Visit Deserialize(string serialized)
        {
            // Convert the string back to byte array
            byte[] bytes = Encoding.ASCII.GetBytes(serialized);

            // Extract the date (first 4 bytes)
            int daysSinceBaseDate = BitConverter.ToInt32(bytes, 0);

            // Calculate the DateTime
            DateTime date = BaseDate.AddDays(daysSinceBaseDate);

            // Extract the name (bytes after the first 4, up to the \0 terminator)
            string name = Encoding.ASCII.GetString(bytes, 4, bytes.Length - 4); // -5 to exclude \0 terminator

            return new Visit { Date = date, Name = name };
        }

        public static IEnumerable<string> SplitVisits(string visits)
        {
            StringBuilder thisPart = new();

            foreach (char c in visits)
            {
                if (thisPart.Length <= 8)
                {
                    thisPart.Append(c);
                }
                else
                {
                    if (c == '\0')
                    {
                        yield return thisPart.ToString();
                        thisPart.Clear();
                    }
                    else
                    {
                        thisPart.Append(c);
                    }
                }
            }
        }

        public bool HasVisited(ApiThing thing)
        {
            this.TryLoad(thing.SubRedditId);

            return _visited[thing.SubRedditId].Contains(thing.Name);
        }

        public void Visit(ApiThing thing)
        {
            this.TryLoad(thing.SubRedditId);

            HashSet<string> visited = _visited[thing.SubRedditId];
            List<Visit> visits = _visits[thing.SubRedditId];

            if (visited.Add(thing.Name))
            {
                visits.Add(new Visit()
                {
                    Date = DateTime.Now,
                    Name = thing.Name
                });

                StringBuilder visitedString = new();

                foreach (Visit visit in visits)
                {
                    string visitString = Serialize(visit);
                    visitedString.Append(visitString);
                }

                Preferences.Set($"{nameof(PreferencesVisitTracker)}_{thing.SubRedditId}", visitedString.ToString());
            }
        }

        private static string Serialize(Visit visit)
        {
            // Convert Date to days since 2000/01/01
            int daysSinceBaseDate = (visit.Date - BaseDate).Days;

            // Convert days to byte array (4 bytes)
            byte[] dateBytes = BitConverter.GetBytes(daysSinceBaseDate);

            // Convert Name to byte array (using ASCII encoding since it uses 1 byte per character)
            byte[] nameBytes = Encoding.ASCII.GetBytes(visit.Name);

            // Concatenate the date bytes and name bytes with a \0 terminator
            byte[] result = new byte[dateBytes.Length + nameBytes.Length + 1];
            Array.Copy(dateBytes, 0, result, 0, dateBytes.Length);
            Array.Copy(nameBytes, 0, result, dateBytes.Length, nameBytes.Length);
            result[^1] = 0; // \0 terminator

            return Encoding.ASCII.GetString(result);
        }

        private void TryLoad(string key)
        {
            if (_loaded.Contains(key))
            {
                return;
            }

            HashSet<string> visited = [];
            List<Visit> visits = [];
            _loaded.Add(key);
            _visited.Add(key, visited);
            _visits.Add(key, visits);

            string visitString = Preferences.Get($"{nameof(PreferencesVisitTracker)}_{key}", "");

            foreach (string s in SplitVisits(visitString))
            {
                if (s.Length == 0)
                {
                    continue;
                }

                try
                {
                    Visit visit = Deserialize(s);

                    visited.Add(visit.Name);
                    visits.Add(visit);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
    }

    public class Visit
    {
        public DateTime Date { get; set; }

        public string Name { get; set; }
    }
}