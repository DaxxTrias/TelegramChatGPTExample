﻿using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloysterGPT
{
    internal static class CommandProcessor
    {
        private static readonly Dictionary<string, Action<Message>> commands = new();

        static CommandProcessor()
        {
            RegisterCommands();
        }

        public static bool ExecuteIfAICommand(Message message)
        {
            foreach (var commandPair in commands.Where((value) => message.Text.Trim().StartsWith(value.Key)))
            {
                message.Text = message.Text[commandPair.Key.Length..];
                commandPair.Value(message);
                return true;
            }

            return false;
        }

        private static void RegisterCommands()
        {
            commands.Add("/start", ClearMemoryCommand);
            commands.Add("/vis", ShowVisitors);
            commands.Add("/add", AddAccess);
            commands.Add("/del", DelAccess);
        }

        private static void ShowVisitors(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            var chatId = message.Chat.Id;
            var data = CloysterGPT.visitors.ToList();
            string vis = "Visitors:\n";
            foreach (var item in data)
            {
                vis += $"`{item.Key}` - {item.Value.who}:{item.Value.access}\n";
            }
            _ = CloysterGPT.Bot.SendMessage(new SendMessage
            {
                ChatId = chatId,
                Text = vis
            }).ConfigureAwait(false);
        }

        private static void AddAccess(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            _ = CloysterGPT.visitors.AddOrUpdate(long.Parse(message.Text), (long id) => { Visitor arg = new(true, "Unknown"); return arg; }, (long id, Visitor arg) => { arg.access = true; return arg; });
            ShowVisitors(message);
        }

        private static void DelAccess(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            _ = CloysterGPT.visitors.AddOrUpdate(long.Parse(message.Text), (long id) => { Visitor arg = new(false, "Unknown"); return arg; }, (long id, Visitor arg) => { arg.access = false; return arg; });
            ShowVisitors(message);
        }

        private static void ClearMemoryCommand(Message message)
        {
            var chatId = message.Chat.Id;
            var chatContext = CloysterGPT.contextByChats[chatId];
            chatContext.ReInitialize();

            _ = CloysterGPT.Bot.SendMessage(new SendMessage
            {
                ChatId = chatId,
                Text = "The dialogue is cleared for AI"
            }).ConfigureAwait(false);
        }
    }
}
