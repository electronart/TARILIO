using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public class WindowsShortcutHelper
    {
        /// <summary>
        /// Create a shortcut
        /// </summary>
        /// <param name="shortCutFileName">Use lnk extension. ie. "C:/My Shortcut.lnk"</param>
        /// <param name="shortCutToLocation">Where the shortcut links to</param>
        /// 
        public static void CreateShortcut(string shortCutFileName, string shortCutToLocation)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortCutFileName);
            shortcut.TargetPath = shortCutToLocation; // Path to the executable
            shortcut.Save();
        }
    }
}
