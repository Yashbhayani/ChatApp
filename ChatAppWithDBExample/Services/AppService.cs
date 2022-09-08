using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ChatAppWithDBExample.Hubs;
using ChatAppWithDBExample.Models;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace ChatAppWithDBExample.Services
{
    public class AppService
    {
        private readonly Chat_AppDbEntities _Context;

        public string ConnectionID { get; private set; }

        public AppService()
        {
            _Context = new Chat_AppDbEntities();
        }

        public bool Login(LoginData loginData, out int userId)
        {
            userId = 0;
            var currentUser = _Context.UserTables.FirstOrDefault(x => x.Username == loginData.Username && x.Password == loginData.Password);
            if (currentUser != null)
            {
                userId = currentUser.Userid;
                return true;
            }
            return false;
        }

        public List<UserDTO> GetUsersToChat()
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            return _Context.UserTables
                .Include("UserConnections")
                .Where(x => x.Userid != userId)
                .Select(x => new UserDTO
                {
                    UserId = x.Userid,
                    UserName = x.Username,
                    FullName = x.Fullname,
                    IsOnline = x.UserConnections.Count > 0,
                }).ToList();
        }

        internal int AddUserConnection(Guid connectionId)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            _Context.UserConnections.Add(new UserConnection
            {
                Connectionid = connectionId,
                Userid = userId,
            });
            _Context.SaveChanges();
            return userId;
        }

        internal int RemoveUserConnection(Guid ConnectionID)
        {
            int userId = 0;
            var current = _Context.UserConnections.FirstOrDefault(x => x.Connectionid == ConnectionID);
            if (current != null)
            {
                userId = current.Userid;
                _Context.UserConnections.Remove(current);
                _Context.SaveChanges();
            }
            return userId;
        }

        internal IList<string> GetUSerConnections(int uSerId)
        {
            return _Context.UserConnections.Where(x => x.Userid == uSerId).Select(x => x.Connectionid.ToString()).ToList();
        }

        internal void RemoveAllUserConnections(int userId)
        {
            var current = _Context.UserConnections.Where(x => x.Userid == userId);
            _Context.UserConnections.RemoveRange(current);
            _Context.SaveChanges();
        }

        internal ChatBoxModel GetChatbox(int toUserId)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            var toUser = _Context.UserTables.FirstOrDefault(x => x.Userid == toUserId);
            var messages = _Context.Messages.Where(x => (x.FromUser == userId && x.touser == toUserId) || (x.FromUser == toUserId && x.touser == userId))
                .OrderByDescending(x => x.date)
                .Skip(0)
                .Take(50)
                .Select(x => new MessageDTO
                {
                    ID = x.id,
                    Message = x.message1,
                    Class = x.FromUser == userId ? "from" : "to",
                })
                .OrderBy(x => x.ID)
                .ToList();

            return new ChatBoxModel
            {
                ToUser = ToUserDTO(toUser),
                Messages = messages,
            };
        }

        internal bool SendMessage(int toUserId, string message)
        {
            try
            {
                int USER_ID = int.Parse(HttpContext.Current.User.Identity.Name);
                _Context.Messages.Add(new Message
                {
                    FromUser = USER_ID,
                    touser = toUserId,
                    message1 = message,
                    date = DateTime.Now
                });
                _Context.SaveChanges();
                ChatHub.RecieveMessage(USER_ID, toUserId, message);
                return true;
            }
            catch { return false; }
        }

        public UserDTO ToUserDTO(UserTable user)
        {
            return new UserDTO
            {
                FullName = user.Fullname,
                UserId = user.Userid,
                UserName = user.Username,
            };
        }


        internal List<MessageDTO> LazyLoadMssages(int toUserId, int skip)
        {
            var userId = int.Parse(HttpContext.Current.User.Identity.Name);
            var messages = _Context.Messages.Where(x => (x.FromUser == userId && x.touser == toUserId) || (x.FromUser == toUserId && x.touser == userId))
                .OrderByDescending(x => x.date)
                .Skip(skip)
                .Take(50)
                .Select(x => new MessageDTO
                {
                    ID = x.id,
                    Message = x.message1,
                    Class = x.FromUser == userId ? "from" : "to",
                })
                .OrderByDescending(x => x.ID)
                .ToList();
            return messages;
        }
    }
}
