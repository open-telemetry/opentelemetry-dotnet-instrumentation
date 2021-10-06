using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.AppSec
{
    /// <summary>
    /// This exception should only be used to signal that we want to attempt write 403
    /// and the blocking page to the response streazm
    /// </summary>
    internal class BlockActionException : Exception
    {
        public BlockActionException()
        {
        }

        public BlockActionException(string message)
            : base(message)
        {
        }

        public BlockActionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BlockActionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
