using System;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace AlwaysOnTop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SharePage : Page
    {
        public SharePage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // refer to https://msdn.microsoft.com/en-us/library/windows/apps/mt243292.aspx
            ShareOperation shareOperation = (ShareOperation)e.Parameter;
            Uri uri;
            string url = "";

            if (shareOperation.Data.Contains(StandardDataFormats.WebLink)) // URI
            {
                uri = await shareOperation.Data.GetWebLinkAsync();
                url = uri.AbsoluteUri;

                Debug.WriteLine("Uri: " + url);
            }

            if (url.StartsWith("http"))
            {
                // refer to https://msdn.microsoft.com/library/windows/apps/mt228341
                uri = new Uri($"alwaysontop://?q={Uri.EscapeDataString(url)}");
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            }

            // Share completed
            shareOperation.ReportCompleted();
        }
    }
}
