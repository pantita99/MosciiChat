using Client.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Client.Models
{
    public class ChatGetUserModel
    {
        [JsonProperty("userID")] // กำหนดให้ JSON ใช้ชื่อ userID
        public string UserID { get; set; }


        public string FullName { get; set; }

        
        public string GUID { get; set; }
        public bool? UserConnected { get; set; }

        public string ID { get; set; } = null!;

        public string SenderId { get; set; } = null!;

        public string Message { get; set; }
        public DateTime Timestamp { get; set; }



        public string DisplayStatus => UserConnected.HasValue
       ? (UserConnected.Value ? "Online" : "Offline")
       : "Unknown";




    }
}

