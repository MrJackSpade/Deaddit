namespace Deaddit.Components
{
    public class PanZoomImageContainer : ContentView
    {
        private readonly Image image;
        private double currentScale = 1;
        private double startScale = 1;
        private double panX = 0;
        private double panY = 0;

        public PanZoomImageContainer()
        {
            image = new Image
            {
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            Content = image;

            PinchGestureRecognizer pinchGesture = new();
            pinchGesture.PinchUpdated += this.OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);

            PanGestureRecognizer panGesture = new();
            panGesture.PanUpdated += this.OnPanUpdated;
            GestureRecognizers.Add(panGesture);
        }

        public void SetImage(ImageSource source)
        {
            image.Source = source;
            this.ResetImagePosition();
        }

        private void ResetImagePosition()
        {
            currentScale = 1;
            startScale = 1;
            panX = 0;
            panY = 0;
            Content.Scale = 1;
            Content.TranslationX = 0;
            Content.TranslationY = 0;
        }

        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                startScale = currentScale;
            }

            if (e.Status == GestureStatus.Running)
            {
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale); // Prevent scaling smaller than original size
                Content.Scale = currentScale;
            }
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    double maxTranslationX = Content.Width * (currentScale - 1) / 2;
                    double maxTranslationY = Content.Height * (currentScale - 1) / 2;

                    Content.TranslationX = Math.Clamp(panX + e.TotalX, -maxTranslationX, maxTranslationX);
                    Content.TranslationY = Math.Clamp(panY + e.TotalY, -maxTranslationY, maxTranslationY);
                    break;

                case GestureStatus.Completed:
                    panX = Content.TranslationX;
                    panY = Content.TranslationY;
                    break;
            }
        }
    }
}