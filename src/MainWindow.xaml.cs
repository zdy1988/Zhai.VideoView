using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Zhai.Famil.Controls;
using Zhai.PictureView;
using System.Linq;

namespace Zhai.VideoView
{
    /// <summary>
    /// VideoViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : GlassesWindow
    {
        VideoWindowViewModel ViewModel => this.DataContext as VideoWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            ViewModel.CurrentVideoChanged += ViewModel_CurrentVideoChanged;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var arg = Application.Current.Properties["ArbitraryArgName"];

            if (arg != null)
            {
                await OpenVideo(arg.ToString());
            }
        }

        public async Task OpenVideo(DirectoryInfo dir, string filename = null, List<DirectoryInfo> borthers = null)
        {
            var newFolder = new Folder(dir, borthers);

            if (newFolder.IsAccessed)
            {
                var oldFolder = ViewModel.Folder;

                ViewModel.Folder = newFolder;
                await ViewModel.Folder.LoadAsync();
                ViewModel.IsVideoCountMoreThanOne = ViewModel.Folder?.Count > 1;
                ViewModel.CurrentVideo = (filename == null ? ViewModel.Folder : ViewModel.Folder.Where(t => t.VideoPath == filename)).FirstOrDefault();

                ThreadPool.QueueUserWorkItem(_ => this.Dispatcher.Invoke(() => oldFolder?.Cleanup()));

                ViewModel.IsShowVideoListView = ViewModel.Folder != null && ViewModel.Folder.Count > 1;
            }
            else
            {
                var box = new Zhai.Famil.Dialogs.MessageBox(this, ($"软件对路径：“{dir.FullName}”没有访问权限！"));
                box.Show();
            }
        }

        public Task OpenVideo(string filename)
            => OpenVideo(Directory.GetParent(filename), filename);

        private void ViewModel_CurrentVideoChanged(object sender, Video video)
        {
            if (video != null)
            {
                this.VideoViewElement.OpenVideo(video.VideoPath);

                //PictureList.ScrollIntoView(picture);
            }
        }



        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.Close();
                }

                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
            }
            else if (e.Key == Key.Space)
            {
                // Toggle Play
                if (App.ViewModelLocator.VideoElement.IsOpened)
                {
                    App.ViewModelLocator.VideoElement.ToggledPlayVideo();
                }
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow
            {
                Owner = App.Current.MainWindow
            };

            window.ShowDialog();
        }
    }
}
