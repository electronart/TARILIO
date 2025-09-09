using Avalonia.Data.Core;
using Avalonia.Data;
using Avalonia.Platform;
using Avalonia.Data.Converters;
using Avalonia.Data;
using DocumentFormat.OpenXml.Office.MetaAttributes;
using HtmlAgilityPack;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System.Reactive;
using Avalonia.Data.Core.Plugins;
using com.googlecode.mp4parser.boxes.apple;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel;
using sun.tools.tree;
using System.Runtime.Versioning;

namespace eSearch.Models
{
    public static class Utils
    {
        /// <summary>
        /// SO 24769701
        /// Utility to trim whitespace from end of a StringBuilder.
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            int i = sb.Length - 1;

            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }

        public static bool IsObsolete(this Enum value)
        {
            var enumType = value.GetType();
            var enumName = enumType.GetEnumName(value);
            var fieldInfo = enumType.GetField(enumName);
            return Attribute.IsDefined(fieldInfo, typeof(ObsoleteAttribute));
        }

        public static string GetDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();

            return descriptionAttribute?.Description ?? value.ToString();
        }

        public static bool MatchesWildcard(this string input, string pattern)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$");
        }

        public static void CrossPlatformOpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }


        #region SO 3577802 Get Property by binding path.
        /// <summary>
        /// Get value from an object by a binding path.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object GetValueByPath(object obj, string path)
        {
            //throw new NotImplementedException("Bugged in this build, sorry!");

            return eSearch.Utils.TypeHelper.GetNestedPropertyValue(obj, path);

        }

        public static string GetValueByPathAsString(object obj, string path)
        {
            var value = GetValueByPath(obj, path);
            if (value == null) return "null";
            return value.ToString() ?? "null";
        }



        public static void InsertSorted<T, TKey>(this List<T> list, T item, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
        {
            int index = list.FindIndex(x => keySelector(x).CompareTo(keySelector(item)) > 0);
            if (index == -1) // If no such element is found, append to the end
                list.Add(item);
            else
                list.Insert(index, item);
        }

        #endregion

        public static string GetChecksum(string theString)
        {
            string hash;
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                hash = BitConverter.ToString(
                  md5.ComputeHash(Encoding.UTF8.GetBytes(theString))
                ).Replace("-", String.Empty);
            }

            return hash;
        }

        /// <summary>
        /// Should not include the extension or path. Just the file name itself... IE if the path is C:/Documents/MyImg.png, this method expects you to supply "MyImg" and nothing else.
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns>String with all invalid file name characters removed.</returns>
        public static string SanitizeFileName(string userInput)
        {
            if (string.IsNullOrEmpty(userInput)) throw new NotSupportedException("Input was null or empty");
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            return new string(userInput.Where(c => !invalidFileNameChars.Contains(c)).ToArray());
        }

        /// <summary>
        /// May return null on unsupported OS.
        /// Currently Windows Only.
        /// </summary>
        /// <returns></returns>
        public static string? GetOSUserInfo()
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.Name;
            }
            return null;
        }


        public static string ToTotalSeconds(this TimeSpan t)
        {
            return $@"{t:%s} seconds";
        }

        public static string FileSizeHumanFriendly(long FileSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0} {1}", len, sizes[order]);
        }


        /// <summary>
        /// Attempt to get the owner of a given file. May return an empty string on failure.
        /// Failure may be for various reasons including unsupported OS, lack of permissions etc.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileOwner(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return "";
                FileSecurity fileSecurity = new FileSecurity(path, AccessControlSections.Owner);
                IdentityReference sid = fileSecurity.GetOwner(typeof(SecurityIdentifier));
                NTAccount ntAccount = sid.Translate(typeof(NTAccount)) as NTAccount;
                string owner = ntAccount.Value;
                if ( owner != null )
                {
                    return owner;
                } else
                {
                    return "";
                }
            } catch (Exception ex)
            {
                Debug.WriteLine("Error retrieving file owner");
                Debug.WriteLine(ex.ToString());
                return "";
            }
        }

        /// <summary>
        /// Cross platform method of revealing a file on the filesystem.
        /// 
        /// </summary>
        /// <param name="path"></param>
        public static async void RevealInFolderCrossPlatform(string path)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using Process fileOpener = new Process();
                    fileOpener.StartInfo.FileName = "explorer";
                    fileOpener.StartInfo.Arguments = "/select," + path + "\"";
                    fileOpener.Start();
                    await fileOpener.WaitForExitAsync();
                    return;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    using Process fileOpener = new Process();
                    fileOpener.StartInfo.FileName = "explorer";
                    fileOpener.StartInfo.Arguments = "-R " + path;
                    fileOpener.Start();
                    await fileOpener.WaitForExitAsync();
                    return;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using Process dbusShowItemsProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dbus-send",
                            Arguments = "--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"file://" + path + "\" string:\"\"",
                            UseShellExecute = true
                        }
                    };
                    dbusShowItemsProcess.Start();
                    await dbusShowItemsProcess.WaitForExitAsync();

                    if (dbusShowItemsProcess.ExitCode == 0)
                    {
                        // The dbus invocation can fail for a variety of reasons:
                        // - dbus is not available
                        // - no programs implement the service,
                        // - ...
                        return;
                    }
                }

                using Process folderOpener = new Process();
                folderOpener.StartInfo.FileName = Path.GetDirectoryName(path);
                folderOpener.StartInfo.UseShellExecute = true;
                folderOpener.Start();
                await folderOpener.WaitForExitAsync();

            } catch (Exception ex)
            {
                // TODO Error handling
                throw;
            }
        }

        public static bool IsOnlyRunningCopyOfESearch()
        {
            int count = 0;
            var processes = System.Diagnostics.Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.ProcessName.Contains("eSearch"))
                {
                    ++count;
                }
                if (count > 1) break;
            }
            return count == 1;
        }

        public static string GetJavaExePath()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string javaPath = Path.Combine(exePath, "openjdk-22.0.1_windows-x64_bin\\jdk-22.0.1\\bin\\java.exe");
            return javaPath;
        }

        public static CultureInfo GetPreferredCulture(out bool isError)
        {
            isError = false;
            string language_tag = S.Get("RFC4646");         //LANGTOOL Setting|en-GB
            try
            {
                if (language_tag != "RFC4646")
                {
                    var cutlure = new CultureInfo(language_tag);
                    return cutlure;
                }
            } catch (CultureNotFoundException)
            {
                isError = true;
            }
            var culture = CultureInfo.CurrentCulture;
            return culture;
        }

        public static bool IsJavaInstalledAndCorrectVersion(out string errorTitle, out string errorMsg)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = GetJavaExePath();
            psi.Arguments = " -version";
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;

            Process pr = Process.Start(psi);
            string strOutput = pr.StandardError.ReadLine().Split(' ')[2].Replace("\"", "");
            Debug.WriteLine("Java Version \"" + strOutput + "\"");
            errorTitle = "";
            errorMsg = "";
            return true;
        }

        public static string AlterHtmlDoc(string result_html, string prepend_raw = "", string append_raw = "")
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(prepend_raw)) sb.Append(prepend_raw);
            sb.Append(result_html);
            if (!string.IsNullOrEmpty(append_raw)) sb.Append(append_raw);
            return sb.ToString();
        }


        // SO 3825390
        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <param name="assumeUTF8">Assume UTF-8 instead of ASCII if encoding not found</param>
        /// <returns>The detected encoding.</returns>
        public static Encoding GetEncoding(string filename, bool assumeUTF8 = false)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII

            if (Has1252BytePatterns(filename))
            {
                return Encoding.GetEncoding(1252);
            }

            var res = UtfUnknown.CharsetDetector.DetectFromFile(filename);
            var resDetected = res.Detected;
            if (resDetected.Confidence > 0)
            {
                return resDetected.Encoding;
            }

            if (assumeUTF8) return Encoding.UTF8;
            return Encoding.ASCII;
        }


        private static bool Has1252BytePatterns(string filename)
        {
            byte[] fileBytes = File.ReadAllBytes(filename);

            foreach (byte b in fileBytes)
            {
                if (b >= 128 && b <= 159)
                {
                    // The file likely contains Windows-1252 specific byte values.
                    return true;
                }
            }
            return false;
        }

        public static string GetTextAsset(string asset_filename, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string uri = "avares://" + assemblyName + "/Assets/" + asset_filename;
            Stream asset = AssetLoader.Open(new System.Uri(uri));
            string content = new StreamReader(asset,encoding).ReadToEnd();
            return content;
            
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        #region Path Searching

        /// <summary>
        /// Finds executables on PATH similar to shell
        /// </summary>
        /// <param name="executableName"></param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        public static string FindOnPath(string executableName)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new NotSupportedException("Currently Windows OS Only.");
            }
            StringBuilder buffer = new StringBuilder(260);
            IntPtr filePart;
            uint result = SearchPath(
            null,
            executableName,
            null,
            buffer.Capacity,
            buffer,
            out filePart);

            if (result > 0)
            {
                Console.WriteLine($"The full path of '{executableName}' is: {buffer.ToString()}");
                return buffer.ToString();
            }
            else
            {
                Console.WriteLine($"Failed to find '{executableName}' in the PATH.");
                return string.Empty;
            }
        }

        // Windows API!
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [SupportedOSPlatform("windows")]
        private static extern uint SearchPath(
            string lpPath,
            string lpFileName,
            string lpExtension,
            int nBufferLength,
            [Out] StringBuilder lpBuffer,
            out IntPtr lpFilePart
        );
        #endregion


        /// <summary>
        /// Static class that provides functions to convert HTML to plain text.
        /// SO 4182594
        /// </summary>
        public static class HtmlToText
        {

            #region Method: ConvertFromFile (public - static)
            /// <summary>
            /// Converts the HTML content from a given file path to plain text.
            /// </summary>
            /// <param name="path">The path to the HTML file.</param>
            /// <returns>The plain text version of the HTML content.</returns>
            public static string ConvertFromFile(string path)
            {
                var doc = new HtmlDocument();

                // Load the HTML file
                doc.Load(path);

                using (var sw = new StringWriter())
                {
                    // Convert the HTML document to plain text
                    ConvertTo(node: doc.DocumentNode,
                              outText: sw,
                              counters: new Dictionary<HtmlNode, int>());
                    sw.Flush();
                    return sw.ToString();
                }
            }
            #endregion

            #region Method: ConvertFromString (public - static)
            /// <summary>
            /// Converts the given HTML string to plain text.
            /// </summary>
            /// <param name="html">The HTML content as a string.</param>
            /// <returns>The plain text version of the HTML content.</returns>
            public static string ConvertFromString(string html)
            {
                var doc = new HtmlDocument();

                // Load the HTML string
                doc.LoadHtml(html);

                using (var sw = new StringWriter())
                {
                    // Convert the HTML string to plain text
                    ConvertTo(node: doc.DocumentNode,
                              outText: sw,
                              counters: new Dictionary<HtmlNode, int>());
                    sw.Flush();
                    return sw.ToString();
                }
            }
            #endregion

            #region Method: ConvertTo (static)
            /// <summary>
            /// Helper method to convert each child node of the given node to text.
            /// </summary>
            /// <param name="node">The HTML node to convert.</param>
            /// <param name="outText">The writer to output the text to.</param>
            /// <param name="counters">Keep track of the ol/li counters during conversion</param>
            private static void ConvertContentTo(HtmlNode node, TextWriter outText, Dictionary<HtmlNode, int> counters)
            {
                // Convert each child node to text
                foreach (var subnode in node.ChildNodes)
                {
                    ConvertTo(subnode, outText, counters);
                }
            }
            #endregion

            #region Method: ConvertTo (public - static)
            /// <summary>
            /// Converts the given HTML node to plain text.
            /// </summary>
            /// <param name="node">The HTML node to convert.</param>
            /// <param name="outText">The writer to output the text to.</param>
            public static void ConvertTo(HtmlNode node, TextWriter outText, Dictionary<HtmlNode, int> counters)
            {
                string html;

                switch (node.NodeType)
                {
                    case HtmlNodeType.Comment:
                        // Don't output comments
                        break;
                    case HtmlNodeType.Document:
                        // Convert entire content of document node to text
                        ConvertContentTo(node, outText, counters);
                        break;
                    case HtmlNodeType.Text:
                        // Ignore script and style nodes
                        var parentName = node.ParentNode.Name;
                        if ((parentName == "script") || (parentName == "style"))
                        {
                            break;
                        }

                        // Get text from the text node
                        html = ((HtmlTextNode)node).Text;

                        // Ignore special closing nodes output as text
                        if (HtmlNode.IsOverlappedClosingElement(html) || string.IsNullOrWhiteSpace(html))
                        {
                            break;
                        }

                        // Write meaningful text (not just white-spaces) to the output
                        outText.Write(HtmlEntity.DeEntitize(html));
                        break;
                    case HtmlNodeType.Element:
                        switch (node.Name.ToLowerInvariant())
                        {
                            case "p":
                            case "div":
                            case "br":
                            case "table":
                                // Treat paragraphs and divs as new lines
                                outText.Write("\n");
                                break;
                            case "li":
                                // Treat list items as dash-prefixed lines
                                if (node.ParentNode.Name == "ol")
                                {
                                    if (!counters.ContainsKey(node.ParentNode))
                                    {
                                        counters[node.ParentNode] = 0;
                                    }
                                    counters[node.ParentNode]++;
                                    outText.Write("\n" + counters[node.ParentNode] + ". ");
                                }
                                else
                                {
                                    outText.Write("\n- ");
                                }
                                break;
                            case "a":
                                // convert hyperlinks to include the URL in parenthesis
                                if (node.HasChildNodes)
                                {
                                    ConvertContentTo(node, outText, counters);
                                }
                                if (node.Attributes["href"] != null)
                                {
                                    outText.Write($" ({node.Attributes["href"].Value})");
                                }
                                break;
                            case "th":
                            case "td":
                                outText.Write(" | ");
                                break;
                        }

                        // Convert child nodes to text if they exist (ignore a href children as they are already handled)
                        if (node.Name.ToLowerInvariant() != "a" && node.HasChildNodes)
                        {
                            ConvertContentTo(node: node,
                                             outText: outText,
                                             counters: counters);
                        }
                        break;
                }
            }
            #endregion

            

        }
    }
}
