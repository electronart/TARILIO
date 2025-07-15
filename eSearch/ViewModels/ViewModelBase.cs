
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
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
                var bitmap = ProductIcon;
                WindowIcon windowIcon = new WindowIcon(bitmap);
                return windowIcon;
            }
        }

        public Avalonia.Media.Imaging.Bitmap ProductIcon {

            get {
                var assemblyName = typeof(Program).Assembly.GetName().Name;
#if TARILIO
                var bitmap = new Bitmap(AssetLoader.Open(new System.Uri("avares://" + assemblyName + "/Assets/tarilio-icon.ico")));
#else
                var bitmap = new Bitmap(AssetLoader.Open(new System.Uri("avares://" + assemblyName + "/Assets/esearch-icon.ico")));
#endif
                return bitmap;
            }
        }

        public SolidColorBrush ApplicationBrandPrimaryColor
        {
            get
            {
#if TARILIO
                var color = Color.Parse("#ff612a");
                return new SolidColorBrush(color);
#else
// eSearch
                var color = Color.Parse("#529136");
                return new SolidColorBrush(color);
#endif
            }
        }

        public SolidColorBrush ApplicationBrandTextColor
        {
            get
            {
                return new SolidColorBrush(Color.Parse("#fff"));
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