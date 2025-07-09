using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DocumentFormat.OpenXml.Linq;
using DynamicData;
using eSearch.Models.AI;
using eSearch.Models.Configuration;
using eSearch.Models.Documents;
using eSearch.Models.Search;
using eSearch.ViewModels;
using OpenXmlPowerTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Web;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Handlers;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class HtmlDocumentControl : UserControl
    {
        private AvaloniaCefBrowser? browser = null;

        JSBindingObj browserJSObject {
            get
            {
                if (_browserJSObject == null)
                {
                    _browserJSObject = new JSBindingObj(this);
                }
                return _browserJSObject;
            }
        }

        JSBindingObj _browserJSObject;


        CustomCSSAndJSProvider BrowserJSAndCSSProvider
        {
            get
            {
                if (_browserJSProvider == null)
                {
                    _browserJSProvider = new CustomCSSAndJSProvider();
                }
                return _browserJSProvider;
            }
        }

        CustomCSSAndJSProvider _browserJSProvider = null;

        /// <summary>
        /// Strided list.
        /// --css-var-name, value, --css-var-name, value...
        /// </summary>
        List<string> CurrentCSSVars = new List<string>();


        public HtmlDocumentControl()
        {
            InitializeComponent();

            InitBrowser();

            RenderBlankPageThemeColored();
        }

        ResultViewModel displayedResult = null;

        public string expectedBrowserAddress = "";

        public void InitBrowser()
        {
            if (browser != null)
            {
                try
                {
                    browserWrapper.Child = null;
                    browser.Dispose();
                } catch (NullReferenceException)
                {
                    // TODO We're swallowing this because CefGlue throws it eroneously
                    // Not a great solution...
                }
                
            }

            browser = new AvaloniaCefBrowser();
            browser.RegisterJavascriptObject(browserJSObject, "Search");
            browser.LoadStart += Browser_LoadStart;
            browser.LoadEnd += Browser_LoadEnd;
            browser.TitleChanged += Browser_TitleChanged;
            browser.FocusHandler = new CustomFocusHandler();
            browser.KeyboardHandler = new CustomKeyboardHandler();
            //browser.RequestHandler = new CustomRequestHandler(this);
            if (Application.Current is App app)
            {
                app.getThemePrimaryColors(out var regionColor, out var baseHighColor, out var altHighColor, out var chromeLow);
                browser.Settings.BackgroundColor = new CefColor(255, chromeLow.R, chromeLow.G, chromeLow.B);
            }
            browserWrapper.Child = browser;
            browserWrapper.Focusable = false;

            
        }


        /// <summary>
        /// Will cause the browser control to automatically resize
        /// </summary>
        /// <param name="enabled"></param>
        public void SetAutomaticControlHeightEnabled(bool enabled)
        {
            _isAutomaticControlHeightEnabled = enabled;
        }

        private bool _isAutomaticControlHeightEnabled = false;

        public class CustomKeyboardHandler : KeyboardHandler
        {
            protected override bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, nint os_event, out bool isKeyboardShortcut)
            {
                if (keyEvent.UnmodifiedCharacter == '/')
                {
                    // Handle the '/' key, e.g., open a custom search bar
                    isKeyboardShortcut = false;
                    if (Program.GetMainWindow() is MainWindow mainWindow)
                    {
                        mainWindow.FocusTextBox();
                    }
                    return true; // Prevent the key from reaching the browser
                }
                return base.OnPreKeyEvent(browser, keyEvent, os_event, out isKeyboardShortcut);
            }
        }

        //protected virtual bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, nint os_event, out bool isKeyboardShortcut)
        //{
        //    isKeyboardShortcut = false;
        //    return false;
        //}
        public void UpdateTheme()
        {
            InitBrowser();
            RenderBlankPageThemeColored();
        }

        private string? gottenSrc = null;

        public string GetCurrentlyRenderedHTML()
        {
            if (browser is BaseCefBrowser baseBrowser)
            {
                #region TODO this is using reflection and a polling hack, very ugly.. :/
                PropertyInfo propertyInfo = baseBrowser.GetType().GetProperty("UnderlyingBrowser", BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    throw new Exception("UnderlyingBrowser property not found.");
                }
                CefBrowser? cefBrowser = (CefBrowser?)propertyInfo.GetValue(baseBrowser);
                #endregion
                gottenSrc = null;
                cefBrowser?.GetMainFrame().GetSource(new SourceVisitor(this));
                while (gottenSrc == null)
                {
                    System.Threading.Thread.Sleep(100);
                }
                return gottenSrc;
            }
            return string.Empty;
        }

        private class SourceVisitor : CefStringVisitor
        {
            private readonly HtmlDocumentControl _parent;

            public SourceVisitor(HtmlDocumentControl parent)
            {
                _parent = parent;
            }

            protected override void Visit(string source)
            {
                _parent.gottenSrc = source;
            }
        }


        public void NavigateToURL(string url)
        {
            SetBrowserAddress(url);

        }

        public void RenderBlankPageThemeColored()
        {

            if (Application.Current is App app)
            {
                app.getThemePrimaryColors(out var bgColor, out var txtColor, out var ctrlColor, out var chromeHigh);
                var cefColor = new CefColor(bgColor.A, bgColor.R, bgColor.G, bgColor.B);
                BrowserJSAndCSSProvider.RenderCustomHtml = "";
                BrowserJSAndCSSProvider.CSSVariables.Clear();
                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-app-region-color");
                BrowserJSAndCSSProvider.CSSVariables.Add(string.Format("{0:X2}{1:X2}{2:X2}", bgColor.R, bgColor.G, bgColor.B));
                browser.Settings.BackgroundColor = cefColor;
                SetBrowserAddress(BlackPageDataURI);
            }
        }

        public void renderBlankPageColored(Avalonia.Media.Color color)
        {
            var cefColor = new CefColor(color.A, color.R, color.G, color.B);

            BrowserJSAndCSSProvider.RenderCustomHtml = "";
            browser.Settings.BackgroundColor = cefColor;
            SetBrowserAddress(BlackPageDataURI);
        }

        public void ShowDevTools()
        {
            browser.ShowDeveloperTools();
        }

        private class CustomFocusHandler : FocusHandler
        {
            protected override void OnGotFocus(CefBrowser browser)
            {
                Debug.WriteLine("Got focus");
                base.OnGotFocus(browser);
            }

            protected override bool OnSetFocus(CefBrowser browser, CefFocusSource source)
            {
                Debug.WriteLine("Source: " + source.ToString());
                return true; // Cancels the focus.
            }

            protected override void OnTakeFocus(CefBrowser browser, bool next)
            {
                base.OnTakeFocus(browser, next);
            }
        }

        public void GoToHit(int hit)
        {
            browser.EvaluateJavaScript<string>("window.go_to_hit(" + hit + ")");
        }

        public void PrintDocument()
        {
            browser.EvaluateJavaScript<string>("window.print()");
        }


        /// <summary>
        /// Data uri to render a black html page.
        /// </summary>
        string BlackPageDataURI
        {
            get
            {
                if (_blackPageDataURI == null)
                {
                    string html = "<html><head></head><body style='background-color: var(--eSearch-app-region-color)'></body></html>";
                    string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
                    _blackPageDataURI = "data:text/html;base64," + base64;
                }
                return _blackPageDataURI;
            }
        }

        string? _blackPageDataURI = null;

        

        public void RenderAILoadingScreen()
        {
            BrowserJSAndCSSProvider.JSFileNames.Clear();
            BrowserJSAndCSSProvider.CSSFileNames.Clear();
            var html = Models.Utils.GetTextAsset("ai_loading_view.html");
            if (Program.GetIsThemeDark())
            {
                BrowserJSAndCSSProvider.CSSFileNames.Add("result_style.css");
                BrowserJSAndCSSProvider.CSSFileNames.Add("result_style_dark.css");
            }
            else
            {
                BrowserJSAndCSSProvider.CSSFileNames.Add("result_style.css");
            }
            RenderHTMLInBrowser(html);
        }

        public void renderResultAccordingToSettings(ResultViewModel result, MainWindowViewModel? mwvm)
        {
            if (mwvm == null) return;
            mwvm.ShowHitNavigation = true;
            displayedResult = result;
            SetBrowserAddress("about:blank");
            BrowserJSAndCSSProvider.JSFileNames.Clear();
            BrowserJSAndCSSProvider.CSSFileNames.Clear();
#if DEBUG
#endif

            var resultFileSize = result.FileSize;
            var maxFileSize = Program.ProgramConfig.ViewerConfig.MaxFileSizeMB * (1e+6); // Convert mb to bytes
            if (resultFileSize > maxFileSize)
            {
                var largeFileSetting = Program.ProgramConfig.ViewerConfig.ViewLargeFileOption;
                switch (largeFileSetting)
                {
                    case DesktopSearch2.Models.Configuration.ViewerConfig.OptionViewLargeFile.Fully:
                        break;
                    case DesktopSearch2.Models.Configuration.ViewerConfig.OptionViewLargeFile.FirstPageOnly:
                        // TODO
                        break;
                    case DesktopSearch2.Models.Configuration.ViewerConfig.OptionViewLargeFile.InReportView:
                        string htmlReport = getSearchReportHtmlBody(result);
                        renderHtmlBody(htmlReport, false);
                        return;
                }
            }

            string extension = Path.GetExtension(result.FileName).ToLower().Substring(1);


            if (extension == "pdf" && File.Exists(result.FilePath))
            {
                switch(Program.ProgramConfig.ViewerConfig.PDFViewerOption)
                {
                    case DesktopSearch2.Models.Configuration.ViewerConfig.OptionPDFViewer.PdfJS:
                        resultAsPDFJS(result);
                        return;
                    case DesktopSearch2.Models.Configuration.ViewerConfig.OptionPDFViewer.Acrobat:
                        resultToAcrobat(result);
                        return;
                    default:
                        break;
                }
            }

            if (extension == "pgm")
            {
                if (File.Exists(result.FilePath))
                {
                    SetBrowserAddress("about:blank");
                    browserJSObject.SetPGMPath(result.FilePath);
                    string contents = Models.Utils.GetTextAsset("pgm_render.html");
                    RenderHTMLInBrowser(contents);
                    mwvm.ShowHitNavigation = false;
                    return;
                }
            }

            if (extension == "tiff" || extension == "tif")
            {
                if (File.Exists(result.FilePath))
                {
                    
                    browserJSObject.SetTiffPath(result.FilePath);
                    BrowserJSAndCSSProvider.JSFileNames.Add("tiff.min.js");
                    BrowserJSAndCSSProvider.JSFileNames.Add("tiff_esearch_render.js");
                    SetBrowserAddress("about:blank");
                    mwvm.ShowHitNavigation = false;
                    return;
                }
            }

            string[] mediaExtensions = new string[] { // TODO This is the list from taglibsharp. This list is duplicated from the TagLibSharp class..
                "mkv", "ogv", "avi", "wmv", "asf", "mp4", "m4v", "mpeg", "mpg", "mpe", "mpv", "mpg", "m2v",  // Video
                "aa", "aax", "aac", "aiff", "ape", "dsf", "flac", "m4a", "m4b", "m4p", "mp3", "mpc", "mpp", "ogg", "oga", "wav", "wma", "wv", "webm", // Audio
                "bmp", "gif", "jpeg", "jpg", "pbm", "ppm", "pnm", "pcx", "png", "tiff", "dng", "svg" // Image
            }; 

            if (mediaExtensions.Contains(extension))
            {
                // Display it as media if possible.
                if (File.Exists(result.FilePath))
                {
                    mwvm.ShowHitNavigation = false;
                    var uri = new System.Uri(result.FilePath);
                    SetBrowserAddress(uri.AbsoluteUri);
                    return;
                }
            }

            #region Determine if the format should be rendered as source code.
            bool isSrcCode = false;
            foreach(var docType in DocumentType.SourceCodeFormats)
            {
                if (docType.Extension == extension)
                {
                    isSrcCode = true;
                    break;
                }
            }
            // Formats that should be rendered using source code renderer but aren't technically source code.
            List<string> extraRenderAsSourceFormats = new List<string> { "json", "jsonl", "xml" };
            if (extraRenderAsSourceFormats.Contains(extension))
            {
                if (extension == "jsonl")
                {
                    // If this is a jsonl record, treat it as source code and change the extension to json.
                    if (!string.IsNullOrEmpty(result.GetMetadataValue("Row"))) // The main file is not json, the subdocuments with Row indices are.
                    {
                        extension = "json";
                        isSrcCode = true;
                    }
                } else
                {
                    isSrcCode = true;
                }
            }

            #endregion
            string html = result.ExtractHtmlRender(isSrcCode);
            

            if (isSrcCode)
            {
                // Escape all html elements.
                html = "<pre data-keep-tags='span'><code class='language-" + extension + " line-numbers' data-keep-tags='span'>" + html + "</code></pre>";
            }

            renderHtmlBody(html, isSrcCode, extension);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlBody"></param>
        /// <param name="isSourceCode">Passing this as true will change the font to a monospace font</param>
        public void renderHtmlBody(string htmlBody, bool isSourceCode, string bodyClassList = "")
        {
            BrowserJSAndCSSProvider.RenderCustomHtml = "";
            SetBrowserAddress("about:blank");
            string prependRaw;
            if (bodyClassList == "")
            {
                prependRaw = "<html><head></head><body>";
            } else
            {
                prependRaw = "<html><head></head><body class=\"" + bodyClassList + "\">";
            }
            //prependRaw += Models.Utils.GetTextAsset("hit_navigator.htm"); disused.
            prependRaw += "<div id='doc-area'>";
            string appendRaw = "</div></body></html>";
            string[] cssFiles;
            if (Program.GetIsThemeDark())
            {
                cssFiles = new string[] { "prism.css", "hit_navigator.css", "result_style.css", "result_style_dark.css" };
            }
            else
            {
                cssFiles = new string[] { "prism.css", "hit_navigator.css", "result_style.css"};
            }

            List<string> jsFiles = [.. new string[] { "prism-keep-highlights.js", "prism.js", "postload_highlighter3.js", "link_handler.js" }];
            if (Program.GetIsThemeDark())
            {
                jsFiles.Add("text_contrast.js");
            }
            htmlBody = Models.Utils.AlterHtmlDoc(htmlBody, prependRaw, appendRaw);
            BrowserJSAndCSSProvider.CSSFileNames = cssFiles.ToList();
            BrowserJSAndCSSProvider.JSFileNames = jsFiles;
            #region Configure CSS Variables
            BrowserJSAndCSSProvider.CSSVariables.Clear();
            // TODO don't like doing string replace on the whole doc, might be a big doc - make a better solution later.
            if (!isSourceCode)
            {
                // For non source code, use the users font preference.
                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-font-family");
                BrowserJSAndCSSProvider.CSSVariables.Add(Program.ProgramConfig.ViewerConfig.FontFamilyName);

            } else
            {
                // For source code, use a monospace font so indents etc display correctly.
                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-font-family");
                BrowserJSAndCSSProvider.CSSVariables.Add("monospace");
            }

            BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-font-size");
            BrowserJSAndCSSProvider.CSSVariables.Add(Program.ProgramConfig.ViewerConfig.FontSizePt + "");

            BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-highlight-color");
            var color = Program.ProgramConfig.ViewerConfig.HitHighlightColor;
            BrowserJSAndCSSProvider.CSSVariables.Add("rgb(" + color.R + "," + color.G + "," + color.B + ")");

            if (Application.Current is App app)
            {
                app.getThemePrimaryColors(out var bgColor, out var txtColor, out var ctrlColor, out var chromeLow);
                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-app-region-color");
                BrowserJSAndCSSProvider.CSSVariables.Add(string.Format("{0:X2}{1:X2}{2:X2}", bgColor.R, bgColor.G, bgColor.B));

                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-app-base-high-color");
                BrowserJSAndCSSProvider.CSSVariables.Add(string.Format("{0:X2}{1:X2}{2:X2}", txtColor.R, txtColor.G, txtColor.B));

                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-app-alt-high-color");
                BrowserJSAndCSSProvider.CSSVariables.Add(string.Format("{0:X2}{1:X2}{2:X2}", ctrlColor.R, ctrlColor.G, ctrlColor.B));

                BrowserJSAndCSSProvider.CSSVariables.Add("--eSearch-app-chrome-low-color");
                BrowserJSAndCSSProvider.CSSVariables.Add(string.Format("{0:X2}{1:X2}{2:X2}", chromeLow.R, chromeLow.G, chromeLow.B));
            }

            #endregion
            RenderHTMLInBrowser(htmlBody);
        }

        public void RenderHTMLInBrowser(string html)
        {
            InitBrowser();

            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("about:blank");
            //urlBuilder.Append(System.Convert.ToBase64String( Encoding.UTF8.GetBytes(html)) );
            string address = urlBuilder.ToString();
            browser.RegisterJavascriptObject(browserJSObject, "Search");// TODO - Workaround for https://github.com/OutSystems/CefGlue/pull/186
            browser.RegisterJavascriptObject(BrowserJSAndCSSProvider, "ExtrasProvider");
            BrowserJSAndCSSProvider.RenderCustomHtml = html;
            expectedBrowserAddress = address;
            browser.Address = address;
            return; // TEMP - testing.

            string tempPath = Path.Combine(Path.GetTempPath(), "ResultRender.html");

            if (File.Exists(tempPath))
            {
                // May throw
                // System.IO.IOException: 'The process cannot access the file 
                // If the user is scrolling back and forth between files fast.
                int attempts = 0;
            retryPoint:
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    if (attempts < 3)
                    {
                        Thread.Sleep(100);
                        ++attempts;
                        goto retryPoint;
                    }
                }
            }

            File.WriteAllText(tempPath, html);
            var uri = new Uri(tempPath).AbsoluteUri;
            SetBrowserAddress(uri);
        }

        public string getSearchReportHtmlBody(ResultViewModel result)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<h1>").Append(S.Get("Search Report")).AppendLine("</h1>");
            sb.Append("<h2>").Append(S.Get("Document Information")).AppendLine("</h2>");
            sb.AppendLine("<p>")
                .AppendLine(result.FilePath)
                .AppendLine("<table>")
                    .AppendLine("<tr>")
                        .Append("<th>").Append(S.Get("Title")).AppendLine("</th>")
                        .Append("<th>").Append(S.Get("Size")).AppendLine("</th>")
                        .Append("<th>").Append(S.Get("Score")).AppendLine("</th>")
                    .AppendLine("</tr>")
                    .AppendLine("<tr>")
                        .Append("<td>").Append(result.Title).AppendLine("</td>")
                        .Append("<td>").Append(result.FileSizeHumanFriendly).AppendLine("</td>")
                        .Append("<td>").Append(result.Score).AppendLine("</td>")
                    .AppendLine("</tr>")
                .AppendLine("</table>")
            .AppendLine("</p>");
            string[] excerpts = result.GetResult().GetContextExcerpts(
                Program.ProgramConfig.ViewerConfig.ReportViewContextAmount,
                Program.ProgramConfig.ViewerConfig.ReportViewContextTypeOption);
            sb.Append("<h2>").Append(S.Get("Hits in Context")).AppendLine("</h2>");
            foreach(string excerpt in excerpts)
            {
                sb.AppendLine("<p>");
                sb.AppendLine(excerpt);
                sb.AppendLine("</p>");
            }
            return sb.ToString();
        }
        public void resultToAcrobat(ResultViewModel result)
        {
            SetBrowserAddress("about:blank");
            if (result == null) return;
            string fileName = result.FilePath;
            if (File.Exists(fileName))
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
        }

        private CompletionStreamingJSBinding? completionStreamingJSBinding = null;

        public void RenderLLMMessage(LLMMessageViewModel message)
        {
            var jsBindingObj = new LLMMessageStreamingJSBinding(message);
            renderHtmlBody(Models.Utils.GetTextAsset("ai_message_streaming_display.html"), false, "ai-message");
            browser.RegisterJavascriptObject(jsBindingObj, "aiStream");

        }

        public void RenderLLMConversation(AISearchConfiguration aiSearchConfig, Conversation existingConversation)
        {
            completionStreamingJSBinding = new CompletionStreamingJSBinding(existingConversation, aiSearchConfig);
            
            browser.UnregisterJavascriptObject("aiStream");
            renderHtmlBody(Models.Utils.GetTextAsset("ai_streaming_display.html"), false);
            browser.RegisterJavascriptObject(completionStreamingJSBinding, "aiStream");
            
            foreach (var message in existingConversation.Messages)
            {
                var role    = JavaScriptEncoder.Default.Encode(message.Role);
                var content = JavaScriptEncoder.Default.Encode(message.Content);
                browser.ExecuteJavaScript(
                    $"window.addMessage('{role}','{content}')"
                );
            }
        }

        public void AddQueryToExistingLLMConversation(string query)
        {
            browser.ExecuteJavaScript($"addMessage('user',{query})");
            browser.ExecuteJavaScript("addMessage('assistant','')");
            browser.ExecuteJavaScript("");
        }

        public void PerformAndRenderStreamingAICompletion(AISearchConfiguration aiSearchConfig, string startText, CancellationToken cancellationToken)
        {
            completionStreamingJSBinding = new CompletionStreamingJSBinding(startText, aiSearchConfig);
            renderHtmlBody(Models.Utils.GetTextAsset("ai_streaming_display.html"), false);
            browser.RegisterJavascriptObject(completionStreamingJSBinding, "aiStream");
        }

        public void resultAsPDFJS(ResultViewModel result)
        {
            if (Program.GetMainWindow().DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowHitNavigation = false;
            }
            Debug.WriteLine("File name " + result.FilePath);
            
            string viewerLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            viewerLocation = Path.Combine(viewerLocation, "pdf_viewer", "pdfViewer", "web");
            viewerLocation = Path.Combine(viewerLocation, "viewer.html");
            string fileURI = new System.Uri(result.FilePath).AbsoluteUri;
            Debug.WriteLine("File URI: " + fileURI);
            viewerLocation = viewerLocation + "?file=" + HttpUtility.UrlEncode(fileURI);
            //viewerLocation = viewerLocation + "?file=" + HttpUtility.UrlEncode("compressed.tracemonkey-pldi-09.pdf");
            Debug.WriteLine("Viewing " + viewerLocation);
            SetBrowserAddress(viewerLocation);
            //browser.ShowDeveloperTools();
            
        }

        public void SetBrowserAddress(string browserAddress)
        {
            BrowserJSAndCSSProvider.RenderCustomHtml = "";
            InitBrowser();
            browser.RegisterJavascriptObject(browserJSObject, "Search");// TODO - Workaround for https://github.com/OutSystems/CefGlue/pull/186
            browser.RegisterJavascriptObject(BrowserJSAndCSSProvider, "ExtrasProvider");
            expectedBrowserAddress = browserAddress;
            browser.Address = browserAddress;
            
        }

        public void clearResult()
        {
            // TODO
        }

        private void Browser_TitleChanged(object sender, string title)
        {
            //throw new System.NotImplementedException();
        }

        private void Browser_LoadEnd(object sender, Xilium.CefGlue.Common.Events.LoadEndEventArgs e)
        {
            //throw new System.NotImplementedException();
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                browser.IsVisible = true;
            });
            string jsScript = Models.Utils.GetTextAsset("browser_init.js");
            browser.ExecuteJavaScript(jsScript);
        }

        private void Browser_LoadStart(object sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
        {
            //throw new System.NotImplementedException();.
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                browser.IsVisible = false;
            });
            
        }

        // https://stackoverflow.com/questions/28067549/how-to-trap-listen-javascript-function-or-events-in-cefsharp
        public class JSBindingObj
        {

            public JSBindingObj(HtmlDocumentControl htmlDocControl)
            {
                this.htmlDocControl = htmlDocControl;
            }

            private HtmlDocumentControl htmlDocControl;


            int prevBrowserHeight = -1;

            /// <summary>
            /// Invoked when the Page Height Changes...
            /// Currently, does not handle shrinking..
            /// </summary>
            /// <param name="height"></param>
            public async void PageHeightUpdate(int newHeight)
            {
                if (newHeight > (prevBrowserHeight + 5) || newHeight < (prevBrowserHeight - 5))
                {
                    
                    if (htmlDocControl._isAutomaticControlHeightEnabled)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            prevBrowserHeight = newHeight;
                            htmlDocControl.Height = newHeight;
                        });
                    }
                }
            }

            public void ReceiveData(string data)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (Program.GetMainWindow().DataContext is MainWindowViewModel mainWindowVM)
                    {

                        string[] parameters = data.Split(',');
                        if (parameters.Length == 2)
                        {
                            if (parameters[0] == "numhits")
                            {
                                mainWindowVM.CurrentDocHitCount = int.Parse(parameters[1]);
                            }
                            if (parameters[0] == "curhit")
                            {
                                mainWindowVM.CurrentDocSelectedHit = int.Parse(parameters[1]);
                            }
                        }

#if DEBUG
                        Debug.WriteLine(data);
#endif
                    }
                });
            }


            public string GetPGMText()
            {
                try
                {
                    return File.ReadAllText(_pgmPath);
                } catch (Exception ex)
                {

                    return "";
                }
            }

            public string GetPGMAsBase64()
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(_pgmPath);
                    return Convert.ToBase64String(fileBytes);
                } catch (Exception ex)
                {
                    return "";
                }
            }


            public void OpenExternalBrowser(string url)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Ensures it opens in the system's default browser
                });
            }



            private string _tiffPath = "";

            public void SetPGMPath(string path)
            {
                _pgmPath = path;
            }

            string _pgmPath = "";

            public string GetPGMPath()
            {
                return _pgmPath;
            }


            public void SetTiffPath(string path)
            {
                _tiffPath = path;
            }

            public string GetTiffAsBase64()
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(_tiffPath);
                    return Convert.ToBase64String(fileBytes);
                }
                catch (Exception ex)
                {
                    return "";
                }
            }

            

            public void CancelAISearchRequest()
            {
                if (Program.GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.CancelCurrentAISearchReq();
                }
            }

            public void RequestHighlightAreas(string text)
            {
                if (htmlDocControl.displayedResult != null)
                {
                    var ranges = htmlDocControl.displayedResult.GetHighlightRanges(text);
                    foreach(var range in ranges)
                    {
                        htmlDocControl.browser.EvaluateJavaScript<string>($"eSearchHighlightTextInDOM({range.Start},{range.End})");
                    }

                    htmlDocControl.browser.EvaluateJavaScript<string>("eSearchUpdateHitCount()");
                }
            }

            public string HitHighlight(string textToHighlight)
            {
                if (htmlDocControl.displayedResult != null)
                {
                    string highlightedText = htmlDocControl.displayedResult.HighlightTextAccordingToResultQuery(textToHighlight);
                    return highlightedText;
                }
                return textToHighlight;
            }
        }

        public class CustomCSSAndJSProvider
        {
            /// <summary>
            /// When not an empty string, this is the html source that should be rendered.
            /// </summary>
            public string RenderCustomHtml = "";

            public string GetCustomRenderHtml()
            {
                return RenderCustomHtml;
            }

            // List of CSS File names as found in the Assets Directory.
            public List<string> CSSFileNames = new List<string>();

            // List of JS File names as found in the Assets Directory.
            public List<string> JSFileNames = new List<string>();

            // Strided list.
            // --css-variable-name, value, --css-variable-name, value...
            public List<string> CSSVariables = new List<string>();

            public int GetTotalCSSRules()
            {
                return CSSFileNames.Count;
            }

            public string GetCSS(int i)
            {
                var css_file =      CSSFileNames[i];
                string contents =   Models.Utils.GetTextAsset(css_file);
                return contents;
            }

            public int GetTotalJSRules()
            {
                return JSFileNames.Count;
            }

            public string GetJS(int i)
            {
                var js_file = JSFileNames[i];
                string contents = Models.Utils.GetTextAsset(js_file);
                return contents;
            }

            /// <summary>
            /// Strided list.
            /// --css-variable-name, value, --css-variable-name, value...
            /// </summary>
            /// <returns></returns>
            public string[] GetExtraCSSVariables()
            {
                return CSSVariables.ToArray();
            }
        }

        public class CustomRequestHandler : RequestHandler
        {

            private HtmlDocumentControl _htmlDocControl;
            public CustomRequestHandler(HtmlDocumentControl htmlDocumentControl)
            {
                _htmlDocControl = htmlDocumentControl;
            }

            protected override bool OnBeforeBrowse(CefBrowser browser, CefFrame frame, CefRequest request, bool userGesture, bool isRedirect)
            {
                var url = request.Url;
                Debug.WriteLine("Detecting Navigation to URL: " + url + " expected url " + _htmlDocControl.expectedBrowserAddress);
                if (ShouldOpenExternally(url))
                {
                    OpenExternalBrowser(url);
                    return true;
                }
                return false;
            }

            private bool ShouldOpenExternally(string url)
            {
                if (!url.StartsWith(_htmlDocControl.expectedBrowserAddress)) return true;
                return false;
            }

            private void OpenExternalBrowser(string url)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Ensures it opens in the system's default browser
                });
            }

            protected override CefResourceRequestHandler GetResourceRequestHandler(CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                // Required by interface to implement this method but returning null will use default handler.
                return null;
            }
        }

    }

    
}
