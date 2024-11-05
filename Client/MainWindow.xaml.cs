

using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            Chat chat = new Chat(); //เพื่อจัดการการแชท เช่น การแสดงข้อความหรือการส่งข้อความระหว่างผู้ใช้
            chat.Show(); //หน้าต่างที่แสดง: เมื่อผู้ใช้คลิกปุ่มแล้ว จะมีหน้าต่างใหม่ (แชท) เปิดขึ้นมาเพื่อให้ผู้ใช้สามารถเริ่มการสนทนาได้

        }
    }
}