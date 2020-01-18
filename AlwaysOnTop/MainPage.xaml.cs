﻿using System;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Microsoft.UI.Xaml.Controls;

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
            AddressBox.Text = BrowserWindow.Source.AbsoluteUri;
        }

        private void BrowserWindow_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            LoadingIndicator.IsActive = false;
            LoadingIndicator.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Visible;

            ErrorDialog.Title = "Navigation Failed";
            ErrorDialog.Subtitle = e.WebErrorStatus.ToString();
            ErrorDialog.IsOpen = true;
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
                else if (applicationView.ViewMode == ApplicationViewMode.CompactOverlay)
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

            try
            {
                Uri uri = new Uri(address);

                if (MobileViewButton.IsChecked == true)
                {
                    // Change UserAgent and refresh
                    HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Get, uri);
                    // https://qiita.com/kapiecii/items/093ffd6f0b09ad775250
                    string ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/1.6.5b18.09.26.16 Mobile/16A366 Safari/605.1.15 _id/000002";
                    requestMsg.Headers.Add("User-Agent", ua);
                    BrowserWindow.NavigateWithHttpRequestMessage(requestMsg);
                }
                else
                {
                    BrowserWindow.Navigate(new Uri(address));
                }
            }
            catch
            {
                ErrorDialog.Title = "Invalid web address";
                ErrorDialog.Subtitle = "Check and re-enter web address";
                ErrorDialog.IsOpen = true;
            }
        }

        private async void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            var uriBrowser = new Uri("https://www.microsoft.com/");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriBrowser);
        }
    }
}
