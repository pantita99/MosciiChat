using Newtonsoft.Json;

namespace Client.Models
{
    public class GetUser //Autehn
    {
        //[JsonProperty("userID")] // กำหนดให้ JSON ใช้ชื่อ userID
        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }
        public bool IsChat { get; set; }
        //public string Message { get; set; }
        //public string DisplayStatus => UserConnected.HasValue ? (UserConnected.Value ? "Online" : "Offline") : "Unknown";
    }
    public class GetUserHistory //chatHistory
    { 
        public Guid GUID { get; set; }
        public string IDSENDER { get; set; }
        public string FULLNAMESENDER { get; set; }
        public bool? IDRECIVER { get; set; }
        public string FULLNAMERECIVER { get; set; } = null!;
        public string MESSAGEHISTORY { get; set; } = null!;
        public string FILENAME { get; set; } = null!;
    }
}

