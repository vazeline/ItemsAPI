using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common.Exceptions
{
    /// <summary>
    /// Denotes an exception whose message is considered "friendly" to consumers - the exact exception message can be returned.
    /// </summary>
    public class ConsumerFriendlyException : Exception
    {
        public ConsumerFriendlyException()
        {
        }

        public ConsumerFriendlyException(string message)
            : base(message)
        {
        }

        public ConsumerFriendlyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ConsumerFriendlyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
