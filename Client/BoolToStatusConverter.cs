using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Client
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "Unknown"; // กรณีที่เป็น null แสดง "ไม่ทราบ" หรือ "Unknown"
            // ถ้า UserConnected เป็น true, แสดงสถานะ Online
            if (value is bool isConnected && isConnected)
                return "Online";
            else
                return "Offline"; // กรณีที่เป็น false หรือ null แสดง Offline
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}
