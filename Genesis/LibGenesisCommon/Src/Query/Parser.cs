using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LibGenesisCommon.Query
{
    /// <summary>
    /// Exception class to be used to propogate parser processing errors.
    /// </summary>
    public class ParserException : Exception
    {
        private static readonly string __PREFIX = "Parser Processing Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public ParserException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public ParserException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public ParserException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public class Parser<T>
    {
        public Condition<T> Parse(List<Token> tokens)
        {
            Contract.Requires(tokens != null && tokens.Count > 0);
            Stack<Condition<T>> stack = new Stack<Condition<T>>();
            ConditionGroup<T> root = new ConditionGroup<T>();
            stack.Push(root);

            Token prev = null;
            foreach(Token token in tokens)
            {
                ProcessToken(prev, token, stack);
                prev = token;
            }

            Condition<T> c = stack.Pop();
            if (!c.Equals(root))
            {
                throw new ParserException("Invalid Stack State: Expected root condition on stack.");
            }
            root.Close();

            return root;
        }

        private void ProcessToken(Token prev, Token token, Stack<Condition<T>> stack)
        {
            if (token.GetType() == typeof(GroupToken))
            {
                ProcessGroupToken(prev, (GroupToken)token, stack);
            }
            else if (token.GetType() == typeof(OperatorToken))
            {
                if (token.Value == TokenConstants.CONST_AND)
                {
                    AndCondition<T> condition = new AndCondition<T>();
                    AddCondition(condition, stack);
                }
                else if (token.Value == TokenConstants.CONST_OR)
                {
                    OrCondition<T> condition = new OrCondition<T>();
                    AddCondition(condition, stack);
                }
            }
            else if (token.GetType() == typeof(StringToken) || token.GetType() == typeof(StringToken))
            {
                if (token.Value == TokenConstants.CONST_NULL)
                {
                    NullClauseValue nv = new NullClauseValue();
                    nv.Value = token.Value;
                    AddClause(nv, stack);
                }
                else
                {
                    ClauseValue cv = new ClauseValue();
                    cv.Value = token.Value;
                    AddClause(cv, stack);
                }
            }
        }

        private void ProcessGroupToken(Token prev, GroupToken token, Stack<Condition<T>> stack)
        {
            if (token.Value == TokenConstants.CONST_GROUP_START)
            {
                ConditionGroup<T> group = new ConditionGroup<T>();
                AddCondition(group, stack);
            }
            else if (token.Value == TokenConstants.CONST_GROUP_END)
            {
                Condition<T> condition = stack.Pop();
                if (condition.GetType() != typeof(ConditionGroup<T>))
                {
                    throw new ParserException("Invalid Stack State: Expected group condition on stack.");
                }
                ConditionGroup<T> group = (ConditionGroup<T>)condition;
                group.Close();
            }
            else if (token.Value == TokenConstants.CONST_RANGE_START)
            {
                ListCondition<T> condition = new ListCondition<T>();
                if (prev.Value == TokenConstants.CONST_EQUALS)
                {
                    condition.Operator = EListOperator.InRange;
                }
                else if (prev.Value == TokenConstants.CONST_NOT_EQUALS)
                {
                    condition.Operator = EListOperator.NotInRange;
                }
                else
                {
                    throw new ParserException(String.Format("Invalid Range Token : Operator is invalid. [operator={0}]", prev.Value));
                }
                AddCondition(condition, stack);
            }
            else if (token.Value == TokenConstants.CONST_GROUP_END)
            {
                Condition<T> condition = stack.Pop();
                if (condition.GetType() != typeof(ListCondition<T>))
                {
                    throw new ParserException("Invalid Stack State: Expected group condition on stack.");
                }
                ListCondition<T> list = (ListCondition<T>)condition;
                list.Close();
            }
            else if (token.Value == TokenConstants.CONST_LIST_START)
            {
                ListCondition<T> condition = new ListCondition<T>();
                if (prev.Value == TokenConstants.CONST_EQUALS)
                {
                    condition.Operator = EListOperator.In;
                }
                else if (prev.Value == TokenConstants.CONST_NOT_EQUALS)
                {
                    condition.Operator = EListOperator.NotIn;
                }
                else
                {
                    throw new ParserException(String.Format("Invalid Range Token : Operator is invalid. [operator={0}]", prev.Value));
                }
                AddCondition(condition, stack);
            }
            else if (token.Value == TokenConstants.CONST_LIST_END)
            {
                Condition<T> condition = stack.Pop();
                if (condition.GetType() != typeof(ListCondition<T>))
                {
                    throw new ParserException("Invalid Stack State: Expected group condition on stack.");
                }
                ListCondition<T> list = (ListCondition<T>)condition;
                list.Close();
            }
            else
            {
                throw new ParserException(String.Format("Invalid Group Token: [{0}]", token.Value));
            }
        }

        private void AddCondition(Condition<T> condition, Stack<Condition<T>> stack, bool putOnStack = true)
        {
            if (putOnStack)
            {
                stack.Push(condition);
            }
        }

        private void AddClause(ClauseElement clause, Stack<Condition<T>> stack)
        {

        }
    }
}
