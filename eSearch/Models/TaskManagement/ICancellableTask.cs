using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.TaskManagement
{
    public interface ICancellableTask
    {

        /// <summary>
        /// The message to be shown to the user when they click cancel to confirm they wish to cancel.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public void GetCancelConfirmationPrompt(out string title, out string message);

        public void Pause();

        public void Resume();

        /// <summary>
        /// Send a signal to the task that it should cancel early
        /// </summary>
        public void RequestCancel();

        /// <summary>
        /// Indicated whether the task received a cancellation request.
        /// </summary>
        /// <returns></returns>
        public bool HasReceivedCancelRequest();

    }
}
