using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidPreprocessorDirectiveException : Exception
    {
        public InvalidPreprocessorDirectiveException()
        { }

        public InvalidPreprocessorDirectiveException(string message)
            : base(message)
        { }

        public InvalidPreprocessorDirectiveException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
