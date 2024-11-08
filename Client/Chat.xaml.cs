using System.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Client.Models;
using RadioButton = System.Windows.Controls.RadioButton; // Alias for RadioButton

namespace Client
{
    public partial class Chat : Window
    {
        private HubConnection _connection;
        private DispatcherTimer _statusRefreshTimer;
        private readonly string url = "https://localhost:7277/chatHub";
        public ObservableCollection<ChatGetUserModel> Messages { get; set; }
        public ObservableCollection<ChatGetUserModel> Users { get; set; }
        // เก็บประวัติการแชทของผู้ใช้แต่ละคน
        private Dictionary<string, ObservableCollection<ChatGetUserModel>> userChatHistories = new Dictionary<string, ObservableCollection<ChatGetUserModel>>();
        public ObservableCollection<string> SplitMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<ChatGetUserModel> UsersWithChatHistory { get; set; } = new ObservableCollection<ChatGetUserModel>();




        public Chat()
        {
            InitializeComponent();
            InitializeSignalR();

            StartUserStatusRefresh();

            Messages = new ObservableCollection<ChatGetUserModel>();
            Users = new ObservableCollection<ChatGetUserModel>();

            //messagesList.ItemsSource = Messages;
            GetUserList.ItemsSource = Users;
            // Set ItemsSource of ListBox specifically for users with chat history
            GetUserListWithChatHistory.ItemsSource = UsersWithChatHistory;


            UsersWithChatHistory = new ObservableCollection<ChatGetUserModel>();
            DataContext = this;
        }



        private void MessageTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;

            // คำนวณความสูงของ TextBox ตามจำนวนบรรทัดที่มี

            int lineCount = textBox.LineCount; // จำนวนบรรทัดที่มีอยู่
            
            IsPlaceholderVisible = string.IsNullOrEmpty(textBox.Text);  // ซ่อนหรือแสดง placeholder
            ScrollToBottom(); // เลื่อน scroll ไปที่ข้อความล่าสุด
        }



        private void StartUserStatusRefresh()
        {
            _statusRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _statusRefreshTimer.Tick += async (sender, e) => await LoadUsers();
            _statusRefreshTimer.Start();
        }



        private async void InitializeSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();

