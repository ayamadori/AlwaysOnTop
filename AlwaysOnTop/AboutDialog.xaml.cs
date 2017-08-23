using System;
using Windows.ApplicationModel;
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
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            // https://github.com/Microsoft/Windows-task-snippets/blob/master/tasks/Store-app-rating-pop-up.md
            var uriReview = new Uri($"ms-windows-store://REVIEW?PFN={Package.Current.Id.FamilyName}");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriReview);
        }
    }
}
