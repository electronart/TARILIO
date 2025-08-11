using eSearch.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XstReader;
using XstReader.ElementProperties;

namespace eSearch.Models.Documents.Parse
{
    internal class PSTParser : IParser
    {
        /// <summary>
        /// (Re)used for parsing attachments.
        /// </summary>
        private FileSystemDocument attachmentFSD = null;

        public string[] Extensions
        {
            get { return new string[] { "pst", "ost" }; }
        }

        private List<IDocument> ParsedRecords
        {
            get
            {
                if (_parsedRecords == null)
                {
                    _parsedRecords = new List<IDocument>();
                }
                return _parsedRecords;
            }
        }

        List<IDocument> _parsedRecords = null;

        public void Parse(string filePath, out ParseResult parseResult)
        {
            _parsedRecords = null;
            attachmentFSD = null;
            parseResult = new ParseResult
            {
                ParserName = "PSTParser (XstReader)",
                Authors = new string[] { Utils.GetFileOwner(filePath) },
                TextContent = "PST File",
                Title = "<PST File> " + Path.GetFileNameWithoutExtension(filePath),
            };
            using (var xstFile = new XstFile(filePath))
            {
                ProcessFoldersRecursively(filePath, xstFile.RootFolder);
            }
            parseResult.SubDocuments = ParsedRecords;
        }

        private void ProcessFoldersRecursively(string filePath, XstFolder folder)
        {
            InMemoryDocument folderResult = BaseParseResult("PST Folder", filePath);
            folderResult.MetaData = GetAllFolderMetadata(folder);
            folderResult.DisplayName = "<PST Folder> " + folder.DisplayName;
            ParsedRecords.Add(folderResult);
            foreach(var subFolder in folder.Folders)
            {
                ProcessFoldersRecursively(filePath, subFolder);
            }
            ProcessMessages(filePath, folder);
        }

        private void ProcessMessages(string filePath, XstFolder folder)
        {
            foreach(var message in folder.Messages)
            {
                try
                {
                    var msgResult = ProcessMessage(filePath, message);
                    ParsedRecords.Add(msgResult);
                    ProcessAttachments(filePath, message);
                } catch (Exception ex)
                {
                    InMemoryDocument errorResult = BaseParseResult("PST Message", filePath);
                    errorResult.Text = $"There was an error parsing a message in {filePath}. {ex.Message}";
                    errorResult.ShouldSkipIndexing = IDocument.SkipReason.ParseError;
                    ParsedRecords.Add(errorResult);
                }
            }
        }

        private InMemoryDocument ProcessMessage(string filePath, XstMessage message, string prependTitle = "")
        {
            Debug.WriteLine("ProcessMessage...");
            InMemoryDocument messageResult = BaseParseResult("PST Message", filePath);
            messageResult.MetaData = GetAllMessageMetadata(message);
            messageResult.Text = message.Body.Text;
            messageResult.DisplayName = prependTitle + (message.Subject ?? "Untitled Message");
            messageResult.FileName = filePath;
            return messageResult;
        }

