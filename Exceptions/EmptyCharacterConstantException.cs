using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class EmptyCharacterConstantException : Exception
    {
        public EmptyCharacterConstantException()
        { }

        public EmptyCharacterConstantException(string message)
            : base(message)
        { }

        public EmptyCharacterConstantException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
