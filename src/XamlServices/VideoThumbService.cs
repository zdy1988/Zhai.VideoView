using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Zhai.Famil.Common.Threads;

namespace Zhai.VideoView.XamlServices
{
    internal class VideoThumbService
    {
        private static readonly AsyncTaskQueue taskQueue = new();



        public static readonly DependencyProperty VideoProperty = DependencyProperty.RegisterAttached("Video", typeof(Video), typeof(VideoThumbService), new PropertyMetadata(new PropertyChangedCallback(OnVideoPropertyChangedCallback)));
        public static Video GetVideo(DependencyObject obj) => (Video)obj.GetValue(VideoProperty);
        public static void SetVideo(DependencyObject obj, object value) => obj.SetValue(VideoProperty, value);
        private static void OnVideoPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is Image image)
            {
                if (image.IsLoaded)
                {
                    return;
                }

                var video = GetVideo(image);

                if (video != null)
                {
                    var binding = new Binding()
                    {
                        Source = video,
                        Path = new PropertyPath("ThumbSource")
                    };

                    image.SetBinding(Image.SourceProperty, binding);

                    taskQueue.Run(() => video.DrawThumb());
                }
            }
        }


        public static readonly DependencyProperty VideoPathProperty = DependencyProperty.RegisterAttached("VideoPath", typeof(string), typeof(VideoThumbService), new PropertyMetadata(new PropertyChangedCallback(OnVideoPathPropertyChangedCallback)));
        public static string GetVideoPath(DependencyObject obj) => (string)obj.GetValue(VideoPathProperty);
        public static void SetVideoPath(DependencyObject obj, object value) => obj.SetValue(VideoPathProperty, value);
        private static void OnVideoPathPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is Image image)
            {
                if (image.IsLoaded)
                {
                    return;
                }

                var filename = GetVideoPath(image);

                if (!string.IsNullOrWhiteSpace(filename))
                {
                    var source = new VideoThumbBase(filename);

                    var binding = new Binding()
                    {
                        Source = source,
                        Path = new PropertyPath("ThumbSource")
                    };

                    image.SetBinding(Image.SourceProperty, binding);

                    taskQueue.Run(() => source.DrawThumb());
                }
            }
        }
    }
}
