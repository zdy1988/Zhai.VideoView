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
    public class GreaterOrEqualsToBoolConverter : ConverterMarkupExtensionBase<GreaterOrEqualsToBoolConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && double.TryParse(parameter.ToString(), out double than))
            {
                if (value is double d)
                {
                    return d >= than;
                }
                else if (value is float f)
                {
                    return f >= than;
                }
                else if (value is ulong ul)
                {
                    return ul >= than;
                }
                else if (value is long l)
                {
                    return l >= than;
                }
                else if (value is uint ui)
                {
                    return ui >= than;
                }
                else if (value is int i)
                {
                    return i >= than;
                }
                else if (value is ushort us)
                {
                    return us >= than;
                }
                else if (value is short s)
                {
                    return s >= than;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}