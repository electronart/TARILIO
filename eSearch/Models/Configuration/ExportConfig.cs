using eSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Models.Configuration
{
    public class ExportConfig
    {
        public string?          OutputFileName = null;

        public int              OutputTypeIndex = -1;

        public string?          OutputDirectory = null;

        public bool             AppendDate = false;

        public bool             AllColumns = true;

        public List<string>?    SelectedColumns = null;


        public static ExportConfig FromExportResultsVM(ExportSearchResultsViewModel vm)
        {
            var cfg = new ExportConfig();
            cfg.OutputFileName = vm.FileNameInput;
            cfg.OutputTypeIndex = vm.SelectedOutputFileTypeIndex;
            cfg.OutputDirectory = vm.OutputDirectoryInput;
            cfg.AppendDate = vm.AppendDateChecked;
            cfg.AllColumns = vm.ExportAllColumns;
            cfg.SelectedColumns = new List<string>();
            foreach(var column in vm.SelectedColumnsModel.SelectedItems)
            {
                cfg.SelectedColumns.Add(column.Header);
            }
            return cfg;
        }
    }
}
