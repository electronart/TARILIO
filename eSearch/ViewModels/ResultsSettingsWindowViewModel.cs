using CsvHelper.Configuration.Attributes;
using eSearch.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    public class ResultsSettingsWindowViewModel : ViewModelBase
    {
        public ResultsSettingsWindowViewModel() {
            AvailableColumns.Add(new CheckBoxItemViewModel
            {
                Header = "Example 1",
                IsChecked = true
            });
            AvailableColumns.Add(new CheckBoxItemViewModel
            {
                Header = "Example 2",
                IsChecked = false
            });
            AvailableColumns.Add(new CheckBoxItemViewModel
            {
                Header = "Example 3",
                IsChecked = false
            });
            AvailableColumns.Add(new CheckBoxItemViewModel
            {
                Header = "Example 4",
                IsChecked = true
            });
            AvailableColumns.Add(new CheckBoxItemViewModel
            {
                Header = "Example 5",
                IsChecked = false
            });

        }

        public ResultsSettingsWindowViewModel(DataColumn[] dataColumns)
        {
            AvailableColumns.Clear();

            foreach(var column in dataColumns
                                    .OrderByDescending(c => c.Visible)
                                    .ThenBy(c => c.Visible ? c.DisplayIndex : int.MaxValue)
                                    .ThenBy(c => c.Visible ? string.Empty : c.Header))
                                    // The sort here puts visible columns at the top ordered by display index
                                    // hidden columns are put at the bottom ordered alphabetically.
            {
                AvailableColumns.Add(new CheckBoxItemViewModel
                {
                    Header = column.Header,
                    IsChecked = column.Visible
                });
            }
        }

        public ObservableCollection<CheckBoxItemViewModel> AvailableColumns
        {
            get
            {
                return _availableColumns;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _availableColumns, value);
            }
        }

        private ObservableCollection<CheckBoxItemViewModel> _availableColumns = new ObservableCollection<CheckBoxItemViewModel>();

        public enum ColumnWidthOption
        {
            WidthContent,
            WidthWindow,
            SetManually
        }

        public ColumnWidthOption SelectedColumnSizingMode
        {
            get
            {
                if (_selectedColumnWidthOption == null)
                {
                    _selectedColumnWidthOption = ColumnWidthOption.WidthContent;
                }
                return (ColumnWidthOption)_selectedColumnWidthOption;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedColumnWidthOption, value);
                this.RaisePropertyChanged(nameof(IsRadioColumnWidthFitToContentChecked));
                this.RaisePropertyChanged(nameof(IsRadioColumnWidthFitToWindowChecked));
                this.RaisePropertyChanged(nameof(IsRadioColumnWidthSetManuallyChecked));
            }
        }

        private ColumnWidthOption? _selectedColumnWidthOption = null;


        public bool IsRadioColumnWidthFitToContentChecked
        {
            get
            {
                if (_isRadioColumnWidthFitToContentChecked == null)
                {
                    _isRadioColumnWidthFitToContentChecked = SelectedColumnSizingMode == ColumnWidthOption.WidthContent;
                }
                return (bool)_isRadioColumnWidthFitToContentChecked;
            }
            set
            {
                if (value == true)
                {
                    SelectedColumnSizingMode = ColumnWidthOption.WidthContent;
                }
                this.RaiseAndSetIfChanged(ref _isRadioColumnWidthFitToContentChecked, value);
            }
        }

        private bool? _isRadioColumnWidthFitToContentChecked = null;

        public bool IsRadioColumnWidthFitToWindowChecked
        {
            get
            {
                if (_isRadioColumnWidthFitToWindowChecked == null)
                {
                    _isRadioColumnWidthFitToWindowChecked = SelectedColumnSizingMode == ColumnWidthOption.WidthWindow;
                }
                return (bool)_isRadioColumnWidthFitToWindowChecked;
            }
            set
            {
                if (value == true)
                {
                    SelectedColumnSizingMode = ColumnWidthOption.WidthWindow;
                }
                this.RaiseAndSetIfChanged(ref _isRadioColumnWidthFitToWindowChecked, value);
            }
        }

        private bool? _isRadioColumnWidthFitToWindowChecked = null;

        public bool IsRadioColumnWidthSetManuallyChecked
        {
            get
            {
                if (_isRadioColumnWidthSetManuallyChecked == null)
                {
                    _isRadioColumnWidthSetManuallyChecked = SelectedColumnSizingMode == ColumnWidthOption.SetManually;
                }
                return (bool)_isRadioColumnWidthSetManuallyChecked;
            }
            set
            {
                if (value == true)
                {
                    SelectedColumnSizingMode = ColumnWidthOption.SetManually;
                }
                this.RaiseAndSetIfChanged(ref _isRadioColumnWidthSetManuallyChecked, value);
                _isRadioColumnWidthSetManuallyChecked = value;
            }
        }

        private bool? _isRadioColumnWidthSetManuallyChecked = null;


        public bool IsLimitResultsChecked
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered()) return true; // Always checked if unregistered.
                if (_isLimitResultsChecked == null)
                {

                    _isLimitResultsChecked = false;
                }

                return (bool)_isLimitResultsChecked;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isLimitResultsChecked, value);
                this.RaisePropertyChanged(nameof(LimitResultsStartAt));
                this.RaisePropertyChanged(nameof(LimitResultsEndAt));
                this.RaisePropertyChanged(nameof(IsLimitResultsStartAtControlEnabled));
                this.RaisePropertyChanged(nameof(IsLimitResultsEndAtControlEnabled));
            }
        }

        private bool? _isLimitResultsChecked = null;

        public bool IsLimitResultsAvailable
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }

        public bool IsLimitResultsStartAtControlEnabled
        {
            get
            {
                return IsLimitResultsAvailable && IsLimitResultsChecked;
            }
        }

        public bool IsLimitResultsEndAtControlEnabled
        {
            get
            {
                return IsLimitResultsAvailable && IsLimitResultsChecked;
            }
        }

        public int LimitResultsStartAt
        {
            get
            {
                if (_limitResultsStartAt == null)
                {
                    _limitResultsStartAt = 1;
                }
                return (int)_limitResultsStartAt;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _limitResultsStartAt, value);
            }
        }

        private int? _limitResultsStartAt = null;

        public int? LimitResultsEndAt
        {
            get
            {
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return 10;
                }

                if (!IsLimitResultsChecked)
                {
                    return null;
                }

                if (_limitResultsEndAt == null)
                {
                    _limitResultsEndAt = 100;
                }

                return (int)_limitResultsEndAt;
            } set
            {
                this.RaiseAndSetIfChanged(ref _limitResultsEndAt, value);
            }
        }

        private int? _limitResultsEndAt = null;


    }
}
