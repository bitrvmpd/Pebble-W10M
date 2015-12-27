using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3bble.Core.Exceptions
{
    /// <summary>
    /// Raised when calling a method on an unconnected Pebble
    /// </summary>
    public class NotConnectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotConnectedException"/> class.
        /// </summary>
        public NotConnectedException()
            : base("You first need to connect to the Pebble")
        {
        }
    }
}
