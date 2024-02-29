using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.AlarmExceptions
{
    public class UnreachableCodeReachedException : Exception
    {
        public UnreachableCodeReachedException()
        { }

        public UnreachableCodeReachedException(string message)
            : base(message)
        { }

        public UnreachableCodeReachedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
