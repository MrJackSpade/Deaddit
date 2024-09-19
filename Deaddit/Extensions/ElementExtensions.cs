namespace Deaddit.Extensions
{
    public static class ElementExtensions
    {
        public static T? Closest<T>(this Element element) where T : Element
        {
            Element current = element;

            while (current != null)
            {
                if (current is T t)
                {
                    return t;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}