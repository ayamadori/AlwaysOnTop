using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.Services.Store;
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
        private readonly DispatcherTimer autoRefreshTimer;
        private string defaultUA;
        // https://qiita.com/niwasawa/items/df30ffddf2e709b2ca43
        private const string mobileUA = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Mobile/15E148 Safari/604.1";


        public MainPage()
        {
            this.InitializeComponent();

            // https://www.eternalcoding.com/?p=1952
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(TitleBar);

            // Setup AutoRefresh timer
            autoRefreshTimer = new DispatcherTimer();
            autoRefreshTimer.Tick += (s, e) => { OpenBrowser(); };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await BrowserWindow.EnsureCoreWebView2Async();
            defaultUA = BrowserWindow.CoreWebView2.Settings.UserAgent;
            BrowserWindow.CoreWebView2.ContainsFullScreenElementChanged += BrowserWindow_ContainsFullScreenElementChanged;

            if (e.Parameter is Uri) // Protocol launch
            {
                string param = ((Uri)e.Parameter).AbsoluteUri;
                string uri = Uri.UnescapeDataString(param.Split('=')[1]);
                AddressBox.Text = uri;
                OpenBrowser();

                // Change to CompactOverlay mode automatically
                var applicationView = ApplicationView.GetForCurrentView();
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(500, 500); // Max size
                bool modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                if (modeSwitched)
                {
                    // modeSwitched is sometimes false. Why?
                    AOTButton.Visibility = Visibility.Collapsed;
                    BackButton.Visibility = Visibility.Visible;
                }
            }
        }

        private async void AOTButton_Click(object sender, RoutedEventArgs e)
        {
            // https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/

            bool modeSwitched;
            var applicationView = ApplicationView.GetForCurrentView();

            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            compactOptions.CustomSize = new Windows.Foundation.Size(500, 500); // Max size
            modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
            if (modeSwitched)
            {
                AOTButton.Visibility = Visibility.Collapsed;
                BackButton.Visibility = Visibility.Visible;
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/

            bool modeSwitched;
            var applicationView = ApplicationView.GetForCurrentView();
            modeSwitched = await applicationView.TryEnterViewModeAsync(ApplicationViewMode.Default);
            if (modeSwitched)
            {
                AOTButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
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

        private void BrowserWindow_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            LoadingIndicator.IsActive = true;
            LoadingIndicator.Visibility = Visibility.Visible;
            RefreshButton.Visibility = Visibility.Collapsed;
        }

        private void BrowserWindow_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            LoadingIndicator.IsActive = false;
            LoadingIndicator.Visibility = Visibility.Collapsed;
            ErrorWindow.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;
            BrowserWindow.Visibility = Visibility.Visible;
            TitleBlock.Text = BrowserWindow.CoreWebView2.DocumentTitle;
            AddressBox.Text = BrowserWindow.Source.AbsoluteUri;
        }
        private void BrowserWindow_ContainsFullScreenElementChanged(CoreWebView2 sender, object args)
        {
            var applicationView = ApplicationView.GetForCurrentView();

            if (sender.ContainsFullScreenElement)
            {
                CommBar.Visibility = Visibility.Collapsed;
                TitleBar.Visibility = Visibility.Collapsed;

                if (applicationView.ViewMode == ApplicationViewMode.Default)
                {                   
                    applicationView.TryEnterFullScreenMode();
                }
                else if (applicationView.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    // 281 = 500 * (9/16)
                    bool success = applicationView.TryResizeView(new Windows.Foundation.Size(500, 281));
                }
            }
            else
            {
                CommBar.Visibility = Visibility.Visible;
                TitleBar.Visibility = Visibility.Visible;

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
            MobileViewButton.Visibility = Visibility.Collapsed;
            PCViewButton.Visibility = Visibility.Visible;
            OpenBrowser();
        }

        private void PCViewButton_Click(object sender, RoutedEventArgs e)
        {
            MobileViewButton.Visibility = Visibility.Visible;
            PCViewButton.Visibility = Visibility.Collapsed;
            OpenBrowser();
        }

        private void OpenBrowser()
        {
            Howtouse.Visibility = Visibility.Collapsed;
            string address = AddressBox.Text;

            // Ensure URI to start with http(s)://
            if (!address.StartsWith("http://") && !address.StartsWith("https://"))
            {
                address = "https://" + address;
                AddressBox.Text = address;
            }

            // Check URI
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute) == true)
            {
                Uri uri = new Uri(address);

                if (MobileViewButton.Visibility == Visibility.Collapsed) // in Mobile View
                {
                    // Change UserAgent
                    BrowserWindow.CoreWebView2.Settings.UserAgent = mobileUA;
                }
                else
                {
                    // Reset UA
                    BrowserWindow.CoreWebView2.Settings.UserAgent = defaultUA;
                }
                BrowserWindow.Source = uri;
            }
            else
            {
                BrowserWindow.Visibility = Visibility.Collapsed;
                ErrorTitle.Text = "Invalid web address";
                ErrorContent.Text = "Check and re-enter web address";
                ErrorWindow.Visibility = Visibility.Visible;
                TitleBlock.Text = ErrorTitle.Text;
            }
        }

        private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            var uriBrowser = new Uri("https://www.microsoft.com/");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriBrowser);
        }

        private void AutoRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            autoRefreshTimer.Stop();
            string content = (sender as RadioMenuFlyoutItem).Text;
            if (content.Contains(" "))
            {
                autoRefreshTimer.Interval = new TimeSpan(0, 0, int.Parse(content.Substring(0, 2)));
                autoRefreshTimer.Start();
            }
        }
        private async void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            StoreContext storeContext = StoreContext.GetDefault();
            string StoreId = "9PFX1DR44QZC";
            StorePurchaseResult result = await storeContext.RequestPurchaseAsync(StoreId);
            if (result.ExtendedError != null)
            {
                Debug.WriteLine(result.ExtendedError);
                return;
            }

            switch (result.Status)
            {
                case StorePurchaseStatus.AlreadyPurchased: // should never get this for a managed consumable since they are stackable
                    Debug.WriteLine("You already bought this consumable.");
                    break;

                case StorePurchaseStatus.Succeeded:
                    Debug.WriteLine("You bought.");
                    break;

                case StorePurchaseStatus.NotPurchased:
                    Debug.WriteLine("Product was not purchased, it may have been canceled.");
                    break;

                case StorePurchaseStatus.NetworkError:
                    Debug.WriteLine("Product was not purchased due to a network error.");
                    break;

                case StorePurchaseStatus.ServerError:
                    Debug.WriteLine("Product was not purchased due to a server error.");
                    break;

                default:
                    Debug.WriteLine("Product was not purchased due to an unknown error.");
                    break;
            }
        }
    }
}
