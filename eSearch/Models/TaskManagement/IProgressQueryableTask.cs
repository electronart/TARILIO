using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.TaskManagement
{
    public interface IProgressQueryableTask
    {
        public int GetProgress();

        public int GetMaxProgress();

        public string GetStatusString();

    }
}
