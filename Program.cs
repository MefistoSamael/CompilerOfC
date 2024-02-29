using CLexer;
using System.Globalization;
using System.Xml;

Lexer lexer = new Lexer("File.txt");
while (true)
{
    try
    {
        Console.WriteLine(lexer.NextToken());
    }
    catch (EndOfStreamException e ) 
    {
        Console.WriteLine("\nend\n");
        return;
    }
}

public class Token
{
    public TokenType Type;

    public string Value;

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    public override string ToString()
    {
        return $"{Value} --- {Type}";
    }
}

public class Lexer 
{
    private LexerState state = LexerState.InitialState;

    private char nextCharacter;

    private TokenType possibleTokenType;

    private string value = string.Empty;

    private string possibleKeyWord = string.Empty;

    private string possibleOperator = string.Empty;

    private bool endOfFile = false;

    private Reader reader;

    public Lexer(string FilePath)
    {
        reader = new Reader(FilePath);
        nextCharacter = reader.NextCharacter();
    }

    public Token NextToken()
    {
        while (true)
        {
            switch (state)
            {
                #region Initial State
                case LexerState.InitialState:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            throw new NotImplementedException();
                        }
                        else if (punctuators.Contains(nextCharacter.ToString()))
                        {
                            state = LexerState.Punctuator;
                        }
                        else if (encodingPrefixes.Contains(nextCharacter))
                        {
                            state = LexerState.Encoding_Prefix;
                        }
                        else if (IsLetter(nextCharacter) || nextCharacter == '_')
                        {
                            state = LexerState.Identifier_Or_Keyword;
                        }
                        break;
                    }
                #endregion

                #region Recognition of identifier or keyword
                case LexerState.Identifier_Or_Keyword:
                    {
                        if (IsLetter(nextCharacter) || IsDigit(nextCharacter) || nextCharacter == '_' )
                        {
                            break;
                        }

                        if (keywords.Contains(value))
                        {
                            possibleTokenType = TokenType.keyword;
                            return GetToken();
                        }

                        string? simillarKeyWord = SimillarKeyWord(value);
                        if (simillarKeyWord is not null)
                            throw new TypoException($"возможно вы имелли ввиду {simillarKeyWord}. Вы ввели  {value}");
                        else
                        {
                            possibleTokenType = TokenType.identifier;
                            return GetToken();
                        }
                    }
                #endregion

                #region Recognition of punctuator
                case LexerState.Punctuator:
                    {
                        if (!punctuators.Contains(value + nextCharacter))
                        {
                            possibleTokenType = TokenType.punctuator;
                            return GetToken();
                        }

                        if (punctuators.Contains(nextCharacter.ToString()))
                            break;

                        if (punctuators.Contains(value))
                        {
                            possibleTokenType = TokenType.punctuator;
                            return GetToken();
                        }

                        string? simillarPunctuator = SimillarPunctuator(value);
                        if (simillarPunctuator is not null)
                            throw new TypoException($"возможно вы имелли ввиду {simillarPunctuator}. Вы ввели  {value}");
                        else
                        {
                            throw new KeyNotFoundException($"неправильный пунктуатор: {value}");
                        }
                    }
                    #endregion


            }


            if (nextCharacter == '\0')
                throw new EndOfStreamException();

