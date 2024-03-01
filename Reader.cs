using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLexer
{
    public class Reader
    {
        private StreamReader reader;

        char current;

        char next;

        private ReaderState state = ReaderState.InitialState;

        public Reader(string FilePath)
        {
            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName, FilePath);
            reader = new StreamReader(path);


            char[] nextSymbol = new char[1];

            reader.Read(nextSymbol, 0, 1);

            current = nextSymbol[0];
        }

        public char NextCharacter()
        {
            char[] nextSymbol = new char[1];

            while (reader.Read(nextSymbol, 0, 1) != 0)
            {
                next = nextSymbol[0];

                switch (state)
                {
                    #region Initial State
                    case ReaderState.InitialState:
                        {
                            switch (current)
                            {
                                case '/':
                                    {
                                        if (next != '/' && next != '*')
                                        {
                                            return DoReturn();
                                        }
                                        state = ReaderState.PossibleComment;
                                        break;
                                    }
                                case '\"':
                                    {
                                        state = ReaderState.StringLiteral;
                                        var val = current; current = next; return val;
                                    }
                                default:
                                    {
                                        return DoReturn();
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region Possible Comment
                    case ReaderState.PossibleComment:
                        {
                            switch (current)
                            {
                                case '/':
                                    {
                                        state = ReaderState.InitialState;
                                        reader.ReadLine();
                                        break;
                                    }
                                case '*':
                                    {
                                        state = ReaderState.MultiLineComment;
                                        break;
                                    }
                                default:
                                    {
                                        state = ReaderState.InitialState;
                                        return DoReturn();
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region Multi line comment
                    case ReaderState.MultiLineComment:
                        {
                            switch (current)
                            {
                                case '*':
                                    {
                                        if (next == '/')
                                            state = ReaderState.InitialState;
                                        
                                        // надо чтобы убрать символ комментария
                                        next = ' ';
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region string or char literal
                    case ReaderState.StringLiteral:
                        {
                            switch (current)
                            {
                                case '\\':
                                    {
                                        state = ReaderState.EscapeCharacter; 
                                        return DoReturn();
                                    }
                                case '\"':
                                    {
                                        state = ReaderState.InitialState;
                                        return DoReturn();
                                    }
                                case '\r':
                                    {
                                        state = ReaderState.PossibleEndLine;
                                        return DoReturn();
                                    }
                                default:
                                    {
                                        return DoReturn();
                                    }
                            }
                        }
                    #endregion

                    #region escaping character
                    case ReaderState.EscapeCharacter:
                        {
                                state = ReaderState.StringLiteral;
                                return DoReturn();
                        }
                    #endregion

                    #region Possible end of line
                    case ReaderState.PossibleEndLine:
                        {
                            if (current == '\n')
                            {
                                throw new Exception("End of line in string literal");
                            }
                            else
                            {
                                state = ReaderState.StringLiteral;
                                return DoReturn();
                            }
                        }
                        #endregion
                }

                current = next;
            }

            if (state != ReaderState.InitialState)
                throw new Exception("End of file at bad place");
            else
                return '\0';
        }

        private enum ReaderState
        {
            PossibleComment,
            MultiLineComment,
            InitialState,
            StringLiteral,
            PossibleEndLine,
            WhiteSpace,
            EscapeCharacter
        }

        private char DoReturn()
        {
            var val = current;
            current = next;
            return val;
        }
    }

}
