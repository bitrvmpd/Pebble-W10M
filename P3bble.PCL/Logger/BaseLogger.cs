using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P3bble.PCL.Logger
{
    public class BaseLogger
    {
        private string FileName { get; set; }

        public virtual void WriteLine(string message) {
            Debug.WriteLine(message);


            //if (string.IsNullOrEmpty(FileName))
            //{
            //    FileName = string.Format("Log-{0:yyyy-MM-dd-HH-mm-ss}.txt", DateTime.Now);
            //    Debug.WriteLine("Logger initialised; writing to " + FileName);
            //}

            //lock (FileName)
            //{
            //    if (IsEnabled)
            //    {
            //        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            //        {
            //            using (StreamWriter sw = new StreamWriter(store.OpenFile(FileName, FileMode.Append, FileAccess.Write)))
            //            {
            //                sw.Write(message + "\n");
            //            }
            //        }
            //    }
            //    else
            //    {
            //        // This is a HORRIBLE hack!
            //        // There is something strange happening in message sequencing that goes
            //        // away when logging to a file - so when RELEASE mode with logging OFF,
            //        // this is my only alternative for now. I am a bad person.
            //        // For Windows Universal you can't use Thread.Sleep()
            //        // Fix http://stackoverflow.com/a/16374324
            //        using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
            //        {
            //            tmpEvent.WaitOne(TimeSpan.FromSeconds(10));
            //        }
            //   }
            //}
        }
        public virtual void ClearUp() { }

        /// <summary>
        /// Gets or Sets a value indicating whether loggins is enabled
        /// </summary>
        /// <value>
        /// <c>true</c> if logging is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }
    }
}
