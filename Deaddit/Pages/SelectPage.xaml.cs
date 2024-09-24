namespace Deaddit.Pages
{
    public partial class SelectPage : ContentPage
    {
        public event EventHandler<string>? OnSelect;

        public SelectPage(params string[] options)
        {
            this.InitializeComponent();
            this.PopulateOptions(options);
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private void PopulateOptions(string[] options)
        {
            foreach (string option in options)
            {
                Button button = new()
                {
                    Text = option,
                    BackgroundColor = Colors.LightGray,
                    TextColor = Colors.Black
                };

                button.Clicked += (s, e) =>
                {
                    OnSelect?.Invoke(this, option);
                    Navigation.PopAsync();
                };

                optionsStack.Children.Add(button);
            }
        }
    }
}