using System;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace AlwaysOnTop
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // https://www.eternalcoding.com/?p=1952
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(MainTitleBar);

            //coreTitleBar.ExtendViewIntoTitleBar = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Uri) // Protocol launch
            {
                string param = ((Uri)e.Parameter).AbsoluteUri;
                string uri = Uri.UnescapeDataString(param.Split('=')[1]);
                Debug.WriteLine("Uri2: " + uri);
                BrowserWindow.Navigate(new Uri(uri));
            }
        }

        private async void AOTButton_Click(object sender, RoutedEventArgs e)
        {
            CommBar.Visibility = Visibility.Collapsed;
            ToNormalButton.Visibility = Visibility.Visible;

            // https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/
            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            compactOptions.CustomSize = new Windows.Foundation.Size(480, 302); // 302=480*(9/16)+32(TitleBar)
            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            string address = AddressBox.Text;
            if (address.StartsWith("http"))
                BrowserWindow.Navigate(new Uri(address));
        }

        private void AddressBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string address = AddressBox.Text;
                if (address.StartsWith("http"))
                    BrowserWindow.Navigate(new Uri(address));
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.contentdialog.aspx
            var dlg = new AboutDialog();
            await dlg.ShowAsync();
        }

        private async void ToNormalButton_Click(object sender, RoutedEventArgs e)
        {
            CommBar.Visibility = Visibility.Visible;
            ToNormalButton.Visibility = Visibility.Collapsed;

            // https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/
            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
        }

        private void BrowserWindow_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            LoadingIndicator.IsActive = true;
            LoadingIndicator.Visibility = Visibility.Visible;
            RefreshButton.Visibility = Visibility.Collapsed;
        }

        private void BrowserWindow_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingIndicator.IsActive = false;
            LoadingIndicator.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
            TitleBlock.Text = BrowserWindow.DocumentTitle;
        }

        private void MobileViewButton_Click(object sender, RoutedEventArgs e)
        {
            // https://qiita.com/niwasawa/items/44aaae7a1f942b62b9c1
            string ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1";
            string address = AddressBox.Text;
            if (address.StartsWith("http"))
            {
                Uri uri = new Uri(address);
                if (MobileViewButton.IsChecked == true)
                {
                    // Change UserAgent and refresh
                    HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMsg.Headers.Add("User-Agent", ua);
                    BrowserWindow.NavigateWithHttpRequestMessage(requestMsg);
                }
                else
                {
                    BrowserWindow.Navigate(new Uri(address));
                }
            }
        }
    }
}
