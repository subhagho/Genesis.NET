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

    public class ConditionBuilder<T>
    {
        private ConditionGroup<T> root = null;
        private Stack<Condition<T>> stack = new Stack<Condition<T>>();

        public Condition<T> GetCondition()
        {
            return root;
        }

        private void CheckRoot()
        {
            if (root == null)
            {
                root = new ConditionGroup<T>();
            }
            stack.Push(root);
        }

        public Condition<T> Peek()
        {
            if (stack != null && stack.Count > 0)
                return stack.Peek();

            return null;
        }

        public Condition<T> Group()
        {
            CheckRoot();
            ConditionGroup<T> group = new ConditionGroup<T>();
            AddCondition(group);
            return group;
        }

        public Condition<T> RangeCondition(string field, EListOperator oper)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper == EListOperator.InRange || oper == EListOperator.NotInRange);
            CheckRoot();

            ListCondition<T> condition = new ListCondition<T>();
            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            condition.LeftElement = var;
            condition.Operator = oper;

            AddCondition(condition);

            return condition;
        }

        public Condition<T> InCondition(string field, EListOperator oper)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper == EListOperator.In || oper == EListOperator.NotIn);
            CheckRoot();

            ListCondition<T> condition = new ListCondition<T>();
            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            condition.LeftElement = var;
            condition.Operator = oper;

            AddCondition(condition);

            return condition;
        }

        public Condition<T> And()
        {
            CheckRoot();
            AndCondition<T> and = new AndCondition<T>();
            AddCondition(and);
            return and;
        }

        public Condition<T> Or()
        {
            CheckRoot();
            OrCondition<T> or = new OrCondition<T>();
            AddCondition(or);
            return or;
        }

        public Condition<T> Compare(string field, EBasicOperator oper, string value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper != EBasicOperator.None);
            CheckRoot();

            BasicCondition<T> condition = new BasicCondition<T>();

            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            condition.LeftElement = var;

            condition.Operator = oper;

            ClauseValue val = new ClauseValue();
            val.Value = value;
            condition.RightElement = val;

            AddCondition(condition);
            return condition;
        }

        public Condition<T> Compare(string field, EBasicOperator oper)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper != EBasicOperator.None);
            CheckRoot();

            BasicCondition<T> condition = new BasicCondition<T>();

            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            condition.LeftElement = var;

            condition.Operator = oper;

            AddCondition(condition);
            return condition;
        }

        public Condition<T> Value(string value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(value));
            ClauseValue val = new ClauseValue();
            val.Value = value;

            return AddClause(val);
        }

        public Condition<T> FieldOperation(string field, EMathOperator oper, string value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper != EMathOperator.None);
            Contract.Requires(!String.IsNullOrWhiteSpace(value));

            ClauseOperationElement elem = new ClauseOperationElement();

            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            elem.LeftElement = var;

            elem.Operator = oper;

            ClauseValue val = new ClauseValue();
            val.Value = value;

            return AddClause(elem);
        }

        public Condition<T> FieldOperation(string field, EMathOperator oper)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(field));
            Contract.Requires(oper != EMathOperator.None);

            ClauseOperationElement elem = new ClauseOperationElement();

            ClauseVariable var = new ClauseVariable();
            var.Name = field;
            elem.LeftElement = var;

            elem.Operator = oper;

            return AddClause(elem);
        }

        public Condition<T> Operation(ClauseElement clause)
        {
            Contract.Requires(clause != null);

            return AddClause(clause);
        }

        public Condition<T> Close()
        {
            Condition<T> condition = stack.Pop();
            if (condition.GetType() == typeof(ConditionGroup<T>))
            {
                ConditionGroup<T> group = (ConditionGroup<T>)condition;
                group.Validate();
                group.Close();
            }
            else if (condition.GetType() == typeof(ListCondition<T>))
            {
                ListCondition<T> list = (ListCondition<T>)condition;
                list.Validate();
                list.Close();
            }
            else
            {
                if (!condition.IsClosed())
                    throw new QueryException(String.Format("Invalid Stack State: Condition is not closed. [type={0}]", condition.GetType().FullName));
            }
            return condition;
        }

        private void AddCondition(Condition<T> condition)
        {
            Condition<T> parent = stack.Peek();
            if (parent.GetType() == typeof(ConditionGroup<T>))
            {
                ConditionGroup<T> group = (ConditionGroup<T>)parent;
                if (group.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: Group condition is already closed. [type={0}]", group.GetType().FullName));
                }
                group.Add(condition);
            }
            else if (parent.GetType() == typeof(AndCondition<T>))
            {
                AndCondition<T> and = (AndCondition<T>)parent;
                if (and.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: And condition is already closed. [type={0}]", and.GetType().FullName));
                }
                and.Add(condition);
            }
            else if (parent.GetType() == typeof(AndCondition<T>))
            {
                AndCondition<T> and = (AndCondition<T>)parent;
                if (and.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: And condition is already closed. [type={0}]", and.GetType().FullName));
                }
                and.Add(condition);
            }
            else if (parent.GetType() == typeof(OrCondition<T>))
            {
                OrCondition<T> or = (OrCondition<T>)parent;
                if (or.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: Or condition is already closed. [type={0}]", or.GetType().FullName));
                }
                or.Add(condition);
            }
            stack.Push(condition);
        }

        private Condition<T> AddClause(ClauseElement clause)
        {
            Condition<T> parent = stack.Peek();
            if (parent.GetType() == typeof(ListCondition<T>))
            {
                ListCondition<T> list = (ListCondition<T>)parent;
                if (list.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: List condition is already closed. [type={0}]", list.GetType().FullName));
                }
                if (clause.GetType() != typeof(ClauseValue))
                {
                    throw new QueryException(String.Format("Invalid Clause: Expected Value clause. [type={0}]", clause.GetType().FullName));
                }
                list.AddValue((ClauseValue)clause);

                return list;
            }
            else if (parent.GetType() == typeof(BasicCondition<T>))
            {
                BasicCondition<T> condition = (BasicCondition<T>)parent;
                if (condition.IsClosed())
                {
                    throw new QueryException(String.Format("Invalid Stack State: List condition is already closed. [type={0}]", condition.GetType().FullName));
                }
                if (condition.LeftElement == null)
                {
                    condition.LeftElement = clause;
                }
                else
                {
                    condition.RightElement = clause;
                }
                return condition;
            }
            throw new QueryException(String.Format("Invalid Stack State: Cannot add clause. [type={0}]", parent.GetType().FullName));
        }
    }
    public class Parser<T>
    {
        public Condition<T> Parse(List<Token> tokens)
        {
            Contract.Requires(tokens != null && tokens.Count > 0);
            ConditionBuilder<T> builder = new ConditionBuilder<T>();

            for (int index = 0; index < tokens.Count; index++)
            {
                int delta = ProcessToken(index, tokens, builder);
                if (delta > 0)
                {
                    index += (delta - 1);
                }
            }

            return builder.GetCondition();
        }

        private int ProcessToken(int index, List<Token> tokens, ConditionBuilder<T> builder)
        {
            Token token = tokens[index];
            if (TokenConstants.IsJoinOperator(token))
            {
                if (token.Value == TokenConstants.CONST_AND)
                {
                    builder.And();
                }
                else if (token.Value == TokenConstants.CONST_OR)
                {
                    builder.Or();
                }
            }
            return 0;
        }
    }
}
