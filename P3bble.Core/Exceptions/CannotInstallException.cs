using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3bble.Core.Exceptions
{
    /// <summary>
    /// Raised when trying to install an app when there is no space
    /// </summary>
    public class CannotInstallException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CannotInstallException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CannotInstallException(string message)
            :base(message)
        {

        }
    }
}
