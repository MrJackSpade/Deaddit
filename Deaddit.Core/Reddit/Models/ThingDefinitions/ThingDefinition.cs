using Deaddit.Core.Reddit.Models.Requests;
using Reddit.Api.Models.Enums;

namespace Deaddit.Core.Reddit.Models.ThingDefinitions
{
    public abstract class ThingDefinition
    {
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

        public abstract Enum? DefaultSort { get; }

        public virtual string DisplayName { get; }

        public abstract ApiEndpointDefinition EndpointDefinition { get; }

        public virtual bool FilteredByDefault => true;

        public abstract ThingKind Kind { get; }

        public string Name { get; }

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
            return Name.GetHashCode();
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