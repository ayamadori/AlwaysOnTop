using System;
using Windows.Services.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AlwaysOnTop
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();

            // https://docs.microsoft.com/en-us/windows/uwp/monetize/launch-feedback-hub-from-your-app
            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
            {
                this.FeedbackButton.Visibility = Visibility.Visible;
            }
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            // https://docs.microsoft.com/en-us/windows/uwp/monetize/request-ratings-and-reviews
            var success = await StoreContext.GetDefault().RequestRateAndReviewAppAsync();
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            // https://docs.microsoft.com/en-us/windows/uwp/monetize/launch-feedback-hub-from-your-app
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }
    }
}
