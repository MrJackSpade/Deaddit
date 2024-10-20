using Deaddit.Core.Reddit.Models.Requests;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public abstract class ThingDefinition
    {
        public abstract Enum? DefaultSort { get; }

        public virtual string DisplayName { get; }

        public virtual bool FilteredByDefault => true;

        public abstract ApiEndpointDefinition EndpointDefinition { get; }

        public abstract ThingKind Kind { get; }

        public string Name { get; }

        public ThingDefinition(string name, char prefix)
        {
            if (prefix != '\0')
            {
                Name = CleanName(name, prefix);
                DisplayName = $"/{prefix}/{Name}";
            }
            else
            {
                Name = name;
                DisplayName = name;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is ThingDefinition other)
            {
                return other.Name == Name && other.Kind == Kind;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        protected static string CleanName(string name, char prefix)
        {
            if (name.StartsWith($"/{prefix}/", StringComparison.OrdinalIgnoreCase))
            {
                return name[3..];
            }

            if (name.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
            {
                return name[2..];
            }

            if (!name.Contains('/'))
            {
                return name;
            }

            throw new ArgumentException("Invalid name");
        }
    }
}