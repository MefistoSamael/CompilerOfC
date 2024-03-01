using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidHeaderNameException : Exception
    {
        public InvalidHeaderNameException()
        { }

        public InvalidHeaderNameException(string message)
            : base(message)
        { }

        public InvalidHeaderNameException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
