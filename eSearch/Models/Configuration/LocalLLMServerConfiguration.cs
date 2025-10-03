using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class LocalLLMServerConfiguration
    {
        /// <summary>
        /// The port that the local llm server will bind to
        /// </summary>
        public int Port = 5000;
        /// <summary>
        /// If this is true, eSearch was previously set to run local server
        /// at next launch the server will be automatically started if this is set true.
        /// </summary>
        public bool Running = false;
    }
}
