using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Configuration
{
    public class PhoneticConfig
    {
        public enum Encoder
        {
            None,
            Soundex,
            DoubleMetaphone
        }

        public Encoder SelectedEncoder = PhoneticConfig.Encoder.None;
    }
}
