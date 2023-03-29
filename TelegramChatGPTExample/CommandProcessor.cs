using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloysterGPT
{
    internal static class CommandProcessor
    {
        private static readonly Dictionary<string, Action<Message>> commands = new();
        private static string tmpcmd = string.Empty;
        //private static readonly Dictionary<string> adminList = new();

        static CommandProcessor()
        {
            RegisterCommands();
        }

        /* Unit test
        [Test]
        public void ExecuteIfAICommand_StartsWithCommand_ReturnsTrue()
        {
            var message = new Message()
            {
                Text = "/test command args"
            };

            var result = YourClass.ExecuteIfAICommand(message);

            Assert.True(result);
        }
        */
        public static bool ExecuteIfAICommand(Message message)
        {
            tmpcmd = string.Empty;
            if (message?.Text?.StartsWith("/") == true)
            {
                tmpcmd = message.Text.Split(' ')[0];
            }

            foreach (var commandPair in commands.Where((value) => message?.Text?.Trim().StartsWith(value.Key) == true))
            {
                message.Text = message.Text[commandPair.Key.Length..];
                commandPair.Value?.Invoke(message);

                //todo: stop using tostrings idiot
                Utils.WriteLine("Command Response triggered: " + tmpcmd);
                tmpcmd = string.Empty;

                return true;
            }
            return false;
        }

        private static void RegisterCommands()
        {
            //todo: for some reason /start will work in non explicit channels
            // however /contact does not (vis also i think)
            commands.Add("/start", ClearMemoryCommand);
            commands.Add("/vis", ShowVisitors);
            commands.Add("/add", AddAccess);
            commands.Add("/del", DelAccess);
            commands.Add("/test", TestCommand);
            commands.Add("/contact", ContactDetails);
        }

        private static void ContactDetails(Message message)
        {
            //todo: remember to come back and refactor in admin to a list. needs to have more then 1 admin
            if (!CloysterGPT.IsAdmin(message))
                return;

            var chatId = message.Chat.Id;
            _ = CloysterGPT.Bot.SendMessage(new SendMessage
            {
                ChatId = chatId,
                Text = "Contact details:\n" +
                "Telegram: @Cloyster\n" +
                "Discord: Aruklas#9856\n"
            });
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

            if (CloysterGPT.visitors.Count > 0)
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = chatId,
                    Text = vis
                }).ConfigureAwait(false);
            }
            else
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = chatId,
                    Text = "No visitors currently"
                }).ConfigureAwait(false);
            }
        }

        private static void AddAccess(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            if (long.TryParse(message.Text, out var chatId))
            {
                _ = CloysterGPT.visitors.AddOrUpdate(chatId, (long id) =>
                {
                    Visitor arg = new(true, "Unknown");
                    return arg;
                }, (long id, Visitor arg) =>
                {
                    arg.access = true;
                    return arg;
                });

                ShowVisitors(message);
            }
            else
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = message.Chat.Id,
                    Text = "Invalid visitor ID format. Please enter a valid visitor ID."
                }).ConfigureAwait(false);
            }
        }

        private static void DelAccess(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            if (long.TryParse(message.Text, out var chatId))
            {
                _ = CloysterGPT.visitors.AddOrUpdate(chatId, (long id) =>
                {
                    Visitor arg = new(false, "Unknown");
                    return arg;
                }, (long id, Visitor arg) =>
                {
                    arg.access = false;
                    return arg;
                });

                ShowVisitors(message);
            }
            else
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = message.Chat.Id,
                    Text = "Invalid visitor ID format. Please enter a valid visitor ID."
                }).ConfigureAwait(false);
            }
        }

        private static void TestCommand(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            Utils.WriteLine("Test command triggered");
        }

        private static void ClearMemoryCommand(Message message)
        {
            var chatId = message.Chat.Id;
            var chatContext = CloysterGPT.contextByChats[chatId];
            chatContext.ReInitialize();

            _ = CloysterGPT.Bot.SendMessage(new SendMessage
            {
                ChatId = chatId,
                Text = "The dialogue is cleared for GPT"
            }).ConfigureAwait(false);
        }

        /* WIP Refactor
        private static void AddAccess(Message message)
        {
            if (!CloysterGPT.IsAdmin(message))
                return;

            string[] args = message.Text.Split(" ");
            if (args.Length != 3)
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = message.Chat.Id,
                    Text = "Invalid input format. Usage: /add <user_id> <true/false>"
                });
                return;
            }

            if (!long.TryParse(args[1], out long id))
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = message.Chat.Id,
                    Text = $"Invalid user id: {args[1]}"
                });
                return;
            }

            if (!bool.TryParse(args[2], out bool access))
            {
                _ = CloysterGPT.Bot.SendMessage(new SendMessage
                {
                    ChatId = message.Chat.Id,
                    Text = $"Invalid access value: {args[2]}. Please enter true or false."
                });
                return;
            }

            _ = CloysterGPT.visitors.AddOrUpdate(id, (long id) => new Visitor(access, "Unknown"), (long id, Visitor arg) =>
            {
                arg.access = access;
                return arg;
            });

            ShowVisitors(message);
        }
        */
    }
}
