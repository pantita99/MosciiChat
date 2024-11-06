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


        public Chat()
        {
            InitializeComponent();
            InitializeSignalR();

            StartUserStatusRefresh();

            Messages = new ObservableCollection<ChatGetUserModel>();
            Users = new ObservableCollection<ChatGetUserModel>();

            //messagesList.ItemsSource = Messages;
            GetUserList.ItemsSource = Users;


            DataContext = this;
        }



        private void MessageTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;

            // คำนวณความสูงของ TextBox ตามจำนวนบรรทัดที่มี

            int lineCount = textBox.LineCount; // จำนวนบรรทัดที่มีอยู่

            // ปรับความสูงของ TextBox

            // เลื่อน ScrollViewer ให้ไปที่ข้อความล่าสุด
            this.ScrollToBottom();
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


        // ฟังก์ชันเพื่อโหลดผู้ใช้และตั้งค่า ItemsSource ของ ListBox

        private async Task LoadUsers()
        {
            try
            {
                if (_connection.State != HubConnectionState.Connected)
                {
                    await _connection.StartAsync();
                }

                var users = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetUserList");
                Users.Clear();

                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user list: {ex.Message}");
            }
        }


        private string selectedUserFullname;
        private string selectedUserId; // ใช้ FullName แทน UserID

        private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetUserList.SelectedItem is ChatGetUserModel selectedUser)
            {
                selectedUserId = selectedUser.UserID;
                selectedUserFullname = selectedUser.FullName;

                LoadChatHistory(selectedUserId);
            }
            else
            {
                selectedUserId = null;
                selectedUserFullname = null;
                Messages.Clear();
            }
        }







        private async void LoadChatHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                var currentUserId = "yourCurrentUserId";  // Replace with actual current user ID logic
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", currentUserId, userId);

                var chatHistory = await _connection.InvokeAsync<List<ChatGetUserModel>>("GetChatHistoryByGuid", chatGuid);

                messagesList.Items.Clear(); // Clear existing messages

                if (chatHistory.Any())
                {
                    foreach (var message in chatHistory)
                    {
                        AddMessageToUI(message.Message); // New method to handle adding messages to UI
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chat history: {ex.Message}");
            }
        }

        private void AddMessageToUI(string messageText)
        {
            var textList = messageText.Split("#$");
            foreach (var addText in textList)
            {
                // Create a new instance of your custom control for each message
                var messageControl = new Items.mymessage();
                messageControl.DataContext = new ChatGetUserModel { Message = addText };

                // Add the control to the ListBox
                messagesList.Items.Add(messageControl);
            }
            ScrollToBottom(); // Ensure the UI scrolls to the latest message
        }










        private void ScrollToBottom()
        {
            if (messagesList.Items.Count > 0)
            {
                messagesList.ScrollIntoView(messagesList.Items[^1]);
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
            // ตรวจสอบว่ามีการเช็ค RadioButton ใด ๆ
            if (sender is System.Windows.Controls.RadioButton radioButton) // ใช้ System.Windows.Controls.RadioButton ที่นี่
            {
                if (ListNameContent != null && ChatsContent != null)
                {
                    if (radioButton.Content.ToString() == "List Name")
                    {
                        ListNameContent.Visibility = Visibility.Visible; // แสดง List Name
                        ChatsContent.Visibility = Visibility.Collapsed; // ซ่อน Chats
                    }
                    else if (radioButton.Content.ToString() == "Chats")
                    {
                        ListNameContent.Visibility = Visibility.Collapsed; // ซ่อน List Name
                        ChatsContent.Visibility = Visibility.Visible; // แสดง Chats
                    }
                }
            }
        }


        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // หากต้องการให้ทำงานเมื่อมีการ Unchecked สามารถเพิ่มโค้ดได้ที่นี่
        }



    }
}