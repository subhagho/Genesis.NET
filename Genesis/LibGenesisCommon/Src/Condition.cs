using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using LibZConfig.Common.Utils;

namespace LibGenesisCommon.Common
{
    /// <summary>
    /// Exception class to be used to propogate Condition processing errors.
    /// </summary>
    public class ConditionException : Exception
    {
        private static readonly string __PREFIX = "Process Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public ConditionException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public ConditionException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public ConditionException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public abstract class Condition<T>
    {
        internal bool complete = false;

        public bool IsComplete()
        {
            return complete;
        }

        public abstract void Validate();
    }

    public class GroupCondition<T> : Condition<T>
    {
        public List<Condition<T>> Conditions { get; }

        internal GroupCondition()
        {
            Conditions = new List<Condition<T>>();
        }

        public GroupCondition<T> Add(Condition<T> condition)
        {
            Contract.Requires(condition != null);
            Conditions.Add(condition);
            return this;
        }

        public GroupCondition<T> Close()
        {
            complete = true;
            return this;
        }

        public bool IsEmpty()
        {
            return (Conditions.Count <= 0);
        }

        public override void Validate()
        {
            if (IsEmpty())
            {
                throw new ConditionException("No group conditions specified.");
            }
            if (!complete)
            {
                throw new ConditionException("Group condition not closed.");
            }
            foreach (Condition<T> condition in Conditions)
            {
                condition.Validate();
            }
        }
    }

    public class AndCondition<T> : Condition<T>
    {
        public Condition<T> Left;
        public Condition<T> Right;

        internal AndCondition() { }

        public override void Validate()
        {
            if (Left == null || Right == null)
            {
                throw new ConditionException("And condition incomplete: Left/Right condition missing.");
            }
            if (!complete)
            {
                throw new ConditionException("And condition not closed.");
            }
            Left.Validate();
            Right.Validate();
        }
    }

    public class OrCondition<T> : Condition<T>
    {
        public Condition<T> Left;
        public Condition<T> Right;

        internal OrCondition() { }

        public override void Validate()
        {
            if (Left == null || Right == null)
            {
                throw new ConditionException("Or condition incomplete: Left/Right condition missing.");
            }
            if (!complete)
            {
                throw new ConditionException("Or condition not closed.");
            }
            Left.Validate();
            Right.Validate();
        }

    }

    public enum EBasicOperator
    {
        None,
        EqualTo,
        NotEqualTo,
        LessThan,
        LessThanEqualTo,
        GreaterThan,
        GreaterThanEqualTo,
        IsNull,
        NotNull
    }

    public abstract class Operator<T> : Condition<T>
    {
        public string Property { get; set; }
        public object Value { get; set; }

        public override void Validate()
        {
            PropertyInfo pi = TypeUtils.FindProperty(typeof(T), Property);
            if (pi == null)
            {
                throw new ConditionException(String.Format("Property not found: [type={0}][property={1}]", typeof(T).FullName, Property));
            }
        }
    }

    public class BasicOperator<T> : Operator<T>
    {
        public EBasicOperator Operator { get; set; }

        internal BasicOperator()
        {
            Operator = EBasicOperator.None;
            Value = null;
        }

        public override void Validate()
        {
            if (String.IsNullOrWhiteSpace(Property))
            {
                throw new ConditionException("Operator condition incomplete: Missing Property name");
            }
            if (Operator == EBasicOperator.None)
            {
                throw new ConditionException("Operator condition incomplete: Operator missing");
            }
            if (Value == null)
            {
                if (Operator != EBasicOperator.IsNull && Operator != EBasicOperator.NotNull)
                {
                    throw new ConditionException("Operator condition incomplete: Value missing");
                }
            }
            if (!complete)
            {
                throw new ConditionException("Operator condition not closed.");
            }
            base.Validate();
        }
    }

    public enum EGroupOperator
    {
        None,
        In,
        NotIn,
        InRange,
        NotInRange
    }

    public class GroupOperator<T> : Operator<T>
    {
        public EGroupOperator Operator { get; set; }

        internal GroupOperator() { }

        public override void Validate()
        {
            if (String.IsNullOrWhiteSpace(Property))
            {
                throw new ConditionException("Operator condition incomplete: Missing Property name");
            }
            if (Operator == EGroupOperator.None)
            {
                throw new ConditionException("Operator condition incomplete: Operator missing");
            }
            if (Value == null)
            {
                throw new ConditionException("Operator condition incomplete: Value missing");
            }
            if (Operator == EGroupOperator.In || Operator == EGroupOperator.NotIn)
            {
                if (ReflectionUtils.ImplementsGenericInterface(Value.GetType(), typeof(ICollection<>)))
                {
                    throw new ConditionException(String.Format("Invalid Value type: [expected={0}][actual={1}]", typeof(ICollection<>).FullName, Value.GetType().FullName));
                }
            }
            else if (Operator == EGroupOperator.InRange || Operator == EGroupOperator.NotInRange)
            {
                if (!Value.GetType().IsArray)
                {
                    throw new ConditionException(String.Format("Invalid Value type: [expected={0}][actual={1}]", typeof(Array).FullName, Value.GetType().FullName));
                }
            }
            if (!complete)
            {
                throw new ConditionException("Operator condition not closed.");
            }
            base.Validate();
        }
    }

