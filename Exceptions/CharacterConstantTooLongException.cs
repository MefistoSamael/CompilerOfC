using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer.Exceptions
{
    public class CharacterConstantTooLongException : Exception
    {
        public CharacterConstantTooLongException()
        { }

        public CharacterConstantTooLongException(string message)
            : base(message)
        { }

        public CharacterConstantTooLongException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
