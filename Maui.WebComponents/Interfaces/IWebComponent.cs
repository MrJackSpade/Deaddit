namespace Maui.WebComponents.Interfaces
{
    public interface IWebComponent
    {
        List<IWebComponent> Children { get; }

        string Id { get; }

        public string InnerText { get; }

        Dictionary<string, string> Style { get; }
    }
}