﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using P3bble.Core.Messages;

namespace P3bble.Core.Exceptions
{
    /// <summary>
    /// Raised when there's a protocol error
    /// </summary>
    public class ProtocolException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolException"/> class.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        internal ProtocolException(LogsMessage logMessage)
            : base(logMessage.Message)
        {
            this.LogMessage = logMessage;
        }

        internal LogsMessage LogMessage { get; set; }
    }
}
