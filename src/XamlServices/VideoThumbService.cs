using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Zhai.Famil.Common.Threads;

namespace Zhai.VideoView.XamlServices
{
    internal class VideoThumbService
    {
        public static readonly DependencyProperty VideoProperty = DependencyProperty.RegisterAttached("Video", typeof(Video), typeof(VideoThumbService), new PropertyMetadata(new PropertyChangedCallback(OnImageSourcePropertyChangedCallback)));
        public static Video GetVideo(DependencyObject obj) => (Video)obj.GetValue(VideoProperty);
        public static void SetVideo(DependencyObject obj, object value) => obj.SetValue(VideoProperty, value);


        private static readonly AsyncTaskQueue taskQueue = new();

        private static void OnImageSourcePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is Image image)
            {
                if (image.IsLoaded)
                {
                    return;
                }

                var video = GetVideo(image);

                taskQueue.Run(() => video.DrawThumb());
            }
        }
    }
}
