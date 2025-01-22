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
            FULLNAMESENDER = Context.User.Identity.Name,
            FILENAME = fileName
        };
        _context.TB_CHATHISTRY.Add(chatEntry);
        await _context.SaveChangesAsync();

        await Clients.User(receiverId).SendAsync("ReceiveFile", fileName, fileBytes);
    }
    public async Task<List<GetUser>> GetChatHistory(string userId1, string userId2)
    {
        try
        {
            if (string.IsNullOrEmpty(userId1) || string.IsNullOrEmpty(userId2))
            {
                throw new ArgumentException("User IDs cannot be null or empty.");
            }

            // ดึงข้อมูลแชทที่เกี่ยวข้องกับผู้ใช้สองคน
            var chatHistory = await _context.TB_CHATHISTRY
                .Where(chat => (chat.IDSENDER == userId1 && chat.IDRECIVER == userId2) ||
                               (chat.IDSENDER == userId2 && chat.IDRECIVER == userId1))
                .OrderBy(chat => chat.GUID) // เรียงตาม GUID หรือเวลาส่ง
                .ToListAsync();

            // แปลงข้อมูลจาก TB_CHATHISTRY เป็น GetUser
            var chatMessages = chatHistory.Select(chat => new GetUser
            {
                UserID = chat.IDSENDER,
                FullName = chat.FULLNAMESENDER,
                Message = chat.MESSAGE,
                Filename = chat.FILENAME,
                BackgroundColor = chat.COLOR,
                IsSender = chat.IDSENDER == userId1 // ตรวจสอบว่าเป็นข้อความที่ผู้ใช้ส่งออกไปหรือไม่
            }).ToList();

            return chatMessages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetChatHistory: {ex.Message}");
            return new List<GetUser>();
        }
    }


    public async Task<List<GetUser>> GetChatHistoryByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid))
        {
            throw new HubException("The GUID cannot be null or empty.");
        }

        try
        {
            var chatHistory = await _context.TB_CHATHISTRY
                .Where(chat => chat.GUID == guid)
                .OrderBy(chat => chat.IDSENDER)
                .ToListAsync();

            return chatHistory.Select(chat => new GetUser
            {
                SenderId = chat.IDSENDER,
                ReceiverId = chat.IDRECIVER,
                UserID = chat.IDSENDER,
                FullName = chat.FULLNAMESENDER,
                Message = chat.MESSAGE,
                Filename = chat.FILENAME,
                BackgroundColor = chat.COLOR // ส่งคืนสีพื้นหลัง
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetChatHistoryByGuid: {ex.Message}");
            throw new HubException("An error occurred while retrieving chat history by GUID.");
        }
    }


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
    public async Task<List<GetUser>> GetUserListWithChatHistory()
    {
        try
        {
            var usersWithHistory = await _context.TB_AUTHENTICATION
                .Where(auth => _context.TB_CHATHISTRY
                    .Any(chat => chat.IDSENDER == auth.UserID.ToString() || chat.IDRECIVER == auth.UserID.ToString()))
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

    public async Task SaveChatHistory(string guid, string senderId, string receiverId, string name, string message, string filename = null, string backgroundColor = null)
    {
        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
        {
            throw new ArgumentException("Invalid arguments. GUID, senderId, and receiverId cannot be null or empty.");
        }

        try
        {
            // ตรวจสอบแชทที่มีอยู่ระหว่าง senderId, receiverId, และ GUID
            var existingChat = await _context.TB_CHATHISTRY
                .FirstOrDefaultAsync(chat => chat.GUID == guid && chat.IDSENDER == senderId && chat.IDRECIVER == receiverId);

            if (existingChat != null)
            {
                // หากพบแชทที่ตรงกันแล้ว
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
                // ถ้าไม่พบแชทที่ตรงกัน ให้ตรวจสอบการแชทระหว่าง senderId และ receiverId
                var chatWithSameIds = await _context.TB_CHATHISTRY
                    .FirstOrDefaultAsync(chat => chat.IDSENDER == senderId && chat.IDRECIVER == receiverId);

                if (chatWithSameIds != null)
                {
                    // ถ้ามีแชทที่ตรงกันระหว่าง senderId และ receiverId แต่ GUID ไม่ตรง
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        chatWithSameIds.MESSAGE += string.IsNullOrEmpty(chatWithSameIds.MESSAGE)
                            ? message
                            : "#$" + message; // เพิ่มข้อความไปที่แชทนี้
                    }

                    chatWithSameIds.FILENAME = filename;
                    chatWithSameIds.COLOR = backgroundColor;

                    _context.TB_CHATHISTRY.Update(chatWithSameIds); // อัปเดตแชทที่มีอยู่
                }
                else
                {
                    // ถ้าไม่พบแชทที่ตรงกัน สร้างแชทใหม่
                    var newChat = new TB_CHATHISTRY
                    {
                        GUID = guid,
                        IDSENDER = senderId,
                        IDRECIVER = receiverId,
                        FULLNAMESENDER = name,
                        MESSAGE = string.IsNullOrWhiteSpace(message) ? null : message,
                        FILENAME = filename,
                        COLOR = backgroundColor
                    };

                    await _context.TB_CHATHISTRY.AddAsync(newChat); // เพิ่มแชทใหม่
                }
            }

            // บันทึกการเปลี่ยนแปลงทั้งหมดในฐานข้อมูล
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"Database update error: {dbEx.Message}");
            throw new HubException("An error occurred while saving chat history. Please try again.");
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
        public string Message { get; set; }
        public string Filename { get; set; }
        public string SenderId { get; set; }    // รหัสผู้ส่งข้อความ
        public string ReceiverId { get; set; }  // รหัสผู้รับข้อความ
        public string BackgroundColor { get; set; }
        public bool IsSender { get; set; }
    }
}