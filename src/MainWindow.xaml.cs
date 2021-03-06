using System;
using System.Windows;
using System.Windows.Input;

namespace Zhai.VideoView
{
    /// <summary>
    /// VideoViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : VideoWindow
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
                
                if(this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
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
