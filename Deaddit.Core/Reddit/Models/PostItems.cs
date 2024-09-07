using System.Collections;

namespace Deaddit.Core.Reddit.Models
{
    public class PostItems(PostTargetKind kind) : IEnumerable<PostItem>
    {
        public List<PostItem> Items { get; set; } = [];

        public PostTargetKind Kind { get; set; } = kind;

        public void Add(PostItem item)
        {
            Items.Add(item);
        }

        public IEnumerator<PostItem> GetEnumerator()
        {
            return ((IEnumerable<PostItem>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }
}