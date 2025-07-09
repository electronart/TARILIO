using Avalonia.Controls.Primitives;
using eSearch.Models.Search;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Models.Search.LuceneWordWheel;

namespace eSearch.ViewModels
{
    public class WheelViewModel : ViewModelBase
    {
        IWordWheel? _wordWheel;

        private string debugIdentifier = string.Empty;


        public ObservableCollection<WheelWord> WheelWords
        {
            get
            {
                if (_wheelWords == null)
                {
                    _wheelWords = new ObservableCollection<WheelWord>();
                    if (_wordWheel != null)
                    {
                        // TODO I don't like the way this is done, moving stuff around in memory like this...
                        int i = 0;
                        int len = _wordWheel.GetTotalWords();
                        while ( i < len)
                        {
                            _wheelWords.Add(_wordWheel.GetWheelWord(i));
                            ++i;
                        }
                    }
                }
                return _wheelWords;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _wheelWords, value);
            }
        }

        public void SetContentOnly(bool contentOnly)
        {
            _wordWheel?.SetContentOnly(contentOnly);
        }

        public bool HideFrequency
        {
            get
            {
                return _hideFrequency;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _hideFrequency, value);
            }
        }

        private bool _hideFrequency = false;

        private ObservableCollection<WheelWord>? _wheelWords = null;
        
        public int SelectedItemIndex
        {
            get { return _selectedItemIndex; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedItemIndex, value);
                this.RaisePropertyChanged(nameof(SelectedItemIndex));
            }
        }
        int _selectedItemIndex;

        public WheelViewModel(IWordWheel? wordWheel) {
            if (wordWheel != null)
            {
                wordWheel.AvailableWordsChanged += WordWheel_AvailableWordsChanged;
            }
            _wordWheel = wordWheel;
            debugIdentifier = Guid.NewGuid().ToString();
        }

        public void SetWordWheel(IWordWheel? newWheel)
        {
            if (_wordWheel != null)
            {
                _wordWheel.AvailableWordsChanged -= WordWheel_AvailableWordsChanged; // Must deregister the event to allow the old wheel to be disposed.
            }
            if (newWheel != null)
            {
                newWheel.AvailableWordsChanged += WordWheel_AvailableWordsChanged;
            }
            _wordWheel = newWheel;
            populateViewModelWheel();
        }

        private void populateViewModelWheel()
        {
            WheelWords.Clear();
            if (_wordWheel != null)
            {
                int i = 0;
                int len = _wordWheel.GetTotalWords();
                while (i < len)
                {
                    WheelWords.Add(_wordWheel.GetWheelWord(i));
                    ++i;
                }
            }
        }

        private void WordWheel_AvailableWordsChanged(object? sender, EventArgs e)
        {
            Debug.WriteLine("WordWheel_AvailableWordsChanged .... " + WheelWords.Count);
            populateViewModelWheel();
            
        }

        /// <summary>
        /// This constructor is purely for the editor - Don't use this in code.
        /// </summary>
        public WheelViewModel()
        {
            _wordWheel = new PlaceholderWordWheel();
        }

        public void SetNewStartSequence(string sequence)
        {
            if (_wordWheel != null)
            {
                int idx = _wordWheel.GetBestMatchIndex(sequence);
                this.RaisePropertyChanged(nameof(WheelWords));
                SelectedItemIndex = idx;
            }

        }


    }
}
