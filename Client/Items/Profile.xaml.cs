using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Items
{
    /// <summary>
    /// Interaction logic for Profile.xaml
    /// </summary>
    public partial class Profile : UserControl
    {
        public Profile()
        {
            InitializeComponent();
            SetRandomBackgroundColor();
        }

        public static readonly DependencyProperty FullNameProperty = DependencyProperty.Register("FullName",typeof(string), typeof(Profile), new PropertyMetadata(null));

        public string FullName
        {
            get { return (string)GetValue(FullNameProperty); }
            set { SetValue(FullNameProperty, value); }
        }

        private void SetRandomBackgroundColor()
        {
            // สุ่มสีโทน Pastel โดยจำกัดค่า RGB ให้มีค่าในช่วงที่สูง (150-255)
            Random random = new Random();
            byte r = (byte)random.Next(150, 256); // ค่า RGB สีแดง
            byte g = (byte)random.Next(150, 256); // ค่า RGB สีเขียว
            byte b = (byte)random.Next(150, 256); // ค่า RGB สีน้ำเงิน

            // สร้าง SolidColorBrush จากสีที่สุ่มได้
            SolidColorBrush pastelColorBrush = new SolidColorBrush(Color.FromRgb(r, g, b));

            // ตั้งค่า Background ของ Border เป็นสีโทน Pastel ที่สุ่มได้
            //InitialsCircle.Background = pastelColorBrush;

            //// ตั้งค่า Foreground (ตัวอักษร) เป็นสีขาว
            //InitialsCircleText.Foreground = Brushes.White;
        }
    }
}
