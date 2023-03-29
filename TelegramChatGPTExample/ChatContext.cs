using OpenAI_API.Chat;
using System;
using System.Threading;

namespace CloysterGPT
{
    internal class AIChatContext
    {
        #region vars
        private Conversation conversation;
        private Func<Conversation> conversationFactory;
        public SemaphoreSlim conversationSemaphore = new(1, 1);
        #endregion

        public Conversation GetConversation(Func<Conversation> conversationFactory)
        {
            _ = Interlocked.CompareExchange(ref this.conversationFactory, conversationFactory, null);
            if (Interlocked.CompareExchange(ref conversation, null, null) == null)
            {
                ReInitialize();
            }

            return conversation;
        }

        public async void ReInitialize()
        {
            _ = Interlocked.Exchange(ref conversation, conversationFactory());
            if (conversation == null)
            {
                Utils.WriteLine("Failed to initialize conversation thread");
                return;
            }

            await conversationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Initialize(conversation);
            }
            finally
            {
                _ = conversationSemaphore.Release();
            }
        }

        private static void Initialize(Conversation newConversation)
        {
            //todo: include an indicator here, such as the message/group id. cut it off though to avoid dox.
            Utils.WriteLine("New GPT conversation thread initiated");
            newConversation.AppendSystemMessage($"{DateTime.Now}");
            // newConversation.AppendExampleChatbotOutput($"Place facts and desired behaviour here");
        }
    }
}