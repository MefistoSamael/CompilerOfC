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

        private HashSet<char> validEscapeCharacters = new HashSet<char> { 'r', 'n', 't' };

        bool firstWhiteSpace = true;


        private ReaderState state = ReaderState.InitialState;

        public Reader(string FilePath)
        {
            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName, FilePath);
            reader = new StreamReader(path);
        }

        public char NextCharacter()
        {
            char[] nextSymbol = new char[1];

            while (reader.Read(nextSymbol, 0, 1) != 0)
            {
                switch (state)
                {
                    #region Initial State
                    case ReaderState.InitialState:
                        {
                            if (String.IsNullOrWhiteSpace(nextSymbol[0].ToString()))
                                continue;

                            switch (nextSymbol[0])
                            {
                                case '/':
                                    {
                                        state = ReaderState.PossibleComment;
                                        break;
                                    }
                                case '\"':
                                    {
                                        state = ReaderState.StringLiteral;
                                        return nextSymbol[0];
                                    }
                                case '\'':
                                    {
                                        state = ReaderState.StringLiteral;
                                        return nextSymbol[0];
                                    }
                                default:
                                    {
                                        return nextSymbol[0];
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region Possible Comment
                    case ReaderState.PossibleComment:
                        {
                            switch (nextSymbol[0])
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
                                        throw new Exception($"Invalid token /{nextSymbol[0]}");
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region Multi line comment
                    case ReaderState.MultiLineComment:
                        {
                            switch (nextSymbol[0])
                            {
                                case '*':
                                    {
                                        reader.Read(nextSymbol, 0, 1);

                                        if (nextSymbol[0] == '/')
                                            state = ReaderState.InitialState;
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
                            switch (nextSymbol[0])
                            {
                                case '\\':
                                    {
                                        state = ReaderState.EscapeCharacter;
                                        return nextSymbol[0];
                                    }
                                case '\"':
                                    {
                                        state = ReaderState.InitialState;
                                        return nextSymbol[0];
                                    }
                                case '\'':
                                    {
                                        state = ReaderState.InitialState;
                                        return nextSymbol[0];
                                    }
                                case '\r':
                                    {
                                        state = ReaderState.PossibleEndLine;
                                        return nextSymbol[0];
                                    }
                                default:
                                    {
                                        return nextSymbol[0];
                                    }
                            }
                            break;
                        }
                    #endregion

                    #region escaping character
                    case ReaderState.EscapeCharacter:
                        {
                            if (validEscapeCharacters.Contains(nextSymbol[0]))
                            {
                                state = ReaderState.StringLiteral;
                                return nextSymbol[0];
                            }
                            else
                            {
                                throw new Exception($"invalid escape character \\{nextSymbol[0]}");
                            }
                        }
                    #endregion

                    #region Possible end of line
                    case ReaderState.PossibleEndLine:
                        {
                            if (nextSymbol[0] == '\n')
                            {
                                throw new Exception("End of line in string literal");
                            }
                            else
                            {
                                state = ReaderState.StringLiteral;
                                return nextSymbol[0];
                            }
                        }
                        #endregion
                }
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
            EscapeCharacter,
            PossibleEndLine,
            WhiteSpace
        }
    }

}
