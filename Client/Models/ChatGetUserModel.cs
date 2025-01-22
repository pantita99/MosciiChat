using Newtonsoft.Json;

namespace Client.Models
{
    public class GetUser
    {

        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }
        public string Message { get; set; }
        public string Filename { get; set; }
        public string SenderId { get; set; }    // รหัสผู้ส่งข้อความ
        public string ReceiverId { get; set; }  // รหัสผู้รับข้อความ
        public string BackgroundColor { get; set; }

    }
    public class GetUserHistory
    {
        public Guid GUID { get; set; }
        public string IDSENDER { get; set; }
        public string FULLNAMESENDER { get; set; }
        public bool? IDRECIVER { get; set; }
        public string FULLNAMERECIVER { get; set; } = null!;
        public string MESSAGEHISTORY { get; set; } = null!;
        public string FILENAME { get; set; } = null!;
        public string message { get; set; }
        public string MessageBackgroundColor { get; set; } = "#e5e5e5"; // Default: สีข้อความฝั่งซ้าย
    }

}

