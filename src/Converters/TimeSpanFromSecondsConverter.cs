using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using Zhai.Famil.Converters;

namespace Zhai.VideoView.Converters
{
    internal class TimeSpanFromSecondsConverter : ConverterMarkupExtensionBase<TimeSpanFromSecondsConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long seconds)
            {
                return TimeSpan.FromMilliseconds(seconds).ToString(@"hh\:mm\:ss");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
