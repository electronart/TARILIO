using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.CustomControls
{
    public class ClearableTextBox : TextBox, IStyleable
    {
        public event EventHandler? Cleared;

        Type IStyleable.StyleKey => typeof(TextBox);

        public ClearableTextBox()
        {

        }



        public new void Cleary()
        {
            base.Clear();
            Cleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
