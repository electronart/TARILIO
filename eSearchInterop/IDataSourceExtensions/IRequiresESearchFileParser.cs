using System;
using System.Collections.Generic;
using System.Text;

namespace eSearch.Interop.IDataSourceExtensions
{
    /// <summary>
    /// To be implemented by plugin datasources that require eSearch's file parsing utilities.
    /// </summary>
    public interface IRequiresESearchFileParser : IDataSource
    {
        /// <summary>
        /// eSearch will call this method on IDataSources that implement IRequiresESearchFileParser
        /// Datasources should 
        /// </summary>
        public void SetESearchFileParser(IESearchFileParser eSearchFileParser);
    }
}
