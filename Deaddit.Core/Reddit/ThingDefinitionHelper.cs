using Deaddit.Core.Reddit.Models.ThingDefinitions;

namespace Deaddit.Core.Reddit
{
    public static class ThingDefinitionHelper
    {
        public static MessageDefinition ForMessages()
        {
            return new MessageDefinition();
        }

        public static MultiDefinition ForMulti(string name)
        {
            return new MultiDefinition(name);
        }

        public static SubRedditDefinition ForSubReddit(string name)
        {
            return new SubRedditDefinition(name);
        }

        public static UserDefinition ForUser(string name)
        {
            return new UserDefinition(name);
        }

        public static ThingDefinition FromName(string name)
        {
            if (name.StartsWith('/'))
            {
                name = name[1..];
            }

            if (!name.Contains('/') || name.StartsWith("r/", StringComparison.OrdinalIgnoreCase))
            {
                return new SubRedditDefinition(name);
            }

            if (name.StartsWith("m/"))
            {
                return new MultiDefinition(name);
            }

            if (name.StartsWith("u/"))
            {
                return new UserDefinition(name);
            }

            throw new NotImplementedException();
        }
    }
}