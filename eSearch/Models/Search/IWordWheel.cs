using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static eSearch.Models.Search.LuceneWordWheel;

namespace eSearch.Models.Search
{
    public interface IWordWheel
    {
        /// <summary>
        /// disused in favor of getbestmatchindex - synchronous, can always just call it from background thread.
        /// Set the character sequence a string must begin with for it to appear inside wordwheel.
        /// Once set, await AvailableWordsChanged event.
        /// </summary>
        /// <param name="sequence"></param>
        //public void SetStartSequence(string sequence);
        /// <summary>
        /// Get the total number of unique words in the word wheel.
        /// </summary>
        /// <returns></returns>
        public int GetTotalWords();
        /// <summary>
        /// Get the word at index i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public WheelWord GetWheelWord(int i);
        /// <summary>
        /// Get the index of the word that best matches the start sequence.
        /// </summary>
        /// <param name="startSequence"></param>
        /// <returns></returns>
        public int GetBestMatchIndex(string startSequence);

        /// <summary>
        /// Begin loading world wheel async
        /// </summary>
        public Task BeginLoad();

        /// <summary>
        /// Occurs when the loaded words changed.
        /// </summary>
        public event EventHandler AvailableWordsChanged;

        /// <summary>
        /// Set whether word wheel will show terms from the main content of the document only (exclude metadata fields)
        /// Defaults to true.
        /// </summary>
        /// <param name="contentOnly"></param>
        public void SetContentOnly(bool contentOnly);

    }
}
