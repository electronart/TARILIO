
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopSearch2.Views;
using eSearch.Models.Localization;
using ReactiveUI;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace eSearch.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        public ViewModelBase() {

        }

        [JsonIgnore]
        public TranslationsViewModel S
        {
            get {
                return Program.TranslationsViewModel;
            }
            //set => this.RaiseAndSetIfChanged(ref _translator, value);
        }

        [JsonIgnore]
        public DarkModeIconsViewModel I
        {
            get
            {
                return Program.DarkModeIconsViewModel;
            }
        }

        [JsonIgnore]
        public string ApplicationName
        {
            get
            {
                return "eSearch";
            }
        }

        [JsonIgnore]
        public WindowIcon ApplicationIcon
        {
            get
            {
                var assemblyName = typeof(Program).Assembly.GetName().Name;
                var bitmap = new Bitmap(AssetLoader.Open(new System.Uri("avares://" + assemblyName + "/Assets/esearch-icon.ico")));
                WindowIcon windowIcon = new WindowIcon(bitmap);
                return windowIcon;
            }
        }

        [JsonIgnore]
        public bool ProgramIsRunningInSearchOnlyMode
        {
            get
            {
#if TARILIO
                return Program.WasLaunchedWithSearchOnlyArgument 
                    || TARILIO.ProductSerials.isValidSerial(Program.ProgramConfig.Serial, out var yearMonth) == TARILIO.ProductSerials.SerialValidationResult.SearchOnly;
#else
                return Program.WasLaunchedWithSearchOnlyArgument;
#endif

            }
        }
    }
}