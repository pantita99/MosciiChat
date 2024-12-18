﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly StarCat_Context _context;

    // Constructor that injects the StarCat_Context
    public ChatHub(StarCat_Context context)
    {
        _context = context;
    }

    // ฟังก์ชันสำหรับส่งข้อความ
    public async Task SendMessage(string senderFullName, string receiverFullName, string messageText)
    {
        try
        {
            // ตรวจสอบว่า messageText ไม่เป็น null หรือว่าง
            if (string.IsNullOrEmpty(messageText))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Cannot send an empty message.");
                return;  // หยุดการทำงานหากข้อความเป็นว่าง
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

            // สร้างหรือดึง GUID สำหรับแชท
            var chatGuid = await GetOrCreateChatGuid(senderId.ToString(), receiverId);

            // บันทึกประวัติการแชท (กรณีที่ไม่มีประวัติการแชท จะบันทึกเป็นข้อความว่าง)
            await SaveChatHistory(chatGuid, senderId.ToString(), receiverId, senderFullName, messageText);

            // ส่งข้อความไปยังผู้รับและผู้ส่ง
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
        // สมมติว่าคุณเก็บ User ID ไว้ใน Context
        var userId = Context.User?.FindFirst("UserID")?.Value;
        return int.TryParse(userId, out var id) ? id : 0;
    }

    // ฟังก์ชันสำหรับการส่งไฟล์
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

        // ตรวจสอบว่าเคยมีการสนทนาหรือไม่
        var existingChat = await _context.TB_CHATHISTRY
            .FirstOrDefaultAsync(chat => (chat.ID == senderId.ToString() && chat.IDRECIVER == receiverId) ||
                                         (chat.ID == receiverId && chat.IDRECIVER == senderId.ToString()));

        // ใช้ GUID เดิมหากมีประวัติการสนทนาแล้ว
        var chatGuid = existingChat?.GUID ?? Guid.NewGuid().ToString();

        var chatEntry = new TB_CHATHISTRY
        {
            GUID = chatGuid,  // ใช้ GUID เดิม
            ID = senderId.ToString(),
            IDRECIVER = receiverIdInt.ToString(),
            MESSAGE = "[File Sent]",
            NAME = Context.User.Identity.Name,
            FILENAME = fileName
        };

        _context.TB_CHATHISTRY.Add(chatEntry);
        await _context.SaveChangesAsync();

        // ส่งไฟล์ไปยังผู้รับ
        await Clients.User(receiverId).SendAsync("ReceiveFile", fileName, fileBytes);
    }

    // ฟังก์ชันดึงประวัติการสนทนา
    public async Task<List<GetUser>> GetChatHistory(string userId1, string userId2)
    {
        try
        {
            if (string.IsNullOrEmpty(userId1) || string.IsNullOrEmpty(userId2))
            {
                throw new ArgumentException("User IDs cannot be null or empty.");
            }

            var chatHistory = await _context.TB_CHATHISTRY
                .Where(chat => (chat.ID == userId1 && chat.IDRECIVER == userId2) ||
                               (chat.ID == userId2 && chat.IDRECIVER == userId1))
                .OrderBy(chat => chat.GUID)  // เรียงตาม GUID
                .ToListAsync();

            var chatMessages = chatHistory.Select(chat => new GetUser
            {
                UserID = chat.ID,
                FullName = chat.NAME,
                Message = chat.MESSAGE,
                Filename = chat.FILENAME
            }).ToList();

            return chatMessages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetChatHistory: {ex.Message}");
            return new List<GetUser>();
        }
    }
    // ฟังก์ชันแสดงประวัติการสนทนาโดย GUID
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
                .ToListAsync();

            return chatHistory.Select(chat => new GetUser
            {
                UserID = chat.ID,
                FullName = chat.NAME,
                Message = chat.MESSAGE,
                Filename = chat.FILENAME
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
            var users = _context.TB_AUTHENTICATION
                .Where(auth => auth.UserID != myUserID)
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

            // ดึงข้อมูลผู้ใช้ที่มีประวัติแชท
            var usersWithHistory = await _context.TB_AUTHENTICATION
                .Where(auth => _context.TB_CHATHISTRY
                    .Any(chat => chat.ID == auth.UserID.ToString() || chat.IDRECIVER == auth.UserID.ToString()))
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
            Console.WriteLine($"Error in GetUserListWithChatHistoryAsync: {ex.Message}");
            throw new HubException("An error occurred while retrieving users with chat history.");
        }
    }


    public async Task SaveChatHistory(string guid, string senderId, string receiverId, string name, string message, string filename = null)
    {
        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
        {
            throw new ArgumentException("Invalid arguments. GUID, senderId, and receiverId cannot be null or empty.");
        }

        try
        {
            // ค้นหาประวัติแชทที่มี GUID, senderId, receiverId
            var existingChat = await _context.TB_CHATHISTRY
                .FirstOrDefaultAsync(chat => chat.GUID == guid && chat.ID == senderId && chat.IDRECIVER == receiverId);

            if (existingChat != null)
            {
                // อัปเดตข้อความที่มีอยู่ โดยไม่ใช้การต่อข้อความยาวเกินไป
                if (!string.IsNullOrWhiteSpace(message))
                {
                    existingChat.MESSAGE += string.IsNullOrEmpty(existingChat.MESSAGE) ? message : "#$" + message;
                }
                existingChat.FILENAME = filename;  // อัปเดตไฟล์แนบ
                _context.TB_CHATHISTRY.Update(existingChat);
            }
            else
            {
                // ตรวจสอบว่าแชทระหว่าง senderId และ receiverId มี GUID หรือไม่
                var chatWithSameIds = await _context.TB_CHATHISTRY
                    .FirstOrDefaultAsync(chat => chat.ID == senderId && chat.IDRECIVER == receiverId);

                if (chatWithSameIds != null)
                {
                    // ถ้ามี GUID อื่นๆ ให้ใช้ GUID ที่มีอยู่เพื่ออัปเดตข้อความ
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        chatWithSameIds.MESSAGE += string.IsNullOrEmpty(chatWithSameIds.MESSAGE) ? message : "#$" + message;
                    }
                    _context.TB_CHATHISTRY.Update(chatWithSameIds);
                }
                else
                {
                    // ถ้าไม่มีประวัติการแชท ให้บันทึกข้อความเป็น null หรือ "" (ข้อความว่าง)
                    var newChat = new TB_CHATHISTRY
                    {
                        GUID = guid,
                        ID = senderId,
                        IDRECIVER = receiverId,
                        NAME = name,
                        MESSAGE = string.IsNullOrWhiteSpace(message) ? null : message,  // บันทึกข้อความเป็น null ถ้า message เป็นค่าว่าง
                        FILENAME = filename
                    };

                    await _context.TB_CHATHISTRY.AddAsync(newChat);
                }
            }

            // บันทึกการเปลี่ยนแปลง
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving chat history: {ex.Message}");
            throw;
        }
    }






    public async Task<string> GetOrCreateChatGuid(string senderId, string receiverId)
    {
        // Look for an existing GUID for the chat between sender and receiver
        var existingChat = await _context.TB_CHATHISTRY
            .Where(c => (c.ID == senderId && c.IDRECIVER == receiverId) || (c.ID == receiverId && c.IDRECIVER == senderId))
            .Select(c => c.GUID)
            .FirstOrDefaultAsync();

        // Return the existing GUID if found, otherwise create a new one
        return existingChat ?? Guid.NewGuid().ToString();
    }


    // โมเดลข้อมูลผู้ใช้
    public class GetUser
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public bool? UserConnected { get; set; }
        public string Message { get; set; }    // เพิ่ม property Message
        public string Filename { get; set; }    // เพิ่ม property Filename
    }
}