using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class LocalLLMConfiguration
    {
        public string  ModelPath;
        public uint    ContextSize = 4096;
        public uint    Seed = 1;
    }
}
