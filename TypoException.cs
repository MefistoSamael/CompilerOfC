using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer
{
    public class TypoException : Exception
    {
        public TypoException()
        { }

        public TypoException(string message)
            : base(message)
        { }

        public TypoException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
