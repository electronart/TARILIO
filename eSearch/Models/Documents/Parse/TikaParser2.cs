
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace eSearch.Models.Documents.Parse
{
    /// <summary>
    /// This version uses Apache Tika jar file with command line arguments.
    /// Don't use - was just for debugging, decided to use TikaServer but leaving this here for reference.
    /// </summary>
    internal class TikaParser2 : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            string jarFileName = "tika-app-2.9.0.jar";
            string exePath      = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string jarPath      = Path.Combine(exePath, jarFileName);
            string javaPath = Utils.GetJavaExePath();


            string arguments = "-jar \"" + jarPath + "\" \"" + filePath + "\"";
            System.Diagnostics.Process launchJar = new System.Diagnostics.Process();
            launchJar.StartInfo.FileName = javaPath;
            launchJar.StartInfo.Arguments = arguments;
            launchJar.StartInfo.UseShellExecute = false;
            launchJar.StartInfo.RedirectStandardOutput = true;
            launchJar.StartInfo.CreateNoWindow = true;
            launchJar.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            launchJar.Start();
            
            launchJar.WaitForExit(30 * 1000);
            var output = launchJar.StandardOutput.ReadToEnd();
            Debug.WriteLine("TIKA Standard output");
            Debug.WriteLine(output);
            throw new Exception("Terminate early temp");

            /*
            parseResult = new();
            parseResult.ParserName = "tikaParser (Java)";
            TextExtractor extractor = new TextExtractor();
            var res = extractor.Extract(filePath);
            parseResult.TextContent = res.Text;
            parseResult.Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
            */
        }
    }
}
