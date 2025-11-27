using eSearch.Models.AI;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class LLMConversationViewModel
    {
        public LLMConversationViewModel(Conversation conversation)
        {
            foreach (var message in conversation.Messages)
            {
                var messageVM = new LLMMessageViewModel(message);
                Messages.Add(messageVM);
            }
        }

        /// <summary>
        /// Note this will extract the conversation from completions etc.
        /// </summary>
        /// <returns></returns>
        public Conversation ExtractConversation()
        {
            Conversation extractedConversation = new Conversation();
            foreach (var messageVM in Messages)
            {
                
                var message = messageVM.GetFinalMessage();
                if (message != null)
                {
                    if (messageVM.Attachments.Count > 0)
                    {
                        message.AttachmentPaths = new List<string>();
                        foreach(var attachmentVM in messageVM.Attachments)
                        {
                            message.AttachmentPaths.Add(attachmentVM.FilePath);
                        }
                    }
                    extractedConversation.Messages.Add(message);
                }
            }
            return extractedConversation;
        }

        public ObservableCollection<LLMMessageViewModel> Messages
        {
            get
            {
                return _messages;
            }
        }

        private ObservableCollection<LLMMessageViewModel> _messages = new ObservableCollection<LLMMessageViewModel>();
    }

    public class DesignLLMConversationViewModel : LLMConversationViewModel
    {
        public DesignLLMConversationViewModel() : base(DesignConversation)
        {
        }

        public static Conversation DesignConversation
        {
           get
           {
                List<Message> messages = new List<Message>();
                messages.Add(new Message { Role = "assistant", Content = "#A Header\nThis is sample message 1", Model = "Sample Model Name" });
                messages.Add(new Message { Role = "user", Content = "A message from the User.", Model = "Sample Model Name" });
                Conversation convo = new Conversation { Id = Guid.NewGuid().ToString(), Messages = messages };
                return convo;
           }
        }
    }
}
