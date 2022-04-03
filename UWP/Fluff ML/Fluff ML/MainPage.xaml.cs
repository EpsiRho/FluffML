using e6API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
                    host.Username = username;
                    host.ApiKey = apikey;
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

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(LoadPosts);
            t.Start();
        }

        private async void LoadPosts()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LoadDataNextButton.IsEnabled = false;
                LoadDataBackButton.IsEnabled = false;
                DownloadPostsProgress.Visibility = Visibility.Visible;
                DownloadPostsProgress.Value = 0;
                DownloadPostsProgress.IsIndeterminate = true;
            });
            int count = 1;
            int pageCount = 1;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            List<string> Lines = new List<string>();
            while(true)
            {
                // Get Tags from search
                string tags = $"votedup:{host.Username} -animated";

                // Get Posts
                var posts = await host.GetPosts(tags, 300, pageCount);
                if (posts == null)
                {
                    break;
                }

                if (posts.Count == 0)
                {
                    break;
                }

                foreach (var post in posts)
                {
                    DownloadFile(post, @"Downloads\Liked");
                    count++;
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LikedDisplay.Text = $"Liked Posts: {count}";
                    });
                    Lines.Add($"Liked\\{post.file.md5}.{post.file.ext}\tLiked");
                }
                pageCount++;
            }

            int newcount = 1;
            pageCount = 1;
            while (true)
            {
                // Get Tags from search
                string tags = $"voteddown:{host.Username} -animated";
                // Get Posts
                var posts = await host.GetPosts(tags, 300, pageCount);
                if (posts == null)
                {
                    break;
                }

                if (posts.Count == 0)
                {
                    break;
                }

                foreach (var post in posts)
                {
                    DownloadFile(post, @"Downloads\Disliked");
                    newcount++;
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DislikedDisplay.Text = $"Disliked Posts: {newcount}";
                    });
                    Lines.Add($"Disliked\\{post.file.md5}.{post.file.ext}\tDisliked");
                }
                pageCount++;
            }

            System.IO.File.WriteAllLines(@"Downloads\tags.tsv", Lines);

            Random rand = new Random();
            List<string> Test = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                Test.Add(Lines[rand.Next(Lines.Count)]);
            }
            System.IO.File.WriteAllLines(@"Downloads\test-tags.tsv", Test);

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LoadDataNextButton.IsEnabled = true;
                LoadDataBackButton.IsEnabled = true;
                DownloadPostsProgress.Value = 0;
                DownloadPostsProgress.IsIndeterminate = true;
            });
        }
        void DownloadFile(Post post, string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (System.IO.File.Exists($"{path}\\{post.file.md5}.{post.file.ext}"))
                {
                    return;
                }

                using (WebClient client = new WebClient())
                {
                    var data = client.DownloadData(new Uri(post.sample.url));

                    Bitmap bmp;
                    using (var ms = new MemoryStream(data))
                    {
                        bmp = new Bitmap(ms);
                    }

                    var square = MakeSquarePhoto(bmp, 500);
                    square.Save($"{path}\\{post.file.md5}.{post.file.ext}");
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        Bitmap MakeSquarePhoto(Bitmap bmp, int size)
        {
            try
            {
                Bitmap res = new Bitmap(size, size);
                Graphics g = Graphics.FromImage(res);
                g.FillRectangle(new SolidBrush(System.Drawing.Color.White), 0, 0, size, size);
                int t = 0, l = 0;
                if (bmp.Height > bmp.Width)
                    t = (bmp.Height - bmp.Width) / 2;
                else
                    l = (bmp.Width - bmp.Height) / 2;
                g.DrawImage(bmp, new Rectangle(0, 0, size, size), new Rectangle(l, t, bmp.Width - l * 2, bmp.Height - t * 2), GraphicsUnit.Pixel);
                return res;
            }
            catch { }
            return null;
        }
    }
}
