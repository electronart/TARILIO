using eSearch.Interop;
using eSearch.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.DataSources
{
    public interface ISupportsIndexConfigurationDataSource : IDataSource
    {
        /// <summary>
        /// Pass the Index Configuration the datasource should use.
        /// This will affect things like files omitted by extension when the datasource builds list of files.
        /// </summary>
        /// <param name="config"></param>
        void UseIndexConfig(IIndexConfiguration config);
    }
}
