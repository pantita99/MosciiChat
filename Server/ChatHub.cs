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
    public async Task<List<TB_CHATHISTRY>> GetChatHistory(string SenderId, string ReceiverId)
    {
        try
        {
            var chatHistory = await _context.TB_CHATHISTRY
                .Where(x => x.IDSENDER == SenderId && x.IDRECIVER == ReceiverId)
                .ToListAsync();
            return chatHistory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetChatHistory: {ex.Message}");
            return new List<TB_CHATHISTRY>(); 
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
    public async Task<List<GetUser>> GetUserListWithChatHistory(string myUserID)
    {
        try
        {
            var usersWithHistory = await _context.TB_AUTHENTICATION
    .Join(
        _context.TB_CHATHISTRY,
        auth => auth.UserID,
        chat => chat.IDRECIVER,
        (auth, chat) => new { auth, chat }
    )
    .Where(x => x.chat.IDSENDER == myUserID)
    .Select(x => new GetUser
    {
        UserID = x.auth.UserID.ToString(),
        FullName = x.auth.FullName,
        UserConnected = x.auth.UserConnected
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

    public async Task SaveChatHistory(string senderId, string receiverId, string message = null, string filename = null, string backgroundColor = null)
    {
        try
        {
            var existingChat = await _context.TB_CHATHISTRY
                .FirstOrDefaultAsync(x => x.IDSENDER == senderId && x.IDRECIVER == receiverId);

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
                var newChat = new TB_CHATHISTRY
                {
                    GUID = Guid.NewGuid().ToString(),
                    IDSENDER = senderId,
                    IDRECIVER = receiverId,
                    MESSAGE = message,
                    NAMEDRECIVER = "",
                    FULLNAMESENDER = "",
                    FILENAME = filename
                };
                _context.TB_CHATHISTRY.Add(newChat);
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveChatHistory: {ex.Message}");
            throw new HubException("An unexpected error occurred while saving chat history.");
        }
    }
    public class GetUser
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }

    }
}