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
        public ObservableCollection<ChatGetUserModel> usersWithoutHistory { get; set; } = new ObservableCollection<ChatGetUserModel>();
        public ObservableCollection<ChatGetUserModel> usersWithHistory { get; set; } = new ObservableCollection<ChatGetUserModel>();
        public ObservableCollection<ChatGetUserModel> UsersWithChatHistory { get; set; } = new ObservableCollection<ChatGetUserModel>();
        private readonly string myUserID = "Wha";
        public static readonly DependencyProperty IsPlaceholderVisibleProperty = DependencyProperty.Register("IsPlaceholderVisible", typeof(bool), typeof(Chat), new PropertyMetadata(true));
        private string selectedUserFullname;
        private string selectedUserId; // ใช้ FullName แทน UserID
        public string TextMessage { get; set; } // สำหรับการพิมพ์ข้อความ
        private bool _isChatVisible = false;
        public static readonly DependencyProperty IsChatVisibleProperty =
            DependencyProperty.Register("IsChatVisible", typeof(bool), typeof(Chat), new PropertyMetadata(false));

        private bool _isPlaceholderVisible = true;  // ใช้ควบคุมการแสดง placeholder
        public Chat()
        {
            InitializeComponent();
            InitializeSignalR();
            StartUserStatusRefresh();
            Messages = new ObservableCollection<ChatGetUserModel>();
            Users = new ObservableCollection<ChatGetUserModel>();
            GetUserList.ItemsSource = Users;
            GetUserListWithChatHistory.ItemsSource = UsersWithChatHistory;
            UsersWithChatHistory = new ObservableCollection<ChatGetUserModel>();
            DataContext = this;
          
        }
        private async void InitializeSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();
            try
            {
                await _connection.StartAsync();
                await LoadUsers();              // โหลดผู้ใช้ทั้งหมด
                await LoadUsersWithChatHistory(); // โหลดผู้ใช้ที่มีประวัติแชท
            }
            catch (Exception ex)
            {

            }
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
        // ฟังก์ชันดึงข้อมูลผู้ใช้ทั้งหมด
        public async Task LoadUsers()
        {
            try
            {
                // เรียกใช้ GetUserList จาก HubConnection (ผู้ใช้ทั้งหมด)
                var users = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetUserList");
                // เคลียร์ข้อมูลในคอลเลกชัน usersWithoutHistory
                usersWithoutHistory.Clear();
                // เพิ่มผู้ใช้ทั้งหมดลงใน usersWithoutHistory
                foreach (var user in users)
                {
                    usersWithoutHistory.Add(user);
                }
                // ตั้งค่า ItemsSource ของ ListBox สำหรับผู้ใช้ที่ไม่มีประวัติแชท
                GetUserList.ItemsSource = usersWithoutHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user list: {ex.Message}");
            }
        }
        // ฟังก์ชันดึงข้อมูลผู้ใช้ที่มีประวัติแชท
        public async Task LoadUsersWithChatHistory()
        {
            try
            {
                // ดึงรายชื่อผู้ใช้ที่มีประวัติแชท
                var usersWithChatHistory = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetUserListWithChatHistoryAsync");
                // เคลียร์ข้อมูลในคอลเลกชัน usersWithHistory
                usersWithHistory.Clear();
                // เพิ่มผู้ใช้ที่มีประวัติแชทลงใน usersWithHistory
                foreach (var user in usersWithChatHistory)
                {
                    usersWithHistory.Add(user);
                }
                // ตั้งค่า ItemsSource ของ ListBox สำหรับผู้ใช้ที่มีประวัติแชท
                GetUserListWithChatHistory.ItemsSource = usersWithHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users with chat history: {ex.Message}");
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
        public bool IsChatVisible
        {
            get { return _isChatVisible; }
            set { SetValue(IsChatVisibleProperty, value); }
        }
        public bool IsPlaceholderVisible
        {
            get { return _isPlaceholderVisible; }
            set { SetValue(IsPlaceholderVisibleProperty, value); }
        }
        private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure that both collections are initialized
            if (usersWithoutHistory == null || usersWithHistory == null)
            {
                MessageBox.Show("User lists are not initialized.");
                return;
            }

            if (GetUserList.SelectedItem is ChatGetUserModel selectedUser)
            {
                // ตั้งค่าผู้ใช้ที่เลือก
                selectedUserId = selectedUser.UserID;
                selectedUserFullname = selectedUser.FullName;

                // ตั้งค่าให้เลือกผู้ใช้เดียวกันใน ChatsContent
                if (usersWithHistory != null)
                {
                    var selectedChatUser = usersWithHistory.FirstOrDefault(user => user.UserID == selectedUserId);
                    if (selectedChatUser != null)
                    {
                        // เลือกผู้ใช้ใน ChatsContent
                        GetUserListWithChatHistory.SelectedItem = selectedChatUser;
                    }
                }

                // ซ่อน/แสดงพื้นที่แชท
                IsChatVisible = true;
                messageTextbox.Text = string.Empty;
                IsPlaceholderVisible = true;

                // แสดง ChatsContent
                ChatsRadioButton.IsChecked = true;

                // โหลดประวัติการแชทสำหรับผู้ใช้ที่เลือก
                LoadChatHistory(selectedUserId);
            }
            // ซ่อนพื้นที่แชทเมื่อไม่มีการเลือกผู้ใช้

        }
        private async void LoadChatHistory(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {

                if (string.IsNullOrWhiteSpace(myUserID))
                {
                    MessageBox.Show("ไม่สามารถดึงข้อมูลผู้ใช้ปัจจุบันได้");
                    return;
                }

                // ดึงหรือสร้าง GUID สำหรับการสนทนาระหว่างผู้ใช้ปัจจุบันและผู้ใช้ที่เลือก
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", myUserID, userId);

                if (string.IsNullOrWhiteSpace(chatGuid))
                {
                    MessageBox.Show("ไม่สามารถสร้างหรือดึง GUID สำหรับแชทได้");
                    return;
                }

                // ดึงประวัติการสนทนาโดยใช้ GUID ที่ได้มา
                var chatHistory = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetChatHistoryByGuid", chatGuid);

                // ล้างข้อความเก่าออกก่อนโหลดข้อความใหม่
                messagesList.Items.Clear();

                if (chatHistory == null || !chatHistory.Any())
                {
                    // ถ้าไม่มีประวัติการสนทนา ให้บันทึกข้อความว่างในฐานข้อมูลเพื่อสร้างบันทึกใหม่
                    await _connection.InvokeAsync("SaveChatHistory", chatGuid, myUserID, userId, selectedUserFullname, string.Empty, DateTime.Now);

                    // แสดงข้อความเริ่มต้นว่าไม่มีประวัติ
                    messagesList.Items.Clear();
                }
                else
                {
                    // แสดงประวัติการสนทนาที่ดึงมาใน UI
                    foreach (var message in chatHistory)
                    {
                        AddMessageToUI(message.Message);
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
                // Don't add anything if the message is empty
                return;
            }

            var textList = messageText.Split("#$");
            foreach (var addText in textList)
            {
                // Create message and display in ListBox
                var messageControl = new Items.mymessage();
                messageControl.DataContext = new ChatGetUserModel { Message = addText };

                // Add message to ListBox
                messagesList.Items.Add(messageControl);
            }

            ScrollToBottom(); // Scroll to show the latest message at the bottom
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
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", myUserID, selectedUserId);

                // Add the new message to the UI before sending to the server
                AddMessageToUI(messageText);

                // Send the message to the server to be saved
                await _connection.InvokeAsync("SaveChatHistory", chatGuid, myUserID, selectedUserId, selectedUserFullname, messageText, null);

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
            if (ListNameContent == null || ChatsContent == null) return; 

            if (sender == ListNameRadioButton)
            {
                // แสดง ListNameContent และซ่อน ChatsContent
                ListNameContent.Visibility = Visibility.Visible;
                ChatsContent.Visibility = Visibility.Collapsed;

                // ซ่อนพื้นที่แชทและล้างประวัติการแชท
                IsChatVisible = false;
                Messages.Clear();
                // ซ่อน ListBox ที่แสดงประวัติการแชท
                messagesList.Visibility = Visibility.Collapsed;
            }
            else if (sender == ChatsRadioButton)
            {
                // แสดง ChatsContent และซ่อน ListNameContent
                ListNameContent.Visibility = Visibility.Collapsed;
                ChatsContent.Visibility = Visibility.Visible;

                // แสดง ListBox ที่แสดงประวัติการแชท
                messagesList.Visibility = Visibility.Visible;

                // โหลดประวัติการแชทเมื่อมีผู้ใช้ที่ถูกเลือกอยู่
                if (!string.IsNullOrEmpty(selectedUserId))
                {
                    IsChatVisible = true;
                    LoadChatHistory(selectedUserId);
                }
                else
                {
                    IsChatVisible = false;
                    Messages.Clear();  // ล้างข้อความถ้าผู้ใช้ยังไม่ได้เลือก
                }

            }
        }
        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // ส่วนนี้คุณสามารถเพิ่มโค้ดที่ต้องการทำงานเมื่อ Unchecked ได้
        }
    }
}