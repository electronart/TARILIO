using DesktopSearch2.Views;
using eSearch.Models;
using eSearch.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {

        public string ProductTag
        {
            get
            {
                return Program.ProgramConfig.GetProductTagText();
            }
        }

        public string ApplicationVersion
        {
            get
            {
                string version = Program.GetProgramVersion();
                return version;
            }
        }

        public string CopyrightString
        {
            get
            {
                string templateStr = "© 2024 ElectronArt Design Ltd";
                return templateStr;
            }
        }

        public bool IsSerialButtonVisible
        {
            get
            {
#if LITE
                return false;
#endif
#if TARILIO
                return true;
#endif
                return false;
            }
        }

        public string ExpiryString
        {
            get
            {
#if LITE
                   return "";
#endif
#if TARILIO
                if (string.IsNullOrEmpty(Program.ProgramConfig.Serial))
                {
                    if (Program.ProgramConfig.IsProgramInEvaluationPeriod(out int daysRemaining))
                    {
                        return String.Format( S.Get("Evaluation ({0} days remaining)"), daysRemaining);
                    }
                    return "";
                }

                var status = TARILIO.ProductSerials.isValidSerial(Program.ProgramConfig.Serial, out string year);

                if (status == TARILIO.ProductSerials.SerialValidationResult.SearchOnly)
                {
                    return S.Get("Does not expire");
                }

                if (status == TARILIO.ProductSerials.SerialValidationResult.Valid)
                {
                    if (year != null)
                    {
                        return String.Format(
                            S.Get("Expires {0}"),
                            year
                        );
                    }
                }
                if (status == TARILIO.ProductSerials.SerialValidationResult.ExpiredSerial)
                {
                    if (year != null)
                    {
                        return String.Format(
                            S.Get("Expired {0}"),
                            year
                        );
                    }
                }
                return "";
#else
                // Open Source Build.
                return string.Empty;
#endif
            }
        }

        public async void ClickSerial()
        {
            var dialogRes = await RegistrationWindow.ShowDialog();
            if (dialogRes == TaskDialogResult.OK)
            {
                //this.RaisePropertyChanged(nameof(ShowRegistrationPromptPanel));
                this.RaisePropertyChanged(nameof(ExpiryString));
                this.RaisePropertyChanged(nameof(DisplayedSerial));
                if (Program.GetMainWindow().DataContext is MainWindowViewModel vm)
                {
                    vm.RaisePropertyChanged(nameof(vm.ProductTagText));
                    vm.RaisePropertyChanged(nameof(ViewModelBase.ProgramIsRunningInSearchOnlyMode));
                }
                #region If any of the queries are limited to 10 results, raise it to 100 now.
                if (Program.GetMainWindow().DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.Session.Query.LimitResultsEndAt = Program.DEFAULT_MAX_RESULTS_REGISTERED;
                }
                #endregion
            }
        }

        public async void ClickOnlineHelp()
        {
            string url = Program.OnlineHelpLinkLocation;
            eSearch.Models.Utils.CrossPlatformOpenBrowser(url);
        }


        public string DisplayedSerial
        {
            get
            {
#if LITE
                return "";
#endif
#if TARILIO
                if (!Program.ProgramConfig.IsProgramRegistered())
                {
                    return S.Get("Serial: None");
                } else
                {
                    return Program.ProgramConfig.Serial;
                }
#else
                // eSearch Pro Open Source
                return string.Empty;
#endif
            }
        }

        public async void ClickLicensesAndAcknowledgements()
        {
            // TODO - I need the licenses.html

            string exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string licenseFilePath = Path.Combine(exeDir, "docs", "license.htm");
            if (File.Exists(licenseFilePath))
            {
                var uri = new System.Uri(licenseFilePath);
                var url = uri.AbsoluteUri;
                Models.Utils.CrossPlatformOpenBrowser(url);
            }
            else
            {
                TaskDialogWindow.OKDialog(S.Get("File not found"), "Expected Location: " + licenseFilePath, Program.GetMainWindow());
            }
        }



        
    }
}
