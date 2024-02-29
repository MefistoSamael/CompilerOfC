using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidDecimalNumberException : Exception
    {
        public InvalidDecimalNumberException()
        { }

        public InvalidDecimalNumberException(string message)
            : base(message)
        { }

        public InvalidDecimalNumberException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