    public class ConditionBuilder<T>
    {
        private Condition<T> condition = null;
        private Stack<Condition<T>> stack = new Stack<Condition<T>>();

        public Condition<T> GetCondition()
        {
            return condition;
        }

        public ConditionBuilder<T> Group()
        {
            GroupCondition<T> group = new GroupCondition<T>();
            AddCondition(group);
            return this;
        }

        public ConditionBuilder<T> Close()
        {
            Condition<T> parent = stack.Peek();
            if (parent == null)
            {
                throw new ConditionException("Condition Stack Error : No codnition found on stack.");
            }
            if (parent.GetType() != typeof(GroupCondition<T>))
            {
                throw new ConditionException(String.Format("Condition Stack Error : Condition isn't of type GroupCondition. [type={0}]", parent.GetType().FullName));
            }
            GroupCondition<T> group = (GroupCondition<T>)parent;
            group.Close();

            stack.Pop();

            return this;
        }

        public ConditionBuilder<T> And()
        {
            AndCondition<T> and = new AndCondition<T>();
            AddCondition(and);
            return this;
        }

        public ConditionBuilder<T> Or()
        {
            OrCondition<T> or = new OrCondition<T>();
            AddCondition(or);
            return this;
        }

        public ConditionBuilder<T> Operator(string property, EBasicOperator oper, object value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(property));

            BasicOperator<T> bo = new BasicOperator<T>();
            bo.Property = property;
            bo.Operator = oper;
            bo.Value = value;
            AddCondition(bo);

            return this;
        }

        public ConditionBuilder<T> Operator(string property, EGroupOperator oper, object value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(property));

            GroupOperator<T> gro = new GroupOperator<T>();
            gro.Property = property;
            gro.Operator = oper;
            gro.Value = value;
            AddCondition(gro);

            return this;
        }

        private void AddCondition(Condition<T> condition)
        {
            if (this.condition == null)
            {
                this.condition = condition;
            }
            else
            {
                Condition<T> parent = stack.Peek();
                if (parent == null)
                {
                    throw new ConditionException("Condition Stack Error : No codnition found on stack.");
                }
                if (parent.GetType() == typeof(GroupCondition<T>))
                {
                    GroupCondition<T> group = (GroupCondition<T>)parent;
                    if (group.IsComplete())
                    {
                        throw new ConditionException("Condition Stack Error : Group condition already closed.");
                    }
                    group.Add(condition);
                }
                else if (parent.GetType() == typeof(AndCondition<T>))
                {
                    AndCondition<T> and = (AndCondition<T>)condition;
                    if (and.IsComplete())
                    {
                        throw new ConditionException("Condition Stack Error : And condition already complete.");
                    }
                    if (and.Left == null)
                    {
                        and.Left = condition;
                    }
                    else if (and.Right != null)
                    {
                        throw new ConditionException("Condition Stack Error : And condition already complete.");
                    }
                    else
                    {
                        and.Right = condition;
                        and.complete = true;
                        if (ReflectionUtils.IsSubclassOfRawGeneric(condition.GetType(), typeof(Operator<>)))
                            stack.Pop();
                    }
                }
                else if (parent.GetType() == typeof(OrCondition<T>))
                {
                    OrCondition<T> or = (OrCondition<T>)condition;
                    if (or.IsComplete())
                    {
                        throw new ConditionException("Condition Stack Error : Or condition already complete.");
                    }
                    if (or.Left == null)
                    {
                        or.Left = condition;
                    }
                    else if (or.Right != null)
                    {
                        throw new ConditionException("Condition Stack Error : Or condition already complete.");
                    }
                    else
                    {
                        or.Right = condition;
                        or.complete = true;
                        if (ReflectionUtils.IsSubclassOfRawGeneric(condition.GetType(), typeof(Operator<>)))
                            stack.Pop();
                    }
                }
                else
                {
                    throw new ConditionException(String.Format("Condition Stack Error : Cannot add to parent. [parent={0}]", parent.GetType().FullName));
                }
            }
            if (!ReflectionUtils.IsSubclassOfRawGeneric(condition.GetType(), typeof(Operator<>)))
                stack.Push(condition);
        }

        public void Validate()
        {
            if (condition == null)
            {
                throw new ConditionException("No condition specified.");
            }
            if (stack.Count != 0)
            {
                throw new ConditionException("Condition Stack Error : Stack is not empty.");
            }
            condition.Validate();
        }
    }

    public static class ConditionParser
    {
        public static Condition<T> Parse<T>(string condition)
        {
            ConditionBuilder<T> builder = new ConditionBuilder<T>();

            builder.Validate();
            return builder.GetCondition();
        }
    }
}
