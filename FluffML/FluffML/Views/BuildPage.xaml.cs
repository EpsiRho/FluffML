using e6API;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluffML
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BuildPage : Page
    {
        RequestHost host { get; set; }
        ObservableCollection<Post> PostsCollection { get; set; }

        public BuildPage()
        {
            this.InitializeComponent();
            host = new RequestHost("Fluff ML/v1.0 (by EpsilonRho)");
            PostsCollection = new ObservableCollection<Post>();
            if (System.IO.File.Exists("model.zip"))
            {
                ModelBuilderPanel.Visibility = Visibility.Collapsed;
            }
        }
        private async void CheckLoginInfo()
        {
            string username = "";
            string apikey = "";
            this.DispatcherQueue.TryEnqueue(() =>
            {
                username = UsernameEntry.Text;
                apikey = ApiKeyEntry.Text;
            });

            var check = await host.TryAuthenticate(username, apikey);

            if (check)
            {
                this.DispatcherQueue.TryEnqueue(() =>
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
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    InvalidMark.Visibility = Visibility.Visible;
                    LoginProgress.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async void ValidateLoginButton_Click(object sender, RoutedEventArgs e)
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

        private void OpenBuilderButton_Click(object sender, RoutedEventArgs e)
        {
            ModelBuilderPanel.Visibility = Visibility.Visible;
            BuilderFlipView.SelectedIndex = 0;
        }

        private void ClearDiskButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoginNextButton_Click(object sender, RoutedEventArgs e)
        {
            BuilderFlipView.SelectedIndex++;
        }

        private void LoadDataBackButton_Click(object sender, RoutedEventArgs e)
        {
            BuilderFlipView.SelectedIndex--;
        }

        private void LoadDataNextButton_Click(object sender, RoutedEventArgs e)
        {
            BuilderFlipView.SelectedIndex++;
        }

        private async void LoadPosts()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                LoadDataNextButton.IsEnabled = false;
                LoadDataBackButton.IsEnabled = false;
                DownloadPostsProgress.Visibility = Visibility.Visible;
                DownloadPostsProgress.Value = 0;
                DownloadPostsProgress.IsIndeterminate = true;
            });
            int count = 1;
            int pageCount = 1;

            for (int i = 0; i < 4; i++)
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
                    var check = DownloadFile(post, @"Downloads\Liked");
                    if (check)
                    {
                        count++;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            LikedDisplay.Text = $"Liked Posts: {count}";
                        });
                    }
                }
                pageCount++;
            }

            int newcount = 1;
            pageCount = 1;
            for (int i = 0; i < 6; i++)
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
                    var check = DownloadFile(post, @"Downloads\Disliked");
                    if (check)
                    {
                        newcount++;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            DislikedDisplay.Text = $"Disliked Posts: {newcount}";
                        });
                    }
                }
                pageCount++;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadDataNextButton.IsEnabled = true;
                LoadDataBackButton.IsEnabled = true;
                DownloadPostsProgress.Value = 1;
                DownloadPostsProgress.IsIndeterminate = false;
            });
        }

        bool DownloadFile(Post post, string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (System.IO.File.Exists($"{path}\\{post.file.md5}.{post.file.ext}"))
                {
                    return true;
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
                return true;
            }
            catch (Exception ex)
            {
                return false;
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

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(LoadPosts);
            t.Start();
        }

        private void ModelButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(CreateModel);
            t.Start();
        }

        private void CreateModel()
        {
            int epoch = 10;
            DispatcherQueue.TryEnqueue(() =>
            {
                CreateModelButton.IsEnabled = false;
                CreateModelBackButton.IsEnabled = false;
                CreateModelNextButton.IsEnabled = false;
                CreateModelProgress.Value = 0;
                CreateModelProgress.IsIndeterminate = true;
                CreateModelProgress.Visibility = Visibility.Visible;
                epoch = (int)EpochBox.Value;
            });
            MLHandler.CreateModel(epoch);
            DispatcherQueue.TryEnqueue(() =>
            {
                CreateModelButton.IsEnabled = true;
                CreateModelProgress.Value = 1;
                CreateModelBackButton.IsEnabled = true;
                CreateModelNextButton.IsEnabled = true;
                CreateModelProgress.IsIndeterminate = false;
            });
        }

        private async void GetPostsAndPredict()
        {
            var posts = await host.GetPosts("-animation", 50);

            if (!Directory.Exists("Downloads\\Predict"))
            {
                Directory.CreateDirectory("Downloads\\Predict");
            }

            foreach (Post post in posts)
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(post.sample.url, $"Downloads\\Predict\\{post.file.md5}.{post.file.ext}");
                }
            }

            //var files = Directory.GetFiles("Downloads\\Predict\\");
            foreach (var post in posts)
            {
                try
                {
                    var predict = MLHandler.ClassifySingleImage($"Downloads\\Predict\\{post.file.md5}.{post.file.ext}");

                    if (predict == "Liked")
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            PostsCollection.Add(post);
                        });
                    }
                }
                catch (Exception)
                {

                }
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                //ModelProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 244, 97));
                //ModelButton.IsEnabled = true;
                //GetPredictButton.IsEnabled = true;
                //ModelProgress.IsIndeterminate = false;
            });
        }

        private void CreateModelNextButton_Click(object sender, RoutedEventArgs e)
        {
            ModelBuilderPanel.Visibility = Visibility.Collapsed;
        }

        private void CreateModelBackButton_Click(object sender, RoutedEventArgs e)
        {
            BuilderFlipView.SelectedIndex--;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(GetPostsAndPredict);
            t.Start();
        }
    }
}