            // Receive and display new messages
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessageToUI(message);  // Ensure new messages are displayed immediately
                });
            });

            try
            {
                await _connection.StartAsync();
                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting: {ex.Message}");
            }
        }



        private void SaveFile(string fileName, byte[] fileBytes)
        {
            // กำหนด path สำหรับบันทึกไฟล์
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

            // บันทึกไฟล์ลงในระบบ
            File.WriteAllBytes(savePath, fileBytes);

            // แจ้งผู้ใช้ว่าไฟล์ถูกบันทึกเรียบร้อยแล้ว
            MessageBox.Show($"File {fileName} saved at {savePath}");
        }



        public async Task LoadUsers()
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected)
                {
                    await _connection.StartAsync();
                }

                // เรียกใช้ GetUserListAsync จาก HubConnection (ผู้ใช้ทั้งหมด)
                var users = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetUserListAsync");

                // เรียกใช้ GetUserListWithChatHistoryAsync (ผู้ใช้ที่มีประวัติแชท)
                var usersWithHistory = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetUserListWithChatHistoryAsync");

                // รวมผู้ใช้ทั้งหมดและผู้ใช้ที่มีประวัติแชท (หลีกเลี่ยงการซ้ำกัน)
                var allUsers = users.Concat(usersWithHistory).DistinctBy(user => user.UserID).ToList();

                // เคลียร์ข้อมูลใน ObservableCollection
                Users.Clear();

                // เพิ่มผู้ใช้ทั้งหมดใน Users
                foreach (var user in allUsers)
                {
                    Users.Add(user);
                }

                // ตั้งค่า ItemsSource ของ ListBox สำหรับผู้ใช้ทั้งหมด
                GetUserList.ItemsSource = Users;

                // ตั้งค่า ItemsSource ของ ListBox สำหรับผู้ใช้ที่มีประวัติแชท
                GetUserListWithChatHistory.ItemsSource = usersWithHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user list: {ex.Message}");
            }
        }



        private void GetUserListWithChatHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetUserListWithChatHistory.SelectedItem is ChatGetUserModel selectedUserWithHistory)
            {
                selectedUserId = selectedUserWithHistory.UserID;
                selectedUserFullname = selectedUserWithHistory.FullName;

                // เลือกแท็บ "Chats" อัตโนมัติเมื่อเลือกผู้ใช้ที่มีประวัติการสนทนา
                ChatsRadioButton.IsChecked = true;

                // แสดงพื้นที่สนทนาและเตรียมพร้อมสำหรับข้อความใหม่
                IsChatVisible = true;
                messageTextbox.Text = string.Empty;
                IsPlaceholderVisible = true;

                // โหลดประวัติการสนทนาของผู้ใช้ที่เลือก
                LoadChatHistory(selectedUserId);
            }
            else
            {
                selectedUserId = null;
                selectedUserFullname = null;
                Messages.Clear();

                // ซ่อนพื้นที่สนทนาเมื่อไม่ได้เลือกผู้ใช้
                IsChatVisible = false;
                IsPlaceholderVisible = false;
            }
        }




        private string selectedUserFullname;
        private string selectedUserId; // ใช้ FullName แทน UserID


       
        
        public string TextMessage { get; set; } // สำหรับการพิมพ์ข้อความ




        private bool _isChatVisible = false;
        public bool IsChatVisible
        {
            get { return _isChatVisible; }
            set { SetValue(IsChatVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsChatVisibleProperty =
            DependencyProperty.Register("IsChatVisible", typeof(bool), typeof(Chat), new PropertyMetadata(false));

        private bool _isPlaceholderVisible = true;  // ใช้ควบคุมการแสดง placeholder
        public bool IsPlaceholderVisible
        {
            get { return _isPlaceholderVisible; }
            set { SetValue(IsPlaceholderVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsPlaceholderVisibleProperty =
            DependencyProperty.Register("IsPlaceholderVisible", typeof(bool), typeof(Chat), new PropertyMetadata(true));





        private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetUserList.SelectedItem is ChatGetUserModel selectedUser)
            {
                selectedUserId = selectedUser.UserID;
                selectedUserFullname = selectedUser.FullName;

                // เปลี่ยนแท็บเป็น "Chats" อัตโนมัติเมื่อเลือกผู้ใช้
                ChatsRadioButton.IsChecked = true;

                // แสดงพื้นที่แชทและเตรียมพร้อมสำหรับการพิมพ์
                IsChatVisible = true;
                messageTextbox.Text = string.Empty;
                IsPlaceholderVisible = true;

                // โหลดประวัติการแชทของผู้ใช้ที่เลือก
                LoadChatHistory(selectedUserId);
            }
            else
            {
                selectedUserId = null;
                selectedUserFullname = null;
                Messages.Clear();

                // ซ่อนพื้นที่แชทเมื่อไม่มีการเลือกผู้ใช้
                IsChatVisible = false;
                IsPlaceholderVisible = false;
            }
        }

        // Chat.xaml.cs

        private string GetCurrentUserId()
        {
            // ดำเนินการดึง ID ของผู้ใช้ปัจจุบัน
            return "yourCurrentUserId"; // แทนที่ด้วยการดึง ID ผู้ใช้จริง
        }

        private async void LoadChatHistory(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                var currentUserId = GetCurrentUserId();  // ดึง ID ของผู้ใช้ปัจจุบัน

                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    MessageBox.Show("ไม่สามารถดึงข้อมูลผู้ใช้ปัจจุบันได้");
                    return;
                }

                // ดึงหรือสร้าง GUID สำหรับการสนทนาระหว่างผู้ใช้ปัจจุบันและผู้ใช้ที่เลือก
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", currentUserId, userId);

                if (string.IsNullOrWhiteSpace(chatGuid))
                {
                    MessageBox.Show("ไม่สามารถสร้างหรือดึง GUID สำหรับแชทได้");
                    return;    
                }

                // ดึงประวัติการสนทนาโดยใช้ GUID ที่ได้รับมา
                var chatHistory = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetChatHistoryByGuid", chatGuid);

                if (chatHistory == null || !chatHistory.Any())
                {
                    MessageBox.Show("ไม่มีประวัติการสนทนาสำหรับผู้ใช้นี้");
                }
                else
                {
                    // ล้างข้อความเก่าก่อนโหลดข้อความใหม่
                    messagesList.Items.Clear();

                    foreach (var message in chatHistory)
                    {
                        AddMessageToUI(message.Message); // แสดงข้อความแต่ละรายการใน UI
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดประวัติการสนทนา: {ex.Message}");
            }
        }

        private void AddMessageToUI(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
              
                return;
            }

            var textList = messageText.Split("#$");
            foreach (var addText in textList)
            {
                // สร้างข้อความและแสดงใน ListBox
                var messageControl = new Items.mymessage();
                messageControl.DataContext = new ChatGetUserModel { Message = addText };

                // เพิ่มข้อความใน ListBox
                messagesList.Items.Add(messageControl);
            }

            ScrollToBottom(); // เลื่อนแสดงข้อความล่าสุดที่ด้านล่าง
        }




        private void ScrollToBottom()
        {
            if (messagesList.Items.Count > 0)
            {
                // เลื่อนให้แสดงข้อความล่าสุด
                messagesScrollViewer.ScrollToEnd();
            }

        }


        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // ตรวจสอบว่าคลิกด้วยปุ่มซ้ายหรือไม่
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close(); // ใช้ null-conditional operator
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = (window.WindowState == WindowState.Normal)
                    ? WindowState.Maximized
                    : WindowState.Normal; // ขยายหรือคืนค่าขนาดหน้าต่าง
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var messageText = messageTextbox.Text;

                if (string.IsNullOrWhiteSpace(selectedUserId))
                {
                    MessageBox.Show("Please select a user to send a message.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(messageText))
                {
                    MessageBox.Show("Message cannot be empty");
                    return;
                }

                if (_connection.State != HubConnectionState.Connected)
                {
                    MessageBox.Show("Connection to the server is not established.");
                    return;
                }

                var currentUserId = "yourCurrentUserId";  // Replace with actual current user ID logic
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", currentUserId, selectedUserId);

                // Add the new message to the UI before sending to the server
                AddMessageToUI(messageText);

                // Send the message to the server to be saved
                await _connection.InvokeAsync("SaveChatHistory", chatGuid, currentUserId, selectedUserId, selectedUserFullname, messageText, null);

                messageTextbox.Text = string.Empty; // Clear the input box
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }











        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select a file to send",
                    Filter = "All Files|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string base64File = Convert.ToBase64String(fileBytes);

                    if (!string.IsNullOrEmpty(selectedUserId))
                    {
                        // ส่งไฟล์ผ่าน SignalR
                        await _connection.InvokeAsync("SendFile", selectedUserId, fileName, base64File);

                        // บันทึกการส่งไฟล์ลงในฐานข้อมูลผ่าน ChatHub
                        await _connection.InvokeAsync("SaveChatHistory", "yourCurrentUserId", selectedUserId, $"[Sent a file: {fileName}]", selectedUserFullname, fileName);
                    }
                    else
                    {
                        MessageBox.Show("Please select a user to send the file.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending file: {ex.Message}");
            }
        }



        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton radioButton)
            {
                if (ListNameContent == null || ChatsContent == null) return;

                if (radioButton == ListNameRadioButton)
                {
                    // แสดง ListNameContent และซ่อน ChatsContent
                    ListNameContent.Visibility = Visibility.Visible;
                    ChatsContent.Visibility = Visibility.Collapsed;

                    // ซ่อนพื้นที่แชทและล้างประวัติการแชท
                    IsChatVisible = false;
                    Messages.Clear();
                }
                else if (radioButton == ChatsRadioButton)
                {
                    // แสดง ChatsContent และซ่อน ListNameContent
                    ListNameContent.Visibility = Visibility.Collapsed;
                    ChatsContent.Visibility = Visibility.Visible;

                    // โหลดประวัติการแชทเมื่อมีผู้ใช้ที่ถูกเลือกอยู่
                    if (!string.IsNullOrEmpty(selectedUserId))
                    {
                        IsChatVisible = true;
                        LoadChatHistory(selectedUserId);
                    }
                    else
                    {
                        IsChatVisible = false;
                        Messages.Clear();
                    }
                }
            }
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // ส่วนนี้คุณสามารถเพิ่มโค้ดที่ต้องการทำงานเมื่อ Unchecked ได้
        }




    }



}