﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Zhai.VideoView
{
    internal enum VideoThumbState
    {
        Loading,
        Loaded,
        Failed
    }

    internal static class VideoThumbStateResources
    {
        public static BitmapImage ImageLoading = new BitmapImage(new Uri("pack://application:,,,/Resources/image-loading.png"));

        public static BitmapImage ImageFailed = new BitmapImage(new Uri("pack://application:,,,/Resources/image-failed.png"));
    }
}
