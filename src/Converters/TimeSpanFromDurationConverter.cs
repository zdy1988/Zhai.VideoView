using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Zhai.Famil.Converters;

namespace Zhai.VideoView.Converters
{
    internal class TimeSpanFromDurationConverter : ConverterMarkupExtensionBase<TimeSpanFromDurationConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long duration)
            {
                return ToDurationString(duration);
            }

            return null;
        }

        private string ToDurationString(long ticks)
        {
            if (ticks <= 0)
            {
                return TimeSpan.Zero.ToString(@"mm\:ss");
            }
            else if (ticks > 0 && ticks <= TimeSpan.TicksPerMinute * 60)
            {
                return new TimeSpan(ticks).ToString(@"mm\:ss");
            }
            else if (ticks > TimeSpan.TicksPerMinute * 60 && ticks <= TimeSpan.TicksPerMinute * 60 * 24)
            {
                return new TimeSpan(ticks).ToString(@"hh\:mm\:ss");
            }
            else if (ticks <= TimeSpan.TicksPerMinute * 60 * 24)
            {
                return new TimeSpan(ticks).ToString(@"dd\.hh\:mm\:ss");
            }

            return TimeSpan.Zero.ToString(@"mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
