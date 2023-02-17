using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Zhai.Famil.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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

            InitializeMainWindow();

            InitializeMediaControl();

            InitializeMouseMoveTimer();

            Loaded += MainWindow_Loaded;

            ViewModel.CurrentVideoChanged += ViewModel_CurrentVideoChanged;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private Storyboard HideControllerAnimation => FindResource("HideControlOpacity") as Storyboard;
        private Storyboard ShowControllerAnimation => FindResource("ShowControlOpacity") as Storyboard;

        private DateTime LastMouseMoveTime;
        private Point LastMousePosition;
        private DispatcherTimer MouseMoveTimer;
        private bool IsControllerHideCompleted;

        private void InitializeMainWindow()
        {
            var settings = Properties.Settings.Default;

            if (settings.IsStartWindowMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void InitializeMediaControl()
        {
            App.ViewModelLocator.VideoWindow.VideoOpened += (sender, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (App.ViewModelLocator.VideoWindow.MediaPlayer != null)
                    {
                        this.VideoViewer.MediaPlayer = App.ViewModelLocator.VideoWindow.MediaPlayer;
                    }
                });
            };

            LastMouseMoveTime = DateTime.UtcNow;

            VideoViewerContorl.MouseMove += (s, e) =>
            {
                var currentPosition = e.GetPosition(this.VideoViewer);
                if (Math.Abs(currentPosition.X - LastMousePosition.X) > double.Epsilon ||
                    Math.Abs(currentPosition.Y - LastMousePosition.Y) > double.Epsilon)
                    LastMouseMoveTime = DateTime.UtcNow;

                LastMousePosition = currentPosition;
            };

            VideoViewerContorl.MouseLeave += (s, e) =>
            {
                LastMouseMoveTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10));
            };

            VideoViewerContorl.AllowDrop = true;

            VideoViewerContorl.Drop += (s, e) =>
            {
                string fileName = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

                if (!string.IsNullOrWhiteSpace(fileName) && VideoSupport.IsSupported(fileName))
                {

                    ViewModel.OpenVideo(fileName);
                }
            };

            VideoViewerContorl.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Link;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            };


            ControllerPanelViewBox.MouseLeftButtonDown += (s, e) => e.Handled = true;
        }

        private void InitializeMouseMoveTimer()
        {
            Storyboard.SetTarget(HideControllerAnimation, ControllerPanel);
            Storyboard.SetTarget(ShowControllerAnimation, ControllerPanel);

            HideControllerAnimation.Completed += (es, ee) =>
            {
                ControllerPanel.Visibility = Visibility.Hidden;
                IsControllerHideCompleted = true;
            };

            ShowControllerAnimation.Completed += (es, ee) =>
            {
                IsControllerHideCompleted = false;
            };

            MouseMoveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150),
                IsEnabled = true
            };

            MouseMoveTimer.Tick += (s, e) =>
            {
                var elapsedSinceMouseMove = DateTime.UtcNow.Subtract(LastMouseMoveTime);
                if (elapsedSinceMouseMove.TotalMilliseconds >= 3000 && App.ViewModelLocator.VideoWindow.IsOpened && ControllerPanel.IsMouseOver == false)
                {
                    if (IsControllerHideCompleted) return;
                    Cursor = Cursors.None;
                    HideControllerAnimation?.Begin();
                    IsControllerHideCompleted = false;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                    ControllerPanel.Visibility = Visibility.Visible;
                    ShowControllerAnimation?.Begin();
                }
            };

            MouseMoveTimer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var arg = Application.Current.Properties["ArbitraryArgName"];

            if (arg != null)
            {
                await ViewModel.OpenVideo(arg.ToString());
            }
        }

        private void ViewModel_CurrentVideoChanged(object sender, Video video)
        {
            if (video != null)
            {
                ViewModel.TryOpenVideo(video.VideoPath);

                VideoList.ScrollIntoView(video);
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
                if (App.ViewModelLocator.VideoWindow.IsOpened)
                {
                    App.ViewModelLocator.VideoWindow.ToggledPlayVideo();
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
                        var window = new SettingsWindow
            {
                Owner = App.Current.MainWindow
            };

            window.ShowDialog();
        }
    }
}
