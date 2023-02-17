using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Zhai.Famil.Common.Mvvm;

namespace Zhai.VideoView
{
    internal class Video : ViewModelBase
    {
        public string Name { get; }

        public long Size { get; }

        public String VideoPath { get; }

        private BitmapSource thumbSource = VideoThumbStateResources.ImageLoading;
        public BitmapSource ThumbSource
        {
            get => thumbSource;
            set => Set(() => ThumbSource, ref thumbSource, value);
        }

        private VideoThumbState thumbState = VideoThumbState.Failed;
        public VideoThumbState ThumbState
        {
            get => thumbState;
            set => Set(() => ThumbState, ref thumbState, value);
        }

        public Video(string filename)
        {
            var file = new FileInfo(filename);

            Name = file.Name;

            Size = file.Length;

            VideoPath = filename;
        }

        public void DrawThumb()
        {
            if (ThumbState != VideoThumbState.Failed)
                return;

            ThumbState = VideoThumbState.Loading;
            ThumbSource = VideoThumbStateResources.ImageLoading;

            if (!string.IsNullOrWhiteSpace(VideoPath))
            {
                try
                {
                    var thumbSource = GetWindowsThumbnail(VideoPath);

                    if (thumbSource != null)
                    {
                        ThumbSource = thumbSource;
                        ThumbState = VideoThumbState.Loaded;
                    }
                    else
                    {
                        ThumbSource = VideoThumbStateResources.ImageFailed;
                        ThumbState = VideoThumbState.Failed;
                    }
                }
                catch
                {
                    ThumbSource = VideoThumbStateResources.ImageFailed;
                    ThumbState = VideoThumbState.Failed;
                }
            }
        }

        private static BitmapSource GetWindowsThumbnail(string filename)
        {
            try
            {
                var thumb = Microsoft.WindowsAPICodePack.Shell.ShellFile.FromFilePath(filename).Thumbnail.BitmapSource;

                if (!thumb.IsFrozen)
                {
                    thumb.Freeze();
                }

                return thumb;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"{nameof(Video)} : GetWindowsThumbnail returned {filename} null  : {ex.Message}");
#endif
                return null;
            }
        }
    }
}
