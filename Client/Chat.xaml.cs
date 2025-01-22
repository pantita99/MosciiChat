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
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR;

namespace Client
{
    public partial class Chat : Window
    {
        private readonly string myUserID = "adgem";
        private HubConnection _connection;
        private DispatcherTimer _statusRefreshTimer;
        private readonly string url = "http://localhost:5050/chatHub";
        //private readonly string url = "http://192.168.3.91:5050/chatHub";
        public ObservableCollection<GetUserHistory> Messages { get; set; }
        public ObservableCollection<GetUserHistory> Users { get; set; }
        // เก็บประวัติการแชทของผู้ใช้แต่ละคน
        private Dictionary<string, ObservableCollection<GetUser>> userChatHistories = new Dictionary<string, ObservableCollection<GetUser>>();
        public ObservableCollection<string> SplitMessages { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<GetUser> usersWithoutHistory { get; set; } = new ObservableCollection<GetUser>();
        public ObservableCollection<GetUser> usersWithHistory { get; set; } = new ObservableCollection<GetUser>();
        public ObservableCollection<GetUser> UsersWithChatHistory { get; set; } = new ObservableCollection<GetUser>();
        public static readonly DependencyProperty IsPlaceholderVisibleProperty = DependencyProperty.Register("IsPlaceholderVisible", typeof(bool), typeof(Chat), new PropertyMetadata(true));
        private string selectedUserFullname;
        private string selectedUserId;
        public string TextMessage { get; set; }
        private bool _isChatVisible = false;
        public static readonly DependencyProperty IsChatVisibleProperty =
            DependencyProperty.Register("IsChatVisible", typeof(bool), typeof(Chat), new PropertyMetadata(false));

        private bool _isPlaceholderVisible = true;
        public Chat()
        {
            InitializeComponent();
            InitializeSignalR();
            StartUserStatusRefresh();
            Messages = new ObservableCollection<GetUserHistory>();
            Users = new ObservableCollection<GetUserHistory>();
            GetUserList.ItemsSource = Users;
            GetUserListWithChatHistory.ItemsSource = UsersWithChatHistory;
            UsersWithChatHistory = new ObservableCollection<GetUser>();
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
                await LoadUsers();
                await LoadUsersWithChatHistory();
            }
            catch (Exception ex)
            {

            }
        }
        private void MessageTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;


            int lineCount = textBox.LineCount;

            IsPlaceholderVisible = string.IsNullOrEmpty(textBox.Text);
            ScrollToBottom();
        }
        private void StartUserStatusRefresh()
        {
            //_statusRefreshTimer = new DispatcherTimer
            //{
            //    Interval = TimeSpan.FromMinutes(1)
            //};
            //_statusRefreshTimer.Tick += async (sender, e) => await LoadUsers();
            //_statusRefreshTimer.Start();
        }

        public async Task LoadUsers()
        {
            try
            {

                var users = await _connection.InvokeAsync<List<GetUser>>("GetUserList", myUserID);

                usersWithoutHistory.Clear();

                foreach (var user in users)
                {
                    usersWithoutHistory.Add(user);
                }

                GetUserList.ItemsSource = usersWithoutHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user list: {ex.Message}");
            }
        }

        public async Task LoadUsersWithChatHistory()
        {
            try
            {

                var usersWithChatHistory = await _connection.InvokeAsync<List<GetUser>>("GetUserListWithChatHistory");

                usersWithHistory.Clear();

                foreach (var user in usersWithChatHistory)
                {
                    usersWithHistory.Add(user);
                }


                GetUserListWithChatHistory.ItemsSource = usersWithHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users with chat history: {ex.Message}");
            }
        }

        private void SaveFile(string fileName, byte[] fileBytes)
        {

            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);


            File.WriteAllBytes(savePath, fileBytes);


