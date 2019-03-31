using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LibGenesisCommon.Query
{
    /// <summary>
    /// Exception class to be used to propogate token processing errors.
    /// </summary>
    public class TokenizerException : Exception
    {
        private static readonly string __PREFIX = "Token Processing Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public TokenizerException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public TokenizerException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public TokenizerException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public static class TokenConstants
    {
        public const string CONST_EQUALS = "=";
        public const string CONST_NOT_EQUALS = "!=";
        public const string CONST_GREATER = ">";
        public const string CONST_GREATER_EQUALS = ">=";
        public const string CONST_LESS = "<";
        public const string CONST_LESS_EQUALS = "<=";
        public const string CONST_NULL = "NULL";
        public const string CONST_GROUP_START = "(";
        public const string CONST_GROUP_END = ")";
        public const string CONST_RANGE_START = "[";
        public const string CONST_RANGE_END = "]";
        public const string CONST_LIST_START = "{";
        public const string CONST_LIST_END = "}";
        public const string CONST_AND = "&&";
        public const string CONST_OR = "||";
        public const string CONST_PLUS = "+";
        public const string CONST_MINUS = "-";
        public const string CONST_MULT = "*";
        public const string CONST_DIVD = "/";
        public const string CONST_MOD = "%";
        public const string CONST_BIT_AND = "&";
        public const string CONST_BIT_OR = "|";
        public const string CONST_BIT_NOT = "^";
        public const string CONST_BIT_LEFT = "<<";
        public const string CONST_BIT_RIGHT = ">>";
        public const string CONST_BIT_XOR = "~";
        public const char CONST_DOUBLE_QUOTE = '\"';
        public const char CONST_SINGLE_QUOTE = '\'';
        public const string CONST_COMMA = ",";

        public static Token ParseToken(string value, int index)
        {
            if (index < value.Length)
            {
                int indx = 0;
                string str = value.Substring(index);
                foreach (char c in str)
                {
                    if (Char.IsWhiteSpace(c))
                    {
                        indx++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (indx < str.Length)
                {
                    str = str.Substring(indx);
                }
                if (!String.IsNullOrWhiteSpace(str))
                {
                    if (str.StartsWith(CONST_NULL))
                    {
                        char cn = str[CONST_NULL.Length];
                        if (!Char.IsLetterOrDigit(cn))
                        {
                            ValueToken t = new ValueToken();
                            t.Value = CONST_NULL;
                            t.Index = index + indx;
                            t.Length = CONST_NULL.Length;

                            return t;
                        }
                    }
                    else if (str.StartsWith(CONST_AND))
                    {
                        return OperatorToken(CONST_AND, index + indx);
                    }
                    else if (str.StartsWith(CONST_OR))
                    {
                        return OperatorToken(CONST_OR, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_LEFT))
                    {
                        return OperatorToken(CONST_BIT_LEFT, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_RIGHT))
                    {
                        return OperatorToken(CONST_BIT_RIGHT, index + indx);
                    }
                    else if (str.StartsWith(CONST_NOT_EQUALS))
                    {
                        return OperatorToken(CONST_NOT_EQUALS, index + indx);
                    }
                    else if (str.StartsWith(CONST_GREATER_EQUALS))
                    {
                        return OperatorToken(CONST_GREATER_EQUALS, index + indx);
                    }
                    else if (str.StartsWith(CONST_LESS_EQUALS))
                    {
                        return OperatorToken(CONST_LESS_EQUALS, index + indx);
                    }
                    else if (str.StartsWith(CONST_EQUALS))
                    {
                        return OperatorToken(CONST_EQUALS, index + indx);
                    }
                    else if (str.StartsWith(CONST_GREATER))
                    {
                        return OperatorToken(CONST_GREATER, index + indx);
                    }
                    else if (str.StartsWith(CONST_LESS))
                    {
                        return OperatorToken(CONST_LESS, index + indx);
                    }
                    else if (str.StartsWith(CONST_GROUP_START))
                    {
                        return GroupToken(CONST_GROUP_START, index + indx);
                    }
                    else if (str.StartsWith(CONST_GROUP_END))
                    {
                        return GroupToken(CONST_GROUP_END, index + indx);
                    }
                    else if (str.StartsWith(CONST_RANGE_START))
                    {
                        return GroupToken(CONST_RANGE_START, index + indx);
                    }
                    else if (str.StartsWith(CONST_RANGE_END))
                    {
                        return GroupToken(CONST_RANGE_END, index + indx);
                    }
                    else if (str.StartsWith(CONST_LIST_START))
                    {
                        return GroupToken(CONST_LIST_START, index + indx);
                    }
                    else if (str.StartsWith(CONST_LIST_END))
                    {
                        return GroupToken(CONST_LIST_END, index + indx);
                    }
                    else if (str.StartsWith(CONST_PLUS))
                    {
                        return OperatorToken(CONST_PLUS, index + indx);
                    }
                    else if (str.StartsWith(CONST_MINUS))
                    {
                        return OperatorToken(CONST_MINUS, index + indx);
                    }
                    else if (str.StartsWith(CONST_MULT))
                    {
                        return OperatorToken(CONST_MULT, index + indx);
                    }
                    else if (str.StartsWith(CONST_DIVD))
                    {
                        return OperatorToken(CONST_DIVD, index + indx);
                    }
                    else if (str.StartsWith(CONST_MOD))
                    {
                        return OperatorToken(CONST_MOD, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_AND))
                    {
                        return OperatorToken(CONST_BIT_AND, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_LEFT))
                    {
                        return OperatorToken(CONST_BIT_LEFT, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_NOT))
                    {
                        return OperatorToken(CONST_BIT_NOT, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_OR))
                    {
                        return OperatorToken(CONST_BIT_OR, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_RIGHT))
                    {
                        return OperatorToken(CONST_BIT_RIGHT, index + indx);
                    }
                    else if (str.StartsWith(CONST_BIT_XOR))
                    {
                        return OperatorToken(CONST_BIT_XOR, index + indx);
                    }
                }
            }
            return null;
        }

        private static OperatorToken OperatorToken(string token, int index)
        {
            OperatorToken t = new OperatorToken();
            t.Value = token;
            t.Index = index;
            t.Length = token.Length;

            return t;
        }

        private static GroupToken GroupToken(string token, int index)
        {
            GroupToken t = new GroupToken();
            t.Value = token;
            t.Index = index;
            t.Length = token.Length;

            return t;
        }
    }


    public class Token
    {
        public string Value { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }

        public override string ToString()
        {
            return "[" + GetType().FullName + ": [Value={" + Value + "}, Index={" + Index + "}, Length={" + Length +
                "}]";
        }
    }

    public class ValueToken : Token
    {

    }

    public class StringToken : ValueToken
    {

    }

    public class OperatorToken : Token
    {

    }

    public class GroupToken : Token
    {
    }

    public class ListSeperatorToken : Token
    {
        public ListSeperatorToken()
        {
            Value = TokenConstants.CONST_COMMA;
        }
    }

    public class Tokenizer
    {
        public List<Token> Tokenize(string query)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(query));

            List<Token> tokens = new List<Token>();
            bool quoted = false;
            StringBuilder sb = null;

            for (int index = 0; index < query.Length; index++)
            {
                char cc = query[index];
                if (Char.IsWhiteSpace(cc))
                {
                    if (quoted)
                    {
                        if (sb == null)
                        {
                            throw new TokenizerException(String.Format("Invalid Tokenizer State: expected value buffer. [string={0}][index={1}]", query, index));
                        }
                        sb.Append(cc);
                    }
                }
                else
                {
                    Token tk = TokenConstants.ParseToken(query, index);
                    if (!quoted && tk != null)
                    {
                        if (sb != null)
                        {
                            ValueToken t = ValueToken(sb, index);
                            tokens.Add(t);

                            sb = null;
                        }
                        tokens.Add(tk);
                        index = tk.Index + tk.Length - 1;
                        continue;
                    }
                    else
                    {
                        if (cc == TokenConstants.CONST_DOUBLE_QUOTE || cc == TokenConstants.CONST_SINGLE_QUOTE)
                        {
                            if (quoted)
                            {
                                char pc = query[index - 1];
                                if (pc == '\\')
                                {
                                    if (sb == null)
                                    {
                                        throw new TokenizerException(String.Format("Invalid Tokenizer State: expected value buffer. [string={0}][index={1}]", query, index));
                                    }
                                    sb.Append(cc);
                                }
                                else
                                {
                                    if (sb == null)
                                    {
                                        throw new TokenizerException(String.Format("Invalid Tokenizer State: expected value buffer. [string={0}][index={1}]", query, index));
                                    }
                                    quoted = false;
                                    StringToken vt = new StringToken();
                                    vt.Value = sb.ToString();
                                    vt.Index = index - vt.Value.Length;
                                    vt.Length = vt.Value.Length;

                                    tokens.Add(vt);

                                    sb = null;
                                }
                            }
                            else
                            {
                                quoted = true;
                                if (sb != null)
                                {
                                    ValueToken t = ValueToken(sb, index);
                                    tokens.Add(t);

                                    sb = null;
                                }
                                sb = new StringBuilder();
                            }
                        }
                        else if (cc == TokenConstants.CONST_COMMA[0])
                        {
                            if (sb != null)
                            {
                                ValueToken vt = ValueToken(sb, index);
                                tokens.Add(vt);

                                sb = null;
                            }
                            ListSeperatorToken t = new ListSeperatorToken();
                            t.Index = index;
                            t.Length = t.Value.Length;
                            tokens.Add(t);
                        }
                        else
                        {
                            if (sb != null)
                            {
                                sb.Append(cc);
                            }
                            else
                            {
                                sb = new StringBuilder();
                                sb.Append(cc);
                            }
                        }
                    }
                }
            }
            if (tokens.Count > 0)
            {
                return tokens;
            }
            return null;
        }

        private ValueToken ValueToken(StringBuilder builder, int index)
        {
            ValueToken vt = new ValueToken();
            vt.Value = builder.ToString();
            vt.Index = index - vt.Value.Length;
            vt.Length = vt.Value.Length;

            return vt;
        }
    }
}