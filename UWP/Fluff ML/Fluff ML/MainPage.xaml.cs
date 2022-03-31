using e6API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Fluff_ML
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        RequestHost host { get; set; }
        public MainPage()
        {
            this.InitializeComponent();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            host = new RequestHost("Fluff ML/v1.0 (by EpsilonRho)");
        }

        private void LoginNextButton_Click(object sender, RoutedEventArgs e)
        {
            BuilderFlipView.SelectedIndex++;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if(BuilderFlipView.SelectedIndex > 0)
            {
                BuilderFlipView.SelectedIndex--;
            }
        }

        private void OpenBuilderButton_Click(object sender, RoutedEventArgs e)
        {
            ModelBuilderPanel.Visibility = Visibility.Visible;
            BuilderFlipView.SelectedIndex = 0;
        }

        private void ClearDiskButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ValidateLoginButton_Click(object sender, RoutedEventArgs e)
        {
            ValidMark.Visibility = Visibility.Collapsed;
            InvalidMark.Visibility = Visibility.Collapsed;
            LoginProgress.Visibility = Visibility.Visible;
            LoginNextButton.IsEnabled = false;

            if (UsernameEntry.Text == "" || ApiKeyEntry.Text == "")
            {
                InvalidMark.Visibility = Visibility.Visible;
                LoginProgress.Visibility = Visibility.Collapsed;
                return;
            }

            Thread t = new Thread(CheckLoginInfo);
            t.Start();
        }

        private async void CheckLoginInfo()
        {
            string username = "";
            string apikey = "";
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                username = UsernameEntry.Text;
                apikey = ApiKeyEntry.Text;
            });

            var check = await host.TryAuthenticate(username, apikey);

            if (check)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ValidMark.Visibility = Visibility.Visible;
                    LoginProgress.Visibility = Visibility.Collapsed;
                    LoginNextButton.IsEnabled = true;
                });
            }
            else
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InvalidMark.Visibility = Visibility.Visible;
                    LoginProgress.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void LoadDataNextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoadDataBackButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
