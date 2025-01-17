﻿using CloysterGPT.Resources;
using OpenAI_API;
using OpenAI_API.Chat;
using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloysterGPT
{
    internal class Visitor
    {
        public bool access;
        public string who;
        public int attempts;

        public Visitor(bool access, string who, int attempts = 0)
        {
            this.access = access;
            this.who = who;
            this.attempts = attempts;
        }
    }

    internal static class CloysterGPT
    {
        #region variables for control
        const long maxUniqueVisitors = 5;  // Restriction to prevent users sharing
        const string groupChatPrefix = "!gpt"; // Prefix for a message in a group chat to allow the bot to distinguish between a message that should be treated as a question and side talks.
        #endregion

        #region initializers
        public static RxTelegram.Bot.TelegramBot Bot;
        internal static ConcurrentDictionary<long, Visitor> visitors;
        internal static ConcurrentDictionary<long, AIChatContext> contextByChats;
        private static OpenAIAPI AI { get; set; }
        #endregion

        static CloysterGPT()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            visitors = new ConcurrentDictionary<long, Visitor>();
            Bot = new RxTelegram.Bot.TelegramBot(appsettings.botId);
            contextByChats = new ConcurrentDictionary<long, AIChatContext>();
            AI = new OpenAIAPI(appsettings.OpenAIkey);
        }

        public static async Task Main()
        {
            await Run();
        }

        private static async Task Run()
        {
            var me = await Bot.GetMe();
            Utils.WriteLine($"Bot name: @{me.Username}");

            var messageListener = Bot.Updates.Message.Subscribe(HandleMessage, exception =>
            {
                Utils.WriteLine($"An error has occured: {exception.Message}");
                
                //todo: add some logic in here to detirmine if it was just a blip in internet. don't just auto terminate immediately
            });

            _ = Console.ReadLine();
            messageListener.Dispose();
        }

        private static async void HandleMessage(Message message)
        {
            if (message.Text == null)
                return;

            var chatId = message.Chat.Id;
            try
            {
                if (!HasAccess(message, chatId))
                {
                    _ = Bot.SendMessage(new SendMessage
                    {
                        ChatId = chatId,
                        Text = "You do not have access to ChatGPT, please contact the bot administrator to get access."
                    });
                    return;
                }

                bool isExecuted = CommandProcessor.ExecuteIfAICommand(message);
                if (isExecuted)
                    return;

                bool isPersonalChat = chatId == message.From.Id;
                bool isExplicitAICall = !isPersonalChat && message.Text.StartsWith(groupChatPrefix);

                //todo: authorize the testing channel with aru as admin (may as well rework admin into dict list)
                //todo: should we just do a user base class and do inherits from it?
                //todo: we seem to be killing the conversation class when we change channels, can it be preserved simply?
                if (isPersonalChat || isExplicitAICall)
                {
                    var chatContext = contextByChats.GetOrAdd(chatId, new AIChatContext());
                    await chatContext.conversationSemaphore.WaitAsync();
                    string response = "";

                    try
                    {
                        var conversation = chatContext.GetConversation(() => { return AI.Chat.CreateConversation(); });
                        conversation.AppendMessage(new ChatMessage(ChatMessageRole.User, message.Text));
                        response = await conversation.GetResponseFromChatbotAsync();

                    }
                    finally
                    {
                        _ = chatContext.conversationSemaphore.Release();
                    }

                    Utils.WriteLine("GPT Response Triggered");
                    _ = Bot.SendMessage(new SendMessage
                    {
                        ChatId = chatId,
                        Text = response
                    });
                }
            }
            catch (Exception exception)
            {
                var contextLimitTag = "This model's maximum context length is";
                if (exception.Message.Contains(contextLimitTag))
                {
                    Utils.WriteLine("GPT's max context length has been reached");
                    _ = Bot.SendMessage(new SendMessage
                    {
                        ChatId = chatId,
                        Text = "Allowed dialogue length exceeded. Please reset the conversation thread. (An Admin can type /start to reset)"
                    });

                    //todo: why dont we just call the clearchatcommand / reinitializer?
                }
                else
                {
                    _ = Bot.SendMessage(new SendMessage
                    {
                        ChatId = chatId,
                        Text = $"An error has occured: {exception.Message}"
                    });
                }
            }
        }

        public static bool IsAdmin(Message message)
        {
            //todo: this doesnt take into account the user in the channel, just the channel itself
            return message.Chat.Id == appsettings.adminId;
        }

        private static bool HasAccess(Message message, long chatId)
        {
            //todo: casual reminder to make your admin list a dictionary
            var isAdmin = IsAdmin(message);
            if (isAdmin)
            {
                _ = visitors.TryAdd(chatId, new Visitor(isAdmin, message.From.Username));
                return isAdmin;
            }

            var visitor = visitors.GetOrAdd(chatId, (long id) => { Visitor arg = new(false, message.From.Username); return arg; });
            if (visitors.Count < maxUniqueVisitors)
                return true;

            return visitor.access;
        }
    }

    internal class APIUsageException : Exception
    {
        public long chatId;
        public APIUsageException(string message, long chatId) : base(message)
        {
            this.chatId = chatId;
        }
    }

    internal class ContextSizeExceededException : APIUsageException
    {
        public ContextSizeExceededException(string message, long chatId) : base(message, chatId)
        {
        }
    }
}