            MessageBox.Show($"File {fileName} saved at {savePath}");
        }
        private void GetUserListWithChatHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetUserListWithChatHistory.SelectedItem is GetUser selectedUserWithHistory)
            {
                selectedUserId = selectedUserWithHistory.UserID;
                selectedUserFullname = selectedUserWithHistory.FullName;


                ChatsRadioButton.IsChecked = true;


                IsChatVisible = true;
                messageTextbox.Text = string.Empty;
                IsPlaceholderVisible = true;


                LoadChatHistory(selectedUserId);
            }
            else
            {
                selectedUserId = null;
                selectedUserFullname = null;
                Messages.Clear();


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

            if (usersWithoutHistory == null || usersWithHistory == null)
            {
                MessageBox.Show("User lists are not initialized.");
                return;
            }

            if (GetUserList.SelectedItem is GetUser selectedUser)
            {

                selectedUserId = selectedUser.UserID;
                selectedUserFullname = selectedUser.FullName;

                if (usersWithHistory != null)
                {
                    var selectedChatUser = usersWithHistory.FirstOrDefault(user => user.UserID == selectedUserId);
                    if (selectedChatUser != null)
                    {
                        GetUserListWithChatHistory.SelectedItem = selectedChatUser;
                    }
                }

                IsChatVisible = true;
                messageTextbox.Text = string.Empty;
                IsPlaceholderVisible = true;
                ChatsRadioButton.IsChecked = true;
                LoadChatHistory(selectedUserId);
            }


        }
        private async void LoadChatHistory(string selectedUserId)
        {
            if (string.IsNullOrWhiteSpace(selectedUserId)) return;

            try
            {
                var chatGuid = await _connection.InvokeAsync<string>("GetOrCreateChatGuid", myUserID, selectedUserId);

                if (string.IsNullOrWhiteSpace(chatGuid))
                {
                    MessageBox.Show("ไม่สามารถสร้างหรือดึง GUID สำหรับแชทได้");
                    return;
                }

                var chatHistory = await _connection.InvokeAsync<List<GetUser>>("GetChatHistoryByGuid", chatGuid);

                messagesList.Items.Clear();

                if (chatHistory == null || !chatHistory.Any()) return;

                foreach (var message in chatHistory)
                {
                    if (!string.IsNullOrWhiteSpace(message?.Message))
                    {
                        var messageParts = message.Message.Split('$')
                            .Select(part => part.TrimEnd('#')) // ลบ # ออกจากตอนท้าย
                            .Where(part => !string.IsNullOrWhiteSpace(part)) // ลบส่วนที่ว่างเปล่า
                            .ToList();

                        var isOwnMessage = message.SenderId == myUserID;

                        foreach (var part in messageParts)
                        {
                            var messageControl = new Items.mymessage
                            {
                                DataContext = new GetUserHistory
                                {
                                    message = part // ข้อความแต่ละส่วน
                                },
                                HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                                    isOwnMessage ? "#cddeff" : "#e5e5e5" // สีพื้นหลังตามผู้ส่ง
                                ))
                            };

                            messagesList.Items.Add(messageControl);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาดในการโหลดประวัติการสนทนา: {ex.Message}");
            }
        }


        private void AddMessageToUI(string messageText, string senderUserID)
        {
            if (string.IsNullOrEmpty(messageText)) return;

            // ตรวจสอบว่าเป็นข้อความของผู้ใช้เองหรือไม่
            var isOwnMessage = senderUserID == myUserID;

            // สร้างอินสแตนซ์ของ Items.mymessage
            var messageControl = new Items.mymessage
            {
                DataContext = new GetUserHistory
                {
                    message = messageText.Trim()
                },
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isOwnMessage ? "#cddeff" : "#e5e5e5" // สีพื้นหลังตามฝั่งข้อความ
                )),
                HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left 
            };

            // เพิ่มข้อความลงใน ListBox
            messagesList.Items.Add(messageControl);

            // เลื่อน Scroll ไปยังข้อความล่าสุด
            ScrollToBottom();
        }



        private void ScrollToBottom()
        {
            if (messagesList.Items.Count > 0)
            {
                messagesScrollViewer.ScrollToEnd();
            }

        }
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
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
                    : WindowState.Normal;
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


                AddMessageToUI(messageText, myUserID);

                // บันทึกประวัติการสนทนา
                await _connection.InvokeAsync("SaveChatHistory", chatGuid, myUserID, "001", "test 001", messageText, null, null);

                messageTextbox.Text = string.Empty;
            }
            catch (HubException ex)
            {
                // แสดงข้อความข้อผิดพลาดที่ได้รับจาก Server
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
            catch (Exception ex)
            {
                // จัดการข้อผิดพลาดทั่วไป
                MessageBox.Show($"Unexpected error: {ex.Message}");
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

                        await _connection.InvokeAsync("SendFile", selectedUserId, fileName, base64File);


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

                ListNameContent.Visibility = Visibility.Visible;
                ChatsContent.Visibility = Visibility.Collapsed;


                IsChatVisible = false;
                Messages.Clear();

                messagesList.Visibility = Visibility.Collapsed;
            }
            else if (sender == ChatsRadioButton)
            {

                ListNameContent.Visibility = Visibility.Collapsed;
                ChatsContent.Visibility = Visibility.Visible;


                messagesList.Visibility = Visibility.Visible;


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
        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}