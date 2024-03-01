using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidSymbolAfterSignException : Exception
    {
        public InvalidSymbolAfterSignException()
        { }

        public InvalidSymbolAfterSignException(string message)
            : base(message)
        { }

        public InvalidSymbolAfterSignException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
