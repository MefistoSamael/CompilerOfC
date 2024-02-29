using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class InvalidEncodingPrefixBeforeCharConstant : Exception
    {
        public InvalidEncodingPrefixBeforeCharConstant()
        { }

        public InvalidEncodingPrefixBeforeCharConstant(string message)
            : base(message)
        { }

        public InvalidEncodingPrefixBeforeCharConstant(string message, Exception innerException)
            : base(message, innerException)
        { }        
    }
}
