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
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDataButton.IsEnabled = false;
            ModelButton.IsEnabled = false;
            GetPredictButton.IsEnabled = false;
            LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            LoginProgress.Value = 1;
            LoginProgress.IsIndeterminate = true;

            if (UsernameEntry.Text == "")
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 75, 86));
                LoginProgress.Value = 1;
                LoginProgress.IsIndeterminate = false;
                return;
            }

            if (ApiEntry.Text == "")
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 75, 86));
                LoginProgress.Value = 1;
                LoginProgress.IsIndeterminate = false;
                return;
            }

            bool check = false;
            try
            {
                check = await host.TryAuthenticate(UsernameEntry.Text, ApiEntry.Text);
            }
            catch (Exception)
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 75, 86));
                LoginProgress.Value = 1;
                LoginProgress.IsIndeterminate = false;
                return;
            }
            if (check)
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 244, 97));
                LoginProgress.Value = 1;
                LoginProgress.IsIndeterminate = false;
                LoadDataButton.IsEnabled = true;
                host.Username = UsernameEntry.Text;
                host.ApiKey = ApiEntry.Text;
            }
            else
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 75, 86));
                LoginProgress.Value = 1;
                LoginProgress.IsIndeterminate = false;
            }
        }

        private async void LoadPosts()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                LoginProgress.Value = 1;
                LoadDataButton.IsEnabled = false;
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            });
            int count = 1;
            int pageCount = 1;

            List<string> Lines = new List<string>();
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
                    DownloadFile(post, @"Downloads\Liked");
                    count++;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LikedDisplay.Text = $"Liked Posts: {count}";
                    });
                    Lines.Add($"Liked\\{post.file.md5}.{post.file.ext}\tLiked");
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
                    DownloadFile(post, @"Downloads\Disliked");
                    newcount++;
                    DispatcherQueue.TryEnqueue(() =>
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
            for(int i = 0; i < 3; i++)
            {
                Test.Add(Lines[rand.Next(Lines.Count)]);
            }
            System.IO.File.WriteAllLines(@"Downloads\test-tags.tsv", Test);

            DispatcherQueue.TryEnqueue(() =>
            {
                LoginProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 244, 97));
                ModelButton.IsEnabled = true;
                LoadDataButton.IsEnabled = true;
                LoadDataProgress.IsIndeterminate = false;
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

        private void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            ModelButton.IsEnabled = false;
            GetPredictButton.IsEnabled = false;
            LoadDataProgress.IsIndeterminate = true;
            Thread t = new Thread(LoadPosts);
            t.Start();
        }

        private void ModelButton_Click(object sender, RoutedEventArgs e)
        {
            ModelProgress.IsIndeterminate = true;
            ModelButton.IsEnabled = false;
            Thread t = new Thread(CreateModel);
            t.Start();
        }

        private void CreateModel()
        {
            MLHandler.CreateModel();
            DispatcherQueue.TryEnqueue(() =>
            {
                ModelProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 244, 97));
                ModelButton.IsEnabled = true;
                GetPredictButton.IsEnabled = true;
                ModelProgress.IsIndeterminate = false;
            });
        }

        private void GetPredictButton_Click(object sender, RoutedEventArgs e)
        {
            GetPredictButton.IsEnabled = false;
            Thread t = new Thread(GetPostsAndPredict);
            t.Start();
        }

        private async void GetPostsAndPredict()
        {
            var posts = await host.GetPosts("", 300);

            foreach(Post post in posts)
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(post.sample.url, $"Downloads\\Predict\\{post.file.md5}.{post.file.ext}");
                }
            }

            foreach (Post post in posts)
            {
                var predict = MLHandler.MakePrediction($"Downloads\\Predict\\{post.file.md5}.{post.file.ext}");

                if (predict == "")
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        PostsCollection.Add(post);
                    });
                }
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                ModelProgress.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 244, 97));
                ModelButton.IsEnabled = true;
                GetPredictButton.IsEnabled = true;
                ModelProgress.IsIndeterminate = false;
            });
        }
    }
}
