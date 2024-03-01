using CLexer;
using CLexer.AlarmExceptions;
using CLexer.Exceptions;
using System.Globalization;
using System.Text.RegularExpressions;
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

public class TokenWithTypo : Token
{
    string message;
    public TokenWithTypo(TokenType type, string value, string message) : base(type, value)
    {
        this.message = message;
    }

    public override string ToString()
    {
        return $"Внимание! Возможно произошла опечатка: \n{message}\n{Value} --- {Type}";
    }
}


public class Lexer 
{
    private LexerState state = LexerState.InitialState;

    private char nextCharacter;

    private TokenType possibleTokenType;

    private string value = string.Empty;

    private Reader reader;

    public List<String> typos = new List<string>();

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
                            if (nextCharacter == '0')
                                state = LexerState.Zero;
                            else
                                state = LexerState.Valid_Decimal;
                                
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Wait_Number;
                        }
                        else if (nextCharacter == '+' || nextCharacter == '-')
                        {
                            state = LexerState.Number_Sign;
                        }
                        else if (nextCharacter == '#')
                        {
                            state = LexerState.Preproccessor_Directives;
                        }
                        else if (nextCharacter == '<')
                        {
                            state = LexerState.Header_LowerThan_Begin;
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
                        {
                            possibleTokenType = TokenType.identifier;
                            return GetToken($"возможно вы хотели ввести: {simillarKeyWord} вы ввели: {value}");
                        }
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
                        {
                            possibleTokenType = TokenType.identifier;
                            return GetToken($"возможно вы хотели ввести: {simillarPunctuator} вы ввели: {value}");
                        }
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
                            throw new InvalidNumberException();
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
                            throw new InvalidNumberException();
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
                            throw new InvalidNumberException();
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
                            throw new InvalidNumberException();
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
                            throw new InvalidNumberException();
                        }

                        throw new UnreachableCodeReachedException();
                    }
                #endregion

                #region Rercognition of sign
                case LexerState.Number_Sign:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            if (nextCharacter == '0') 
                            {
                                state = LexerState.Zero;
                                break;
                            }
                            else
                            {
                                state = LexerState.Valid_Decimal;
                                break;
                            }
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Wait_Number;
                            break;
                        }
                        else if (IsPunctuator(value + nextCharacter))
                        {
                            state = LexerState.Punctuator;
                            break;
                        }
                        else
                        {
                            throw new InvalidSymbolAfterSignException(); 
                        }
                    }
                #endregion

                #region Recognition of 0
                case LexerState.Zero:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            state = LexerState.Valid_Decimal;
                            break;
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Valid_Float;
                            break;
                        }
                        else if (nextCharacter == 'b' || nextCharacter == 'B')
                        {
                            state = LexerState.Invalid_Binary; 
                            break;   
                        }
                        else if (nextCharacter == 'x' || nextCharacter == 'X')
                        {
                            state = LexerState.Invalid_Hex;
                            break;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                        {
                            throw new InvalidNumberException();
                        }
                    }
                #endregion

                #region Recognition of binary number. Step invalid number
                case LexerState.Invalid_Binary:
                    {
                        if (nextCharacter == '0' || nextCharacter == '1')
                        {
                            state = LexerState.Valid_Binary; 
                            break;
                        }
                        else
                        {
                            throw new InvalidNumberException();
                        }
                    }
                #endregion

                #region Recognition of binary number. Step valid number
                case LexerState.Valid_Binary:
                    {
                        if (nextCharacter == '0' || nextCharacter == '1')
                        {
                            break;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else if (IsIntegerSuffix(nextCharacter))
                        {
                            state = LexerState.Integer_Suffix;
                            break;
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step invalid number
                case LexerState.Invalid_Hex:
                    {
                        if (nextCharacter == '.')
                        {
                            state = LexerState.Wait_Hex_Number;
                            break;
                        }
                        else if (IsHexHumber(nextCharacter))
                        {
                            state = LexerState.Valid_Hex;
                            break;
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Waiting for the number after .
                case LexerState.Wait_Hex_Number:
                    {
                        if (IsHexHumber(nextCharacter))
                        {
                            state = LexerState.Valid_Hex_Float;
                            break;
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step valid number
                case LexerState.Valid_Hex:
                    {
                        if (IsHexHumber(nextCharacter))
                        {
                            break;
                        }
                        else if (IsIntegerSuffix(nextCharacter))
                        {
                            state = LexerState.Integer_Suffix;
                            break;
                        }
                        else if (nextCharacter == '.')
                        {
                            state = LexerState.Valid_Hex_Float;
                            break;
                        }
                        else if (nextCharacter == 'p' || nextCharacter == 'P')
                        {
                            state = LexerState.Invalid_Binary_Exponent;
                            break;
                        }
                        else if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step valid floating number
                case LexerState.Valid_Hex_Float:
                    {
                        if (IsHexHumber(nextCharacter))
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
                        else if (nextCharacter == 'p' || nextCharacter == 'P')
                        {
                            state = LexerState.Invalid_Binary_Exponent;
                            break;
                        }
                        else 
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step invalid binary exponent
                case LexerState.Invalid_Binary_Exponent:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            state = LexerState.Valid_Binary_Exponent;
                            break;
                        }
                        else if (nextCharacter == '+' || nextCharacter == '-')
                        {
                            state = LexerState.Binary_Exponent_Sign;
                            break;
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step valid binary exponent
                case LexerState.Valid_Binary_Exponent:
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
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of Hex numbers. Step binary exponent sign
                case LexerState.Binary_Exponent_Sign:
                    {
                        if (IsDigit(nextCharacter))
                        {
                            state = LexerState.Valid_Binary_Exponent;
                            break;
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of floating suffix
                case LexerState.Float_Suffix:
                    {
                        if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.constant;
                            return GetToken();
                        }
                        else
                            throw new InvalidNumberException();
                    }
                #endregion

                #region Recognition of preproccessor directives
                case LexerState.Preproccessor_Directives:
                    { 
                        if (IsLetter(nextCharacter))
                                break;
                        else if (IsEndOfLexem(nextCharacter) && IsPreprocessorDirective(value))
                        {
                            possibleTokenType = TokenType.preprocessor_directive;
                            return GetToken();
                        }
                        else 
                            throw new InvalidPreprocessorDirectiveException();
                    }
                #endregion

                #region Recognition of header name with <. Begining
                case LexerState.Header_LowerThan_Begin:
                    {
                        if (IsPunctuator(value + nextCharacter))
                        {
                            state = LexerState.Punctuator;
                            break;
                        }
                        else if (nextCharacter == '>')
                        {
                            state = LexerState.Header_LowerThan_End;
                            break;
                        }
                        else
                            break;
                    }
                #endregion

                #region Recognition of header name with <. Ending
                case LexerState.Header_LowerThan_End:
                    {
                        if (IsEndOfLexem(nextCharacter))
                        {
                            possibleTokenType = TokenType.header_name;
                            return GetToken();
                        }
                        else
                            throw new InvalidHeaderNameException();
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
    /// Возвращает токен полученный из лексеммы
    /// </summary>
    /// <returns>Токен, полученный в результате анализа</returns>
    /// <remarks>
    /// Обнуляет состояние и переменную содержимиого. Возвращает полученный токен
    /// </remarks>
    private Token GetToken(string message)
    {
        var returnValue = value;
        var returnType = possibleTokenType;

        state = LexerState.InitialState;
        value = string.Empty;
        return new TokenWithTypo(returnType, returnValue, message);
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
        if (value.Length <= 2)
            return null;
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

    private bool IsPreprocessorDirective(string identifier)
    {
        return preprocessorDirectives.Contains(identifier);
    }

    private bool IsFloatingSuffix(char nextCharacter)
    {
        return floatSuffixes.Contains(nextCharacter);
    }

    private bool IsPunctuator(string identifier)
    {
        return punctuators.Contains(RemoveWhitespace(identifier));
    }

    private string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !Char.IsWhiteSpace(c))
            .ToArray());
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

    private bool IsHexHumber(char nextCharacter)
    {
        return
            (nextCharacter >= '0' && nextCharacter <= '9') ||
            (nextCharacter >= 'a' && nextCharacter <= 'f') ||
            (nextCharacter >= 'A' && nextCharacter <= 'F');
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

    private HashSet<string> preprocessorDirectives = new HashSet<string>
        {
            "#include", "#define", "#error", "#if", "#elif", "#endif",
            "#else", "#ifdef", "#ifndef", "#line", "#undef"
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
        Punctuator,
        Number_Sign,
        Preproccessor_Directives,
        Header_Quoted_Begin,
        Header_Quoted_End,
        Header_LowerThan_Begin,
        Header_LowerThan_End
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
    c_operator,
    identifier_with_typo,
    preprocessor_directive,
}