        private void ProcessAttachments(string filePath, XstMessage message)
        {
            if (!Directory.Exists(Program.ESEARCH_TEMP_FILES_PATH))
            {
                Directory.CreateDirectory(Program.ESEARCH_TEMP_FILES_PATH);
            }

            foreach (var attachment in message.Attachments)
            {
                if (attachment.IsFile)
                {
                    string fileName = attachment.FileNameForSaving;
                    if (string.IsNullOrEmpty(fileName)) fileName = Guid.NewGuid().ToString();
                    string tempPath = Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, fileName);
                    attachment.SaveToFile(tempPath);
                    if (attachmentFSD == null)
                    {
                        attachmentFSD = new FileSystemDocument();
                    }
                    attachmentFSD.SetDocument(tempPath);
                    ParsedRecords.Add(attachmentFSD);
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception)
                    { 
                        // Silently swallow. We can clean these up later - Not critical to indexing.
                    }
                }
                if (attachment.IsEmail)
                {
                    var attachedMessage = attachment.AttachedEmailMessage;
                    if (attachedMessage != null)
                    {
                        ParsedRecords.Add(ProcessMessage(filePath, attachedMessage, "<Attached Message>"));
                    }
                }
                
            }
        }

        private List<Metadata> GetAllMessageMetadata(XstMessage message)
        {
            List<Metadata> result = new List<Metadata>()
            {
                GetDisplayNameMetadata(message),
                GetIsNotRealFileMarkerMetadata(),
                new Metadata { Key = "To", Value = message.To },
                new Metadata { Key = "From", Value = message.From },
                new Metadata { Key = "Priority", Value         = GetPriorityString(message.Priority) },
                new Metadata { Key = "HasAttachments", Value   = message.HasAttachments.ToString() },
                new Metadata { Key = "Cc", Value = message.Cc },
                new Metadata { Key = "Bcc", Value = message.Bcc },
                new Metadata { Key = "Importance", Value = GetImportanceString(message.Importance) },
                new Metadata { Key = "Date", Value = GetDateString(message.Date) },
                new Metadata { Key = "IsDraft", Value = message.IsDraft.ToString() },
                new Metadata { Key = "IsRead", Value = message.IsRead.ToString() },
                new Metadata { Key = "Subject", Value = message.Subject },
                new Metadata { Key = "Sensitivity", Value = GetSensitivityString(message.Sensitivity)}


            };
            if (message.HasAttachments)
            {
                List<string> attachmentNames = new List<string>();
                int numAttachments = 0;
                foreach (var attachment in message.Attachments)
                {
                    if (attachment.IsFile)
                    {
                        attachmentNames.Add(attachment.FileName);
                    }
                    if (attachment.IsEmail)
                    {
                        attachmentNames.Add("<Attached Message>" + attachment.AttachedEmailMessage ?? "Untitled");
                    }
                    numAttachments++;
                }
                result.Add(new Metadata { Key = "Num Attachments", Value = "" + numAttachments });
                result.Add(new Metadata { Key = "Attachments", Value = string.Join(", ", attachmentNames) });
            }
            return result;
        }

        private string GetDateString(DateTime? date)
        {
            if (date == null) return "Unknown/Unset";
            else return date.ToString();
        }

        private string GetSensitivityString(MessageSensitivity? sensitivity)
        {
            switch(sensitivity)
            {
                case MessageSensitivity.Normal:
                    return "Normal";
                case MessageSensitivity.Personal:
                    return "Personal";
                case MessageSensitivity.Private:
                    return "Private";
                case MessageSensitivity.Confidential:
                    return "Confidential";
                default:
                    return "Unknown/Unset";
            }
        }

        private string GetImportanceString(MessageImportance? messageImportance)
        {
            switch (messageImportance)
            {
                case MessageImportance.LowImportance:
                    return "Low";
                case MessageImportance.NormalImportance:
                    return "Normal";
                case MessageImportance.HightImportance:
                    return "High";
                default:
                    return "Unknown/Unset";
            }
        }

        private string GetPriorityString(MessagePriority? messagePriority)
        {
            switch (messagePriority)
            {
                case MessagePriority.Normal:
                    return "Normal";
                case MessagePriority.Urgent:
                    return "Urgent";
                case MessagePriority.NotUrgent:
                    return "Not Urgent";
                default:
                    return "Unknown/Unset";
            }
        }

        private List<Metadata> GetAllFolderMetadata(XstFolder folder)
        {
            List<Metadata> result = new List<Metadata>
            {
                GetDisplayNameMetadata(folder),
                GetPathMetadata(folder),
                GetMessageCountMetadata(folder),
                GetUnreadCountMetadata(folder),
                GetParentFolderMetadata(folder),
                GetIsNotRealFileMarkerMetadata()
            };
            return result;
        }

        private Metadata GetIsNotRealFileMarkerMetadata()
        {
            return new Metadata { Key = "_eSearch_virtual_document", Value = "1" }; // Indicates the document is not a real file on the filesystem, but rather part of the file or from other source.
        }

        private Metadata GetDisplayNameMetadata(XstElement element)
        {
            return new Metadata { Key = "DisplayName", Value = element.DisplayName };
        }

        private Metadata GetPathMetadata(XstFolder folder)
        {
            List<string> folderNames = new List<string>();
            folderNames.Prepend(folder.DisplayName);

            XstFolder parentFolder = folder.ParentFolder;
            while (parentFolder != null)
            {
                folderNames.Prepend(parentFolder.DisplayName);
                parentFolder = parentFolder.ParentFolder;
            }
            string folderPath = String.Join(">", folderNames.ToArray());
            return new Metadata { Key = "Location", Value = folderPath };
        }

        private Metadata GetMessageCountMetadata(XstFolder folder)
        {
            int messages = folder.ContentCount;
            return new Metadata { Key = "Message Count", Value = "" + messages };
        }

        private Metadata GetUnreadCountMetadata(XstFolder folder)
        {
            int unreadMessages = folder.ContentUnreadCount;
            return new Metadata { Key = "Unread Messages", Value = "" +unreadMessages };
        }

        private Metadata GetParentFolderMetadata(XstFolder folder)
        {
            if (folder.ParentFolder != null)
            {
                return new Metadata { Key = "Parent Folder", Value = folder.ParentFolder.DisplayName };
            }
            else
            {
                return new Metadata { Key = "Parent Folder", Value = "N/A - This is the root folder" };
            }
        }

        private InMemoryDocument BaseParseResult(string type, string filePath)
        {
            InMemoryDocument result = new InMemoryDocument
            {
                Parser = "PSTParser (XstReader)",
                FileType = type,
                FileName = filePath
            };
            return result;
        }
    }
}
