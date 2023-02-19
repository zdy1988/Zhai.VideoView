using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Zhai.VideoView
{
    /// <summary>
    /// ControllerPanelControl.xaml 的交互逻辑
    /// </summary>
    public partial class VideoWindowControllerPanel : UserControl
    {
        VideoWindowViewModel ViewModel => this.DataContext as VideoWindowViewModel;

        public VideoWindowControllerPanel()
        {
            InitializeComponent();

            this.PositionSlider.AddHandler(Slider.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PositionSlider_PreviewMouseLeftButtonDown), true);
        }

        private void PositionSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            ViewModel.IsPositionChanging = true;
        }

        private void PositionSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ViewModel.SetPosition((float)this.PositionSlider.Value);

            ViewModel.IsPositionChanging = false;
        }

        private void PositionSlider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // ViewModel.SetPosition((float)this.PositionSlider.Value);
        }

        private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.SetPosition((float)this.PositionSlider.Value);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
