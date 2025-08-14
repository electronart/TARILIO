using DocumentFormat.OpenXml.Presentation;
using eSearch.Models.AI;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class LLMMessageViewModel : ViewModelBase
    {

        public LLMMessageViewModel(Message existingMessage)
        {
            if (existingMessage == null) throw new ArgumentNullException(nameof(existingMessage));
            ExistingMessage = existingMessage;
        }


        public LLMMessageViewModel(string role, IAsyncEnumerable<string> messageStreamEnumerator, CancellationTokenSource cancellationSource)
        {
            if (role == null)                    throw new ArgumentNullException(nameof(role));
            if (messageStreamEnumerator == null) throw new ArgumentNullException(nameof(messageStreamEnumerator));
            this._messageStreamEnumerator = messageStreamEnumerator;
            this._role = role;
            this.CancellationSource = cancellationSource;
        }

        /// <summary>
        /// Populated only when this message is a restored message from a previous conversation
        /// or a user message ie. no api calls needed.
        /// </summary>
        private Message? ExistingMessage
        {
            get
            {
                return _existingMessage;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _existingMessage, value);
                this.IsFinishedStreaming = value != null;
            }
        }

        private Message? _existingMessage = null;

        /// <summary>
        /// Check IsFinishedStreaming for an indication if this MessageViewModel is a finished message yet.
        /// </summary>
        /// <returns></returns>
        public Message? GetFinalMessage()
        {
            return ExistingMessage;
        }

        public bool IsMessageRoleSystem
        {
            get {
                return Role.ToLower() == "system";
            }
        }

        public CancellationTokenSource? CancellationSource
        {
            get
            {
                return _cancellationSource;
            }
            set
            {
                this._cancellationSource = value;
            }
        }

        private CancellationTokenSource? _cancellationSource = null;
        private DateTime? _startedMessageStream;

        /// <summary>
        /// Populated only when this message is a streaming API call to an LLM.
        /// </summary>
        public async IAsyncEnumerable<string> GetMessageStreamEnumerator()
        {
            if (_messageStreamEnumerator != null)
            {
                _startedMessageStream = DateTime.Now;
                await foreach (var str in _messageStreamEnumerator)
                {
                    _recordedOutputContent.Append(str);
                    yield return str;
                }
                _messageStreamEnumerator = null;
                ExistingMessage = new Message
                {
                    Content = _recordedOutputContent.ToString(),
                    Role = _role,
                    Model = Program.ProgramConfig.GetSelectedConfiguration()?.Model ?? string.Empty
                };
                if (!string.IsNullOrEmpty(Note))
                {
                    ExistingMessage.Note = Note;
                }
            }
            else
            {
                // Either the message stream has already completed or this is an existing message.
                if (ExistingMessage == null)
                {
                    // _existingMessage is only null when using a message stream, so this means
                    // the message stream has already completed. Build a message object instead.
                    ExistingMessage = new Message
                    {
                        Content = _recordedOutputContent.ToString(),
                        Role = _role,
                        Model = Program.ProgramConfig.GetSelectedConfiguration()?.Model ?? string.Empty
                    };
                    if (!string.IsNullOrEmpty(Note))
                    {
                        ExistingMessage.Note = Note;
                    }
                }
                await foreach(var str in ExistingMessage.ContentAsAsyncEnumerable())
                {
                    yield return str;
                }
            }
        }

        public bool IsFinishedStreaming
        {
            get
            {
                return _isFinishedStreaming;
            } set
            {
                if (_startedMessageStream != null && ExistingMessage != null)
                {
                    DateTime now = DateTime.Now;
                    var elapsed = now - _startedMessageStream;
                    ExistingMessage.GenerationTime = elapsed;
                }
                this.RaiseAndSetIfChanged(ref _isFinishedStreaming, value);
            }
        }

        private bool _isFinishedStreaming = false;

        /// <summary>
        /// This should be set true for a short period of time after the user copies a message.
        /// It will display the 'done' checkbox to indicate the message was copied.
        /// </summary>
        public bool JustCopiedMessage
        {
            get => _justCopiedMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _justCopiedMessage, value);
            }
        }

        private bool _justCopiedMessage = false;


        private IAsyncEnumerable<string>? _messageStreamEnumerator;

        private StringBuilder _recordedOutputContent = new StringBuilder();

        public string Note
        {
            get
            {
                if (_note == null)
                {
                    _note = GetFinalMessage()?.Note ?? string.Empty;
                }
                return _note;
            }
            set
            {
                
                var msg = GetFinalMessage();
                if (msg != null)
                {
                    msg.Note = value;
                    this.RaiseAndSetIfChanged(ref _note, value);
                }
            }
        }

        private string? _note = null;

        public string Role
        {
            get
            {
                return ExistingMessage?.Role ?? _role;
            }
        }

        private string _role = string.Empty;

        public bool IsEditingNote
        {
            get
            {
                return _isEditingNote;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isEditingNote, value);
            }
        }

        private bool _isEditingNote = false;
    }

    public class DesignLLMMessageViewModel : LLMMessageViewModel
    {
        public DesignLLMMessageViewModel() : base(new Message { Content = "Hello World!", Role = "Sample Role", Model = "Sample Model Name" })
        {
        }
    }
}
