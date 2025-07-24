using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableBuilder
{
    public class FolderCopy
    {
        public FolderCopy(string SourceDir, string TargetDir)
        {
            SourceDirectory = SourceDir;
            TargetDirectory = TargetDir;
        }

        public string SourceDirectory;
        public string TargetDirectory;
    }
}
