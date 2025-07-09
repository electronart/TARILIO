using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models
{
    public class ValidationError
    {
        public ValidationError(string propertyName, string errorMessage)
        {
            this.PropertyName = propertyName;
            this.ErrorMsg     = errorMessage;
        }


        public readonly string PropertyName;
        public readonly string ErrorMsg;
    }
}
