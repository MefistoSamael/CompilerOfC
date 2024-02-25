Reader reader = new Reader("File.txt");

while (true)
{
    try
    {
        Console.Write(reader.NextCharacter());
    }
    catch (EndOfStreamException ex)
    {
        Console.WriteLine("\nEnf of file");
        break;
    }
}

public class Token
{
    public TokenType Type;

    public string Value;

}

public class Lexer 
{
    public char character;

    public int currentLine;

    public char nextCharacter;

    private TokenType possibleTokenType;
    private string possibleValue = string.Empty;

    private StreamReader reader;

    public Lexer(string FilePath)
    {
        string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName, FilePath);
        reader = new StreamReader(path);
    }

    public Token NextToken()
    {
        throw new NotImplementedException();
    }
}

public class Reader
{
    private StreamReader reader;

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
                    if(!String.IsNullOrWhiteSpace(nextSymbol[0].ToString()))
                        firstWhiteSpace = true;

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
                            // если пробел встречен первый раз - вернуть его
                            // иначе - ничего не возвращать
                            if (String.IsNullOrWhiteSpace(nextSymbol[0].ToString()))
                            {
                                if (firstWhiteSpace)
                                {
                                    firstWhiteSpace = false;
                                }
                                else
                                {
                                    break;
                                }
                            }

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
            throw new EndOfStreamException();
    }
}

public enum TokenType
{
    keyword,
    identifier,
    constant,
    string_literal,
    punctuator,
    header_name,
    c_operator
}