﻿/// <licence>
/// 
/// (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)
/// 
/// Pervasive Interaction Technology Laboratory (pIT lab)
/// IT University of Copenhagen
///
/// This library is free software; you can redistribute it and/or 
/// modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
/// as published by the Free Software Foundation. Check 
/// http://www.gnu.org/licenses/gpl.html for details.
/// 
/// </licence>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using NooSphere.Cloud.ActivityManager.Authentication;
using NooSphere.Cloud.ActivityManager.Events;
using NooSphere.Cloud.Data.Registry;
using NooSphere.Cloud.Data.Storage;
using NooSphere.Core.ActivityModel;

namespace NooSphere.Cloud.ActivityManager.Controllers.Api
{
    public class FriendController : BaseController
    {
        #region Private Members
        private UserController UserController = new UserController();
        private FriendRequestRegistry FriendRequestRegistry = new FriendRequestRegistry(ConfigurationManager.AppSettings["MONGOLAB_URI"]);
        #endregion

        #region Exposed API Methods
        /// <summary>
        /// Get a complete list of friends for the specific user.
        /// </summary>
        /// <returns>Json representation of the list of friends.</returns>
        [RequireUser]
        public List<JObject> Get(Guid userId)
        {
            if (CurrentUserId == userId)
            {
                List<User> friends = UserRegistry.Get(CurrentUserId).Friends;
                return UserController.ReturnObject(friends);
            }
            return null;
        }

        /// <summary>
        /// Make a friend request.
        /// </summary>
        /// <param name="userId">Guid representation of the user Id.</param>
        /// <param name="friendId">Guid representation of the friend Id.</param>
        /// <returns>Returns true if the friend request was performed, false if not.</returns>
        [RequireUser]
        public bool Post(Guid userId, Guid friendId)
        {
            if (CurrentUserId == userId)
            {
                User user = UserRegistry.Get(userId);
                User friend = UserRegistry.Get(friendId);
                FriendRequest fr = new FriendRequest();
                fr.UserId = user.Id;
                fr.FriendId = friend.Id;
                if (FriendRequestRegistry.Add(fr))
                {
                    Notifier.NotifyGroup(userId, NotificationType.FriendRequest, friend);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Respond to friend request.
        /// </summary>
        /// <param name="userId">Guid representation of the user Id.</param>
        /// <param name="friendId">Guid representation of the friend Id.</param>
        /// <param name="approve">Boolean; true to accept, false to decline.</param>
        /// <returns>Returns true if response is processed, false if not.</returns>
        [RequireUser]
        public bool Post(Guid userId, Guid friendId, bool approve)
        {
            if (CurrentUserId == friendId)
            {
                if (FriendRequestRegistry.Remove(userId, friendId))
                {
                    if (approve)
                    {
                        CreateFriendship(userId, friendId);
                        CreateFriendship(friendId, userId);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Delete a friend connection.
        /// </summary>
        /// <param name="userId">Guid representation of the user Id.</param>
        /// <param name="friendId">Guid representation of the friend Id.</param>
        /// <returns>Returns true if the friend connection was deleted, false if not.</returns>
        [RequireUser]
        public bool Delete(Guid userId, Guid friendId)
        {
            if (CurrentUserId == userId || CurrentUserId == friendId)
            {
                DestroyFriendship(userId, friendId);
                DestroyFriendship(friendId, userId);
                
                return true;
            }
            return false;
        }
        #endregion

        #region Public Methods
        public List<FriendRequest> GetFriendRequests(Guid userId)
        {
            return FriendRequestRegistry.Get(userId);
        }

        public void Clear()
        {
            foreach (FriendRequest fr in FriendRequestRegistry.Get())
            {
                FriendRequestRegistry.Remove(fr.Id);
            }
        }
        #endregion

        #region Private Methods
        private void CreateFriendship(Guid userId, Guid friendId)
        {
            JObject user = UserController.GetExtendedUser(userId);
            JObject friend = UserController.GetExtendedUser(friendId);

            List<JObject> friends;
            if (user["Friends"] != null)
                friends = user["Friends"].Children<JObject>().ToList();
            else
                friends = new List<JObject>();
            friends.Add(friend);
            user["Friends"] = JToken.FromObject(friends);

            UserController.UpdateUser(user);
            foreach(Device device in DeviceRegistry.ConnectedDevices(userId))
                Notifier.Subscribe(device.ConnectionId, friendId);
            Notifier.NotifyGroup(userId, NotificationType.FriendAdded, friend);
        }

        private void DestroyFriendship(Guid userId, Guid friendId)
        {
            JObject user = UserController.GetExtendedUser(userId);
            JObject friend = UserController.GetExtendedUser(friendId);

            List<User> friends = UserRegistry.Get(userId).Friends.Where(u => u.Id != friendId).ToList();

            List<JObject> result = new List<JObject>();
            foreach (User f in friends)
                result.Add(UserController.GetExtendedUser(f.Id));

            user["Friends"] = JToken.FromObject(result);

            UserController.UpdateUser(user);
            foreach (Device device in DeviceRegistry.ConnectedDevices(userId))
                Notifier.Unsubscribe(device.ConnectionId, friendId);
            Notifier.NotifyGroup(userId, NotificationType.FriendDeleted, friendId);
        }
        #endregion
    }
}