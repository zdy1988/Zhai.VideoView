using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Zhai.Famil.Common.Mvvm;

namespace Zhai.VideoView
{
    internal class VideoThumbBase : ViewModelBase
    {
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

        private string _filename;

        public VideoThumbBase(string filename)
        {
            _filename = filename;
        }

        public void DrawThumb()
        {
            if (ThumbState != VideoThumbState.Failed)
                return;

            ThumbState = VideoThumbState.Loading;
            ThumbSource = VideoThumbStateResources.ImageLoading;

            if (!string.IsNullOrWhiteSpace(_filename))
            {
                try
                {
                    var thumbSource = GetWindowsThumbnail(_filename);

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

        private BitmapSource GetWindowsThumbnail(string filename)
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
