using System;
using System.Windows;
using System.Windows.Input;
using Zhai.Famil.Controls;

namespace Zhai.VideoView
{
    /// <summary>
    /// VideoViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : GlassesWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        public MainWindow(string mrl)
            :this()
        {
            this.VideoViewElement.OpenMedia(mrl);
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
                if (App.VideoElementViewModel.IsOpened)
                {
                    App.VideoElementViewModel.ToggledPlayMedia();
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
