using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Zhai.VideoView
{

    public partial class VideoViewElement : UserControl
    {
        public VideoViewElement()
        {
            InitializeComponent();

            InitializeMediaControl();
        }

        private Storyboard HideControllerAnimation => FindResource("HideControlOpacity") as Storyboard;
        private Storyboard ShowControllerAnimation => FindResource("ShowControlOpacity") as Storyboard;

        private DateTime LastMouseMoveTime;
        private Point LastMousePosition;
        private DispatcherTimer MouseMoveTimer;
        private bool IsControllerHideCompleted;

        private void InitializeMediaControl()
        {
            App.VideoElementViewModel.MediaOpened += (sender, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (App.VideoElementViewModel.MediaPlayer != null)
                    {
                        this.VideoViewer.MediaPlayer = App.VideoElementViewModel.MediaPlayer;
                    }
                });
            };

            LastMouseMoveTime = DateTime.UtcNow;

            Loaded += VideoViewerElement_Loaded;

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

                    this.OpenMedia(fileName);
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

        private void VideoViewerElement_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= VideoViewerElement_Loaded;

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
                if (elapsedSinceMouseMove.TotalMilliseconds >= 3000 && App.VideoElementViewModel.IsOpened && ControllerPanel.IsMouseOver == false)
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


        #region Methods

        public void OpenMedia(string urlString) => App.VideoElementViewModel.TryOpenMedia(urlString);

        public void Play() => App.VideoElementViewModel?.TryPlayMedia();

        public void Pause() => App.VideoElementViewModel?.TryPausMedia();

        public void ToggledPlay() => App.VideoElementViewModel?.ToggledPlayMedia();

        public void Dispose() => ThreadPool.QueueUserWorkItem(_ => this.Dispatcher.Invoke(() => App.VideoElementViewModel?.DisposePlayer()));

        #endregion

        #region DependencyProperty

        public static readonly DependencyProperty VideoSourceProperty = DependencyProperty.Register(nameof(VideoSource), typeof(object), typeof(VideoViewElement), new PropertyMetadata(OnVideoSourceChanged));

        public object VideoSource
        {
            get => (object)GetValue(VideoSourceProperty);
            set => SetValue(VideoSourceProperty, value);
        }

        private static void OnVideoSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VideoViewElement videoViewer && e.NewValue is string localPath)
            {
                videoViewer.OpenMedia(localPath);
            }
        }

        #endregion
    }
}