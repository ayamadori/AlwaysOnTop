using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
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
            Window.Current.SetTitleBar(TitleBar);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Uri) // Protocol launch
            {
                string param = ((Uri)e.Parameter).AbsoluteUri.Split('?')[1];

                string[] protoParams = param.Split('&'); // Split the paramters into an array

                for (int i = 0; i < protoParams.Length; i++)
                {
                    if(protoParams[i].Contains("videoIndex="))
                    {
                         int videoIndex = Int32.Parse(protoParams[i].Split('=')[1]);
                         FullscreenVideoByIndex(videoIndex);
                    }
                    if (protoParams[i].Contains("url="))
                    {
                        string uri = Uri.UnescapeDataString(protoParams[i].Split('=')[1]);
                        AddressBox.Text = uri;
                    }

                }

                OpenBrowser();

                // Change to CompactOverlay mode automatically
                var applicationView = ApplicationView.GetForCurrentView();
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(500, 500); // Max size
                bool modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                if (modeSwitched)
                {
                    // modeSwitched is sometimes false. Why?
                    AOTButton.IsChecked = true;
                }
            }
        }

        private async void AOTButton_Click(object sender, RoutedEventArgs e)
        {
            // https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/

            bool modeSwitched;
            var applicationView = ApplicationView.GetForCurrentView();

            if (AOTButton.IsChecked == true)
            {
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(500, 500); // Max size
                modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
            }
            else
            {
                modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser();
        }

        private void AddressBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                OpenBrowser();
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.contentdialog.aspx
            var dlg = new AboutDialog();
            await dlg.ShowAsync();
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

        private async void BrowserWindow_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            LoadingIndicator.IsActive = false;
            LoadingIndicator.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
            var dlg = new MessageDialog(e.WebErrorStatus.ToString(), "Navigation Failed");
            await dlg.ShowAsync();
        }

        private void BrowserWindow_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var applicationView = ApplicationView.GetForCurrentView();

            if (sender.ContainsFullScreenElement)
            {
                TitleBar.Visibility = Visibility.Collapsed;
                CommBar.Visibility = Visibility.Collapsed;

                if (applicationView.ViewMode == ApplicationViewMode.Default)
                {
                    applicationView.TryEnterFullScreenMode();
                }
                else if(applicationView.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    // 281 = 500 * (9/16)
                    bool success = applicationView.TryResizeView(new Windows.Foundation.Size(500, 281));
                }
            }
            else
            {
                TitleBar.Visibility = Visibility.Visible;
                CommBar.Visibility = Visibility.Visible;

                if (applicationView.IsFullScreenMode)
                {
                    applicationView.ExitFullScreenMode();
                }
                else if (applicationView.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    bool success = applicationView.TryResizeView(new Windows.Foundation.Size(500, 500)); // Max size
                }
            }
        }

        private void MobileViewButton_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser();
        }

        private void FullscreenVideoByIndex(int videoIndex)
        {
            BrowserWindow.DOMContentLoaded += async (s, e) =>
            {//alwaysontop:?url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3DyNHUIM0ZfTE%0A&videoIndex=0

                // Ideally, this will eventually hide EVERYTHING except the video, then recreate a simple video player on top of the existing video
                CommBar.Visibility = Visibility.Collapsed;
                

                // Switch to Compact overlay and set the size 
                var applicationView = ApplicationView.GetForCurrentView();
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(500, 500 * 0.5625);
                await applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);


                // Hide every element
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('*').forEach(element=>{element.style['margin'] = '0px';})" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('*').forEach(element=>{element.style['padding'] = '0px';})" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('*').forEach(element=>{element.style['position'] = 'absolute';})" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('*').forEach(element=>{element.style['z-index'] = '-99';})" });


                // Grab the given video and style it to make it look fullscreen (full screen API does not always work)
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['position'] = 'fixed'; " });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['display'] = 'block'; " });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['top'] = '0';" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['left'] = '0';" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['z-index'] = '999';" });

                // Why doesn't it care about the viewport size when applying CSS?

                // Set min and max height of video, lock it to filling the window
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['max-width'] = '99vw !important';" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['max-height'] = '99vh !important';" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['min-height'] = '99vh !important';" });
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['min-width'] = '99vw !important';" });


                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].style['overflow'] = 'hidden';" });


                // Try using the fullscreen API
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"if(document.querySelectorAll('video')[" + videoIndex + "].requestFullscreen) document.querySelectorAll('video')[" + videoIndex + "].requestFullscreen();" });

                // Play the video
                await BrowserWindow.InvokeScriptAsync("eval", new string[] { @"document.querySelectorAll('video')[" + videoIndex + "].play();" });
                
            };
        }

        private async void OpenBrowser()
        {
            string address = AddressBox.Text;
            if (address.StartsWith("http://") || address.StartsWith("https://"))
            {
                Uri uri = new Uri(address);
                if (MobileViewButton.IsChecked == true)
                {
                    // Change UserAgent and refresh
                    HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
                    // https://qiita.com/niwasawa/items/44aaae7a1f942b62b9c1
                    string ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1";
                    requestMsg.Headers.Add("User-Agent", ua);
                    BrowserWindow.NavigateWithHttpRequestMessage(requestMsg);
                }
                else
                {
                    BrowserWindow.Navigate(new Uri(address));
                }
            }
            else
            {
                var dlg = new MessageDialog("NOTE: Web address must start with http(s)://", "Invalid web address");
                await dlg.ShowAsync();
            }
        }
    }
}
