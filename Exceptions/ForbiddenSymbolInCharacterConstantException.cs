using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class ForbiddenSymbolInCharacterConstantException : Exception
    {
        public ForbiddenSymbolInCharacterConstantException()
        { }

        public ForbiddenSymbolInCharacterConstantException(string message)
            : base(message)
        { }

        public ForbiddenSymbolInCharacterConstantException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
