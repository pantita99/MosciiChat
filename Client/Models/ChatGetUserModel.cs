using Newtonsoft.Json;

namespace Client.Models
{
    //public class GetUser
    //{

    //    public string UserID { get; set; }
    //    public string FullName { get; set; }
    //    public bool? UserConnected { get; set; }
    //    public string Message { get; set; }
    //    public string Filename { get; set; }
    //    public string SenderId { get; set; }    // รหัสผู้ส่งข้อความ
    //    public string ReceiverId { get; set; }  // รหัสผู้รับข้อความ
    //    public string BackgroundColor { get; set; }

    //}
    public class GetUser
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }

    }
    public class GetChatHistory
    {
        public string GUID { get; set; }

        public string IDSENDER { get; set; }

        public string FULLNAMESENDER { get; set; }

        public string IDRECIVER { get; set; }

        public string NAMEDRECIVER { get; set; }

        public string MESSAGE { get; set; }

        public string FILENAME { get; set; }

        public string COLOR { get; set; }
    }

}

