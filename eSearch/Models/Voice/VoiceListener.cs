using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Runtime.Versioning;
using org.apache.xmlbeans.impl.xb.xsdschema;

namespace eSearch.Models.Voice
{
    public class VoiceListener
    {
        public event EventHandler<string> OnVoiceInput;
        SpeechRecognitionEngine recognizer;

        [SupportedOSPlatform("windows")]
        public VoiceListener() { }

        [SupportedOSPlatform("windows")]
        public void BeginListening()
        {
            recognizer = new SpeechRecognitionEngine();
            recognizer.LoadGrammar(new DictationGrammar());
            recognizer.SpeechRecognized += (s, e) =>
            {
                OnVoiceInput?.Invoke(this, e.Result.Text);
            };
            recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        private void Recognizer_RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            OnVoiceInput?.Invoke(this, string.Empty);
        }

        [SupportedOSPlatform("windows")]
        public void StopListening()
        {
            recognizer.RecognizeAsyncStop();
        }
    }
}