            value += nextCharacter;
            nextCharacter = reader.NextCharacter();
        }
        
    }

    /// <summary>
    /// Возвращает токен полученный из лексеммы
    /// </summary>
    /// <returns>Токен, полученный в результате анализа</returns>
    /// <remarks>
    /// Обнуляет состояние и переменную содержимиого. Возвращает полученный токен
    /// </remarks>
    private Token GetToken()
    {
        var returnValue = value;
        var returnType = possibleTokenType;

        state = LexerState.InitialState;
        value = string.Empty;
        return new Token(returnType, returnValue);
    }

    /// <summary>
    /// Проверяет похожа ли переданная строка на ключевое слово
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Похожее ключевое слово, если есть. Иначе - null</returns>
    /// <remarks>
    /// Строик похожи - если отличаются одним символом (не учтен случай разных символов в конце)
    /// </remarks>
    private string? SimillarKeyWord(string value)
    {
        foreach (var word in keywords) 
        {
            if (Math.Abs(word.Length - value.Length) > 1)
                continue;

            int differenceAmount = 0;

            int minLenght = word.Length < value.Length ? word.Length : value.Length;
            for (int i = 0; i < minLenght; i++)
                if (word[i] != value[i])
                    differenceAmount++;

            if (differenceAmount <= 1)
                return word;
        }

        return null;
    }

    /// <summary>
    /// Проверяет похожа ли переданная строка на пунктуатор
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Похожий пунктуатор, если есть. Иначе - null</returns>
    /// <remarks>
    /// Строик похожи - если отличаются одним символом (не учтен случай разных символов в конце)
    /// </remarks>
    private string? SimillarPunctuator(string value)
    {
        if (value.Length < 3)
            return null;

        foreach (var punctuator in punctuators)
        {
            if (Math.Abs(punctuator.Length - value.Length) > 1)
                continue;

            int differenceAmount = 0;

            int minLenght = punctuator.Length < value.Length ? punctuator.Length : value.Length;
            for (int i = 0; i < minLenght; i++)
                if (punctuator[i] != value[i])
                    differenceAmount++;

            if (differenceAmount <= 1)
                return punctuator;
        }

        return null;
    }

    bool IsWhiteSpace(char ch)
    {
        return string.IsNullOrWhiteSpace(ch.ToString());
    }
    private bool IsKeyWordLike(string identifier)
    {
        return keywords.Contains(identifier);
    }

    private bool IsLetter(char symbol)
    {
        return char.IsAsciiLetter(symbol);
    }

    private bool IsDigit(char symbol)
    {
        return char.IsAsciiDigit(symbol);
    }
    #region Constants
    private HashSet<char> encodingPrefixes = new HashSet<char> { 'u', 'U', 'L' };

    private HashSet<string> punctuators = new HashSet<string>
        {
            "[", "]", "(", ")", "{", "}", ".", "->",
            "++", "--", "&", "*", "+", "-", "~", "!",
            "/", "%", "<<", ">>", "<", ">", "<=", ">=",
            "==", "!=", "^", "|", "&&", "||", "?", ":",
            ";", "...", "=", "*=", "/=", "%=", "+=", "-=",
            "<<=", ">>=", "&=", "^=", "|=", ",", "#", "##",
            "<:", ":>", "<%", "%>", "%:", "%:%:"
        };

    private HashSet<string> keywords = new HashSet<string>
        {
            "auto", "break", "case", "char", "const", "continue",
            "default", "do", "double", "else", "enum", "extern",
            "float", "for", "goto", "if", "inline", "int", "long",
            "register", "restrict", "return", "short", "signed",
            "sizeof", "static", "struct", "switch", "typedef", "union",
            "unsigned", "void", "volatile", "while", "_Alignas",
            "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic",
            "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
        };

    #endregion

    private enum LexerState
    {
        InitialState,
        Zero,
        Invalid_Binary,
        Valid_Binary,
        Valid_Octal,
        Wait_Number,
        Valid_Decimal,
        Integer_Suffix,
        Valid_Float,
        Float_Suffix,
        Invalid_Exponent,
        Valid_Exponent,
        Exponent_Sign,
        Invalid_Hex,
        Wait_Hex_Number,
        Valid_Hex,
        Valid_Hex_Float,
        Invalid_Binary_Exponent,
        Valid_Binary_Exponent,
        Binary_Exponent_Sign,
        Encoding_Prefix,
        Char_Constant_Begin,
        String_Literal_Begin,
        Char_Constant_Mid,
        String_Literal_Mid,
        Valid_Char_Constant,
        Valid_String_Literal,
        Identifier_Or_Keyword,
        Punctuator
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