﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Zhai.Famil.Controls;
using Zhai.Famil.Converters;

namespace Zhai.VideoView.Converters
{
    internal class VideoViewThemeConverter : ConverterMarkupExtensionBase<VideoViewThemeConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDarked)
                return isDarked ? WindowTheme.Dark : WindowTheme.Light;

            return WindowTheme.Dark;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is WindowTheme theme && theme == WindowTheme.Dark;
        }
    }
}
