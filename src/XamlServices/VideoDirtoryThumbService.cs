using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Zhai.Famil.Common.Threads;
using System.Windows.Controls;
using System.Windows.Data;

namespace Zhai.VideoView.XamlServices
{
    internal class VideoDirtoryThumbService
    {
        public static readonly DependencyProperty VideoDirectoryProperty = DependencyProperty.RegisterAttached("VideoDirectory", typeof(DirectoryInfo), typeof(VideoDirtoryThumbService), new PropertyMetadata(new PropertyChangedCallback(OnImageSourcePropertyChangedCallback)));
        public static DirectoryInfo GetVideoDirectory(DependencyObject obj) => (DirectoryInfo)obj.GetValue(VideoDirectoryProperty);
        public static void SetVideoDirectory(DependencyObject obj, object value) => obj.SetValue(VideoDirectoryProperty, value);



        private static void OnImageSourcePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is Image image)
            {
                if (image.IsLoaded)
                {
                    return;
                }

                var directory = GetVideoDirectory(image);

                if (directory == null)
                {
                    image.Source = VideoThumbStateResources.ImageFailed;
                    return;
                }

                var filename = FindCover(directory.FullName, directory.Name);

                if (string.IsNullOrEmpty(filename))
                {
                    var file = directory.EnumerateFiles().Where(VideoSupport.VideoSupportExpression).FirstOrDefault();

                    if (file != null)
                        filename = file.FullName;
                }

                if (!string.IsNullOrEmpty(filename))
                {
                    var source = new VideoThumbBase(filename);

                    var binding = new Binding()
                    {
                        Source = source,
                        Path = new PropertyPath("ThumbSource")
                    };

                    image.SetBinding(Image.SourceProperty, binding);

                    source.DrawThumb();
                }
            }
        }

        private static string FindCover(string directory, string name)
        {
            string[] reservePaths = new string[] { $"{directory}\\cover", $"{directory}\\{name}" };

            foreach (var path in reservePaths)
            {
                foreach (var format in new string[] { "jpg", "jpeg", "png" })
                {
                    string cover = $"{path}.{format}";

                    if (File.Exists(cover))
                    {
                        return cover;
                    }
                }
            }

            return string.Empty;
        }
    }
}
