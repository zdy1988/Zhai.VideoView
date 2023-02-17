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
    internal class VideoViewTitleConverter : ConverterMarkupExtensionBase<VideoViewTitleConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string title = $" {Properties.Settings.Default.AppName}";

            if (value is Video video)
                return $" {video.Name} -{title}";

            return title;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
