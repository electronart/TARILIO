using Newtonsoft.Json;
using nietras.SeparatedValues;
using org.mp4parser.aspectj.lang.reflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.AI
{
    public class Message
    {
        public required string Role     { get; set; } // "system", "user", "assistant"
        public required string Content  { get; set; }

        public DateTime Time { get; set; } = DateTime.Now;

        /// <summary>
        /// User in this case refers to the machine user at operating system level.
        /// Eg. On windows may return in format WORKGROUP\USERNAME
        /// </summary>
        public string? User
        {
            get
            {
                if (_user == null)
                {
                    _user = Utils.GetOSUserInfo() ?? string.Empty;
                }
                return _user;
            }
            set
            {
                _user = value;
            }
        }

        private string? _user = null;

        /// <summary>
        /// The Language Model name that was set at time of request/response.
        /// </summary>
        public required string Model { get; set; }

        public async IAsyncEnumerable<string> ContentAsAsyncEnumerable()
        {
            yield return Content;
        }

        public string? Machine
        {
            get
            {
                if (_machine == null)
                {
                    _machine = System.Environment.MachineName;
                }
                return _machine;
            }
            set
            {
                _machine = value;
            }
        }

        private string? _machine = null;

        public string Note = string.Empty;
    }

    public class Conversation
    {
        public string Id = Guid.NewGuid().ToString();

        public List<Message> Messages { get; set; } = new List<Message>();

        public void ExportAsCSVFile(string filePath)
        {
            using var writer = Sep.New(',').Writer(o => o with { Escape = true }).ToFile(filePath);
            foreach(var message in Messages)
            {
                using var row = writer.NewRow();
                row["Role"].Set(message.Role);
                row["Time"].Set(message.Time.ToString());
                row["Content"].Set(message.Content);
                row["Model"].Set(message.Model);
                row["User"].Set(message.User);
                row["Machine"].Set(message.Machine);
                row["Note"].Set(message.Note);
            }
        }

        public bool HasMessages()
        {
            return Messages.Count > 0;
        }

        public DateTime GetTimeOfFirstMessage()
        {
            return Messages[0].Time;
        }

        public static Conversation ImportFromCSVFile(string filePath)
        {
            List<Message> importedMessages = new List<Message>();
            using var reader = Sep.New(',').Reader(o => o with { Unescape = true }).FromFile(filePath);
            foreach(var row in reader)
            {
                var role    = row["Role"];
                var content = row["Content"];
                var time    = row["Time"];
                var model   = row["Model"];
                var user    = row["User"];
                var machine = row["Machine"];
                var note    = row["Note"];
                importedMessages.Add(new Message
                {
                    Role = role.ToString(),
                    Content = content.ToString(),
                    Time = DateTime.Parse(time.ToString()),
                    Model = model.ToString(),
                    User = user.ToString(),
                    Machine = machine.ToString(),
                    Note = note.ToString()
                });
            }
            Conversation conversation = new Conversation { Messages = importedMessages };
            return conversation;
        }

        public void ExportAsJsonLFile(string path)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var message in Messages)
            {
                sb.AppendLine(JsonConvert.SerializeObject(message, Formatting.None));
            }

            System.IO.File.WriteAllText(path, sb.ToString());
        }

        public static Conversation ImportFromJsonLFile(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);

            Conversation importedConversation = new Conversation();

            foreach(string line in lines)
            {
                Message? message = JsonConvert.DeserializeObject<Message>(line);
                if (message != null) {
                    importedConversation.Messages.Add(message);
                }
            }
            return importedConversation;
        }
    }
}
