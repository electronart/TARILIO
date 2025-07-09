using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace eSearch.Interop
{
    /// <summary>
    /// Manager for a plugin DataSource. There should be one of these for each IDataSource within the plugin.
    /// </summary>
    public interface IPluginDataSourceManager
    {
        /// <summary>
        /// Should return 
        /// </summary>
        /// <returns></returns>
        public string GetDataSourceName();

        /// <summary>
        /// Path to a PNG icon for the DataSource. Ideally 48 by 48 with Alpha.
        /// Displayed on the add data source button when creating an index.
        /// </summary>
        /// <returns></returns>
        public string GetDataSourceIconPath();

        /// <summary>
        /// This method should invoke a UI in the plugin to configure a datasource.
        /// The plugin is in charge of creating this datasource and saving the configuration.
        /// 
        /// When eSearch invokes the configurator,
        /// eSearch will await this task, then call GetConfiguredDataSources() which it will use to refresh the list of
        /// sources displayed to the user.
        /// 
        /// </summary>
        /// <param name="indexID">
        /// The Index ID this data source is being created for. The plugin if it creates a datasource should associate the source
        /// with this Index ID for later retrieval.
        /// </param>
        /// <param name="dataSource">
        /// May be null.
        /// If not null, an existing dataSource being edited, otherwise, assume the user is creating a new datasource.
        /// </param>
        public Task InvokeDataSourceConfigurator(string indexID, IDataSource? dataSource);

        /// <summary>
        /// Get any configured data sources for the given Index.
        /// </summary>
        /// <param name="indexID"></param>
        /// <returns></returns>
        public IEnumerable<IDataSource> GetConfiguredDataSources(string indexID);

        public void RemoveDataSource(IDataSource dataSource);


    }
}
