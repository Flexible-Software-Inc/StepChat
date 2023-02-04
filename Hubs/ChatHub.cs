﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NuGet.Protocol;
using StepChat.Models;
using System.Security.Cryptography;

namespace StepChat.Hubs
{
    public class ChatHub : Hub
    {
        public async Task StartMessaging(string? userId)
        {
            var aes = Aes.Create();
            aes.BlockSize = 256;
            aes.KeySize = 32;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            await Clients.Caller.SendAsync("SendPrivateKeys", aes.Key);
            await Clients.User(userId!).SendAsync("SendPrivateKeys", aes.Key);
        }
        public async Task SendMessage(MessagesModel? context, string? userId)
        {
            if (userId != null && context != null)
            {
                var a = Clients.All.ToJson();
                var c = Clients.Users(userId!).ToJson();
                await Clients.User(userId!).SendAsync("ReceiveMessage", context);
            }
        }

    }
}
