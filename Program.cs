using CLexer;
using CLexer.AlarmExceptions;
using CLexer.Exceptions;
using System.Globalization;
using System.Xml;

Lexer lexer = new Lexer("File.txt");
while (true)
{
    try
    {
        Console.WriteLine(lexer.NextToken());
    }
    catch (EndOfStreamException) 
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
        while (IsWhiteSpace(nextCharacter))
            nextCharacter = reader.NextCharacter();

        while (true)
        {
            switch (state)
            {
                #region Initial State
                case LexerState.InitialState:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            var digit = nextCharacter - '0';
                            if (digit >= 1 && digit <= 9)
                                state = LexerState.Valid_Decimal;
                            else if (digit == 0)
                                state = LexerState.Zero;
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Wait_Number;
                        }
                        else if (IsPunctuator(nextCharacter.ToString()))
                        {
                            state = LexerState.Punctuator;
                        }
                        else if (encodingPrefixes.Contains(nextCharacter))
                        {
                            state = LexerState.Encoding_Prefix;
                        }
                        else if ((IsLetter(nextCharacter) && !IsEncodingPrefix(nextCharacter))
                            || nextCharacter == '_')
                        {
                            state = LexerState.Identifier_Or_Keyword;
                        }
                        else if (IsEncodingPrefix(nextCharacter))
                        {
                            state = LexerState.Encoding_Prefix;
                        }
                        else if (nextCharacter == '"')
                        {
                            state = LexerState.String_Literal_Begin;
                        }
                        else if (nextCharacter == '\'')
                        {
                            state = LexerState.Char_Constant_Begin;
                        }
                        break;
                    }
                #endregion

                #region Recognition of identifier or keyword
                case LexerState.Identifier_Or_Keyword:
                    {
                        if (IsAlphaNumeric(nextCharacter) || nextCharacter == '_' )
                        {
                            break;
                        }

                        if (IsKeyWordLike(value))
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
                        if (!IsPunctuator(value + nextCharacter))
                        {
                            possibleTokenType = TokenType.punctuator;
                            return GetToken();
                        }

                        if (IsPunctuator(nextCharacter.ToString()))
                            break;

                        if (IsPunctuator(value))
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

                #region Encoding prefix handling
                case LexerState.Encoding_Prefix:
                    {
                        if (value == "u" && nextCharacter == '8')
                            break;
                        else if (nextCharacter == '\'')
                        {
                            if (value == "L")
                            {
                                state = LexerState.Char_Constant_Begin;
                                break;
                            }
                            else
                            {
                                throw new InvalidEncodingPrefixBeforeCharConstant
                                    ($"Ожидалось: L. Получено:{value}");
                            }
                        }

                        if (IsAlphaNumeric(nextCharacter) || nextCharacter == '_')
                        {
                            state = LexerState.Identifier_Or_Keyword;
                            break;
                        }
                        
                        if (nextCharacter == '"')
                        {
                            state = LexerState.String_Literal_Begin; 
                            break;
                        }

                        throw new Exception($"Неожиданный символ {nextCharacter}");
                    }
                #endregion

                #region Recognition of char constant
                case LexerState.Char_Constant_Begin:
                    {
                        if (nextCharacter == '\'')
                            throw new EmptyCharacterConstantException();

                        state = LexerState.Char_Constant_Mid;
                        break;
                    }
                #endregion

                #region Recognition of the possible escape sequence and closing of constant
                case LexerState.Char_Constant_Mid:
                    {
                        if (value.Last() == '\\')
                        {
                            if (!ValidEscapeSequence(nextCharacter))
                                throw new InvalidEscpeSequenceException();
                            else if (nextCharacter == '\'')
                                throw new ForbiddenSymbolInCharacterConstantException($@"запрещенный символ: {nextCharacter}");

                            // получаем символ идущий за эскейп последовательностью
                            value += nextCharacter;
                            nextCharacter = reader.NextCharacter();
                        }

                        if (nextCharacter == '\'')
                        {
                            state = LexerState.Valid_Char_Constant;
                            break;
                        }
                        else
                        {
                            throw new CharacterConstantTooLongException();
                        }
                    }
                #endregion

                #region Finishing recognition of char constant
                case LexerState.Valid_Char_Constant:
                    {
                        if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }

                        throw new Exception($"неожиданный символ после символьной константы: {nextCharacter}." +
                            $"Ожидался пунктуатор или пробел");
                    }
                #endregion

                #region Recognition of string literal
                case LexerState.String_Literal_Begin:
                    {
                        if (nextCharacter == '\\')
                        {
                            value += nextCharacter;
                            nextCharacter = reader.NextCharacter();
                            if (!ValidEscapeSequence(nextCharacter))
                                throw new InvalidEscpeSequenceException();
                        }
                        else if (nextCharacter == '\"')
                        {
                            state = LexerState.Valid_String_Literal; 
                        }

                        break;
                    }
                #endregion

                #region Recognition of trailing symbols of string literal
                case LexerState.Valid_String_Literal:
                    {
                        if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.string_literal;
                            return GetToken();
                        }
                        else
                            throw new Exception($"неожиданный символ после строкового литерала: {nextCharacter}." +
                            $"Ожидался пунктуатор или пробел");
                    }
                #endregion

                #region Waiting decimal number
                case LexerState.Wait_Number:
                    {
                        if (!IsDigit(nextCharacter))
                        {
                            state = LexerState.Punctuator;
                        }

                        var digit = nextCharacter - '0';
                        if (digit >= 0 && digit <= 9)
                        {
                            state = LexerState.Valid_Float;
                            break;
                        }

                        throw new Exception("что то пошло не так");
                    }
                #endregion

                #region Recognition of decimal numbers
                case LexerState.Valid_Decimal:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            var digit = nextCharacter - '0';
                            if (digit <= 9 && digit >= 0)
                                break;
                        }
                        else if (IsIntegerSuffix(nextCharacter))
                        {
                            state = LexerState.Integer_Suffix; 
                            break;
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Valid_Float;
                            break;
                        }
                        else if (nextCharacter == 'e' || nextCharacter == 'E')
                        {
                            state = LexerState.Invalid_Exponent;
                            break;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new InvalidDecimalNumberException();
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of integer suffix
                case LexerState.Integer_Suffix:
                    {
                        if (IsIntegerSuffix(nextCharacter))
                        {
                            if (value.Last() != nextCharacter)
                            {
                                break;
                            }
                            else if (value.Last() == nextCharacter && (nextCharacter == 'l' || nextCharacter == 'L'))
                            {
                                state = LexerState.Repeated_Integer_Suffix;
                                break;
                            }
                            else
                            {
                                throw new IntegerSuffixInvalidCombinationException();
                            }
                                
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new Exception("неожиданный символ после константы");
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of repeated suffixes (like ll)
                case LexerState.Repeated_Integer_Suffix:
                    {
                        if (IsIntegerSuffix(nextCharacter))
                        {
                            if (value.Last() != nextCharacter)
                            {
                                break;
                            }
                            else
                            {
                                throw new IntegerSuffixInvalidCombinationException();
                            }

                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new Exception("неожиданный символ после константы");
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of valid float numbers
                case LexerState.Valid_Float:
                    {
                        if (IsDigit(nextCharacter))
                            break;
                        else if (nextCharacter == 'e' || nextCharacter == 'E')
                        {
                            state = LexerState.Invalid_Exponent;
                            break;
                        }
                        else if (IsFloatingSuffix(nextCharacter))
                        {
                            state = LexerState.Float_Suffix;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new InvalidDecimalNumberException();
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of exponent. Invalid exponent step
                case LexerState.Invalid_Exponent:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            state = LexerState.Valid_Exponent;
                            break;
                        }
                        else if (nextCharacter == '+' || nextCharacter == '-')
                        {
                            state = LexerState.Exponent_Sign;
                            break;
                        }
                        else
                        {
                            throw new InvalidDecimalNumberException();
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of exponent. Valid exponent step
                case LexerState.Valid_Exponent:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            break;
                        }
                        else if (IsFloatingSuffix(nextCharacter))
                        {
                            state = LexerState.Float_Suffix;
                            break;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new InvalidDecimalNumberException();
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Recognition of exponent. Exponent sign step
                case LexerState.Exponent_Sign:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            state = LexerState.Valid_Exponent;
                            break;
                        }
                        else
                        {
                            throw new InvalidDecimalNumberException();
                        }

                        throw new UnreachableCodeReachedException();
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

    private bool ValidEscapeSequence(char nextCharacter)
    {
        return escapeSequences.Contains(nextCharacter);
    }

    private bool IsEndOfLexem(char nextCharacter)
    {
        return IsWhiteSpace(nextCharacter) || IsPunctuator(nextCharacter.ToString());
    }

    bool IsWhiteSpace(char ch)
    {
        return string.IsNullOrWhiteSpace(ch.ToString());
    }
    private bool IsKeyWordLike(string identifier)
    {
        return keywords.Contains(identifier);
    }

    private bool IsFloatingSuffix(char nextCharacter)
    {
        return floatSuffixes.Contains(nextCharacter);
    }

    private bool IsPunctuator(string identifier)
    {
        return punctuators.Contains(identifier);
    }

    private bool IsIntegerSuffix(char nextCharacter)
    {
        return integerSuffixes.Contains(nextCharacter);
    }

    private bool IsEncodingPrefix(char symbol)
    {
        return encodingPrefixes.Contains(symbol);
    }

    private bool IsAlphaNumeric(char symmbol)
    {
        return IsLetter(symmbol) || IsDigit(symmbol);
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

    private HashSet<char> integerSuffixes = new HashSet<char>
    {
            'u', 'U', 'l', 'L'
    };

    private HashSet<char> floatSuffixes = new HashSet<char>
    {
            'f', 'F', 'l', 'L'
    };

    HashSet<char> escapeSequences = new HashSet<char>
        {
            'a', 'b', 'f', 'n', 'r', 't', 'v',
            '\'', '\"', '\\', '?'
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
        Repeated_Integer_Suffix,
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