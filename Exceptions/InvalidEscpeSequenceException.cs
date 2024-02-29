using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidEscpeSequenceException : Exception
    {
        public InvalidEscpeSequenceException()
        { }

        public InvalidEscpeSequenceException(string message)
            : base(message)
        { }

        public InvalidEscpeSequenceException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
