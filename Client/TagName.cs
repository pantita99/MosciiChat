using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Client
{
    public class TagName : IValueConverter

    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fullName)
            {
                var names = fullName.Split(' ');
                if (names.Length >= 2)
                {
                    // Return the first letter of the first and last name
                    return $"{names[0][0]}{names[1][0]}".ToUpper();
                }
                else if (names.Length == 1)
                {
                    // If only one name, return the first letter of that
                    return $"{names[0][0]}".ToUpper();
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

