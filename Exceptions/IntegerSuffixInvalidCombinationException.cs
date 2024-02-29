using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class IntegerSuffixInvalidCombinationException : Exception
    {
        public IntegerSuffixInvalidCombinationException()
        { }

        public IntegerSuffixInvalidCombinationException(string message)
            : base(message)
        { }

        public IntegerSuffixInvalidCombinationException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
