using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly StarCat_Context _context;

    public ChatHub(StarCat_Context context)
    {
        _context = context;
    }
    public async Task SendMessage(string senderFullName, string receiverFullName, string messageText)
    {
        try
        {
            if (string.IsNullOrEmpty(messageText))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Cannot send an empty message.");
                return;
            }
            var receiverUser = await _context.TB_AUTHENTICATION.FirstOrDefaultAsync(u => u.FullName == receiverFullName);
            if (receiverUser == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", $"User '{receiverFullName}' does not exist.");
                return;
            }

            var senderId = GetCurrentUserId();
            if (senderId == 0)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Sender ID is not available.");
                return;
            }

            var receiverId = receiverUser.UserID;
            if (string.IsNullOrEmpty(receiverId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Receiver User ID is not available");
                return;
            }
            var chatGuid = await GetOrCreateChatGuid(senderId.ToString(), receiverId);

            await SaveChatHistory(chatGuid, senderId.ToString(), receiverId, senderFullName, messageText);

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderFullName, messageText);
            await Clients.Caller.SendAsync("ReceiveMessage", senderFullName, messageText);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"Error sending message: {ex.Message}");
        }
    }
    private int GetCurrentUserId()
    {

        var userId = Context.User?.FindFirst("UserID")?.Value;
        return int.TryParse(userId, out var id) ? id : 0;
    }
    public async Task SendFile(string receiverId, string fileName, byte[] fileBytes)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        if (!int.TryParse(userId, out var senderId) || !int.TryParse(receiverId, out var receiverIdInt))
        {
            throw new ArgumentException("Invalid sender or receiver ID.");
        }
        var existingChat = await _context.TB_CHATHISTRY
            .FirstOrDefaultAsync(chat => (chat.IDSENDER == senderId.ToString() && chat.IDRECIVER == receiverId) ||
                                         (chat.IDSENDER == receiverId && chat.IDRECIVER == senderId.ToString()));

        var chatGuid = existingChat?.GUID ?? Guid.NewGuid().ToString();

        var chatEntry = new TB_CHATHISTRY
        {
            GUID = chatGuid,
            IDSENDER = senderId.ToString(),
            IDRECIVER = receiverIdInt.ToString(),
            MESSAGE = "[File Sent]",
            NAMEDRECIVER = Context.User.Identity.Name,
            FULLNAMESENDER = Context.User.Identity.Name,
            FILENAME = fileName
        };
        _context.TB_CHATHISTRY.Add(chatEntry);
        await _context.SaveChangesAsync();

        await Clients.User(receiverId).SendAsync("ReceiveFile", fileName, fileBytes);
    }
    //public async Task<List<GetUser>> GetChatHistory(string SenderId, string ReceiverId)
    //{
    //    try
    //    {
            
    //        var chatHistory = await _context.TB_CHATHISTRY
    //            .Where(chat =>
    //                (chat.IDSENDER == SenderId && chat.IDRECIVER == ReceiverId) ||
    //                (chat.IDSENDER == ReceiverId && chat.IDRECIVER == SenderId))
    //            .OrderBy(chat => chat.GUID) // หรือใช้ฟิลด์ที่ระบุเวลาส่งข้อความ
    //            .Select(chat => new GetUser
    //            {
    //                SenderId = chat.IDSENDER,
    //                ReceiverId = chat.IDRECIVER,
    //                Message = chat.MESSAGE,

    //                BackgroundColor = chat.COLOR
                  
    //            })
    //        .ToListAsync();

    //        return chatHistory;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error in GetCombinedChatHistory: {ex.Message}");
    //        throw new HubException("An unexpected error occurred while retrieving chat history.");
    //    }
        
    //}


    //public async Task<List<GetUser>> GetChatHistoryByGuid(string senderId, string receiverId)
    //{
    //    try
    //    {
    //        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
    //        {
    //            throw new ArgumentException("SenderId และ ReceiverId ไม่สามารถเป็นค่าว่างได้");
    //        }

    //        // ดึงประวัติแชทระหว่าง SenderId และ ReceiverId
    //        var chatHistory = await _context.TB_CHATHISTRY
    //            .Where(chat => (chat.IDSENDER == senderId && chat.IDRECIVER == receiverId) ||
    //                           (chat.IDSENDER == receiverId && chat.IDRECIVER == senderId))
    //            .ToListAsync();

    //        // แปลงข้อมูลเป็นรูปแบบที่ต้องการ
    //        return chatHistory.Select(chat => new GetUser
    //        {
    //            SenderId = chat.IDSENDER,
    //            ReceiverId = chat.IDRECIVER,
    //            UserID = chat.IDSENDER, // หรือ chat.IDRECIVER ตามความเหมาะสม
    //            FullName = chat.NAMEDRECIVER, // ปรับให้ตรงตามคอลัมน์จริงในตาราง
    //            Message = chat.MESSAGE,
    //            Filename = chat.FILENAME,
    //            BackgroundColor = chat.COLOR
    //        }).ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error in GetChatHistory: {ex.Message}");
    //        throw new HubException("An error occurred while retrieving chat history.");
    //    }
    //}



    public List<GetUser> GetUserList(string myUserID)
    {
        try
        {
            var users = _context.TB_AUTHENTICATION.Where(auth => auth.UserID != myUserID)
                .Select(auth => new GetUser
                {
                    UserID = auth.UserID.ToString(),
                    FullName = auth.FullName,
                    UserConnected = auth.UserConnected,
                })
                .ToList();
            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetUserList: {ex.Message}");
            throw new HubException("An error occurred while retrieving the user list.");
        }
    }
    public async Task<List<GetUser>> GetUserListWithChatHistory(string myUserID)
    {
        try
        {
            var usersWithHistory = await _context.TB_AUTHENTICATION.Where(auth => _context.TB_CHATHISTRY.Any(chat => chat.IDSENDER == myUserID))
                .Select(auth => new GetUser
                {
                    UserID = auth.UserID.ToString(),
                    FullName = auth.FullName,
                    UserConnected = auth.UserConnected
                })
                .ToListAsync();

            return usersWithHistory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetUserListWithChatHistory: {ex.Message}");
            throw new HubException("An error occurred while retrieving users with chat history.");
        }
    }

    public async Task SaveChatHistory(string senderId, string receiverId, string name, string message = null, string filename = null, string backgroundColor = null)
    {
        try
        {
            var existingChat = await _context.TB_CHATHISTRY
                .FirstOrDefaultAsync(x => x.IDSENDER == senderId || x.IDRECIVER == receiverId);

            if (existingChat != null)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    // เพิ่มข้อความใหม่ไปที่แชท
                    existingChat.MESSAGE += string.IsNullOrEmpty(existingChat.MESSAGE)
                        ? message
                        : "#$" + message; // เชื่อมข้อความใหม่
                }

                // อัปเดตไฟล์และสีพื้นหลัง
                existingChat.FILENAME = filename;
                existingChat.COLOR = backgroundColor;

                _context.TB_CHATHISTRY.Update(existingChat); // อัปเดตแชทที่มีอยู่
            }
            else
            {
                var newHistory = new TB_CHATHISTRY
                {
                    
                    GUID = new Guid().ToString(),
                    IDSENDER = senderId,
                    IDRECIVER = receiverId,
                    NAMEDRECIVER = name,
                    FULLNAMESENDER = "",
                    MESSAGE = message,
                    FILENAME = filename,
                    COLOR = backgroundColor
                };
                await _context.TB_CHATHISTRY.AddAsync(newHistory); // เพิ่มแชทใหม่
                //var chatWithSameIds = await _context.TB_CHATHISTRY
                //    .FirstOrDefaultAsync(chat => chat.IDSENDER == senderId && chat.IDRECIVER == receiverId);

                //if (chatWithSameIds != null)
                //{
                //    // ถ้ามีแชทที่ตรงกันระหว่าง senderId และ receiverId แต่ GUID ไม่ตรง
                //    if (!string.IsNullOrWhiteSpace(message))
                //    {
                //        chatWithSameIds.MESSAGE += string.IsNullOrEmpty(chatWithSameIds.MESSAGE)
                //            ? message
                //            : "#$" + message; // เพิ่มข้อความไปที่แชทนี้
                //    }

                //    chatWithSameIds.FILENAME = filename;
                //    chatWithSameIds.COLOR = backgroundColor;

                //    _context.TB_CHATHISTRY.Update(chatWithSameIds); // อัปเดตแชทที่มีอยู่
                //}
                //else
                //{
                //    // ถ้าไม่พบแชทที่ตรงกัน สร้างแชทใหม่
                //    var newChat = new TB_CHATHISTRY
                //    {
                //        GUID = guid,
                //        IDSENDER = senderId,
                //        IDRECIVER = receiverId,
                //        NAMEDRECIVER = name,
                //        FULLNAMESENDER = name,
                //        MESSAGE = string.IsNullOrWhiteSpace(message) ? null : message,
                //        FILENAME = filename,
                //        COLOR = backgroundColor
                //    };

                //    await _context.TB_CHATHISTRY.AddAsync(newChat); // เพิ่มแชทใหม่
                //}
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveChatHistory: {ex.Message}");
            throw new HubException("An unexpected error occurred while saving chat history.");
        }
    }

    public async Task<string> GetOrCreateChatGuid(string senderId, string receiverId)
    {
        string guid = string.Empty;
        try 
        { 
            var existingChat = await _context.TB_CHATHISTRY.FirstOrDefaultAsync(x => x.IDSENDER == senderId && x.IDRECIVER == receiverId);
            if (existingChat == null)
            {
                return guid = Guid.NewGuid().ToString();
            }
            else 
            {
                return existingChat.GUID;
            }
        }
        catch(Exception ex) 
        {
        
        }
        return guid;
    }




    public class GetUser
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }
       
    }
}