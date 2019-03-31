using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace LibGenesisCommon.Query
{
    /// <summary>
    /// Exception class to be used to propogate query processing errors.
    /// </summary>
    public class QueryException : Exception
    {
        private static readonly string __PREFIX = "Query Processing Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public QueryException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public QueryException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public QueryException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public abstract class Condition<T>
    {
        protected bool closed = false;

        public virtual bool IsClosed()
        {
            return closed;
        }

        public abstract void Validate();
    }

    public class ConditionGroup<T> : Condition<T>
    {
        private List<Condition<T>> conditions = new List<Condition<T>>();
        
        public List<Condition<T>> Conditions
        {
            get { return conditions; }
        }

        public ConditionGroup<T> Add(Condition<T> condition)
        {
            Contract.Requires(condition != null);
            conditions.Add(condition);

            return this;
        }

        public ConditionGroup<T> Close()
        {
            closed = true;
            return this;
        }

        public override void Validate()
        {
            if (!closed)
            {
                throw new QueryException("Condition Group not closed.");
            }
            if (conditions.Count <= 0)
            {
                throw new QueryException("No conditions specified for Group.");
            }
            foreach (Condition<T> condition in conditions)
            {
                condition.Validate();
            }
        }
    }

    public class AndCondition<T> : Condition<T>
    {
        private Condition<T> _left;
        public Condition<T> LeftClause
        {
            get { return _left; }
        }

        private Condition<T> _right;
        public Condition<T> RightClause
        {
            get { return _right; }
        }

        public AndCondition<T> Add(Condition<T> condition)
        {
            Contract.Requires(condition != null);
            Contract.Requires(!closed);
            if (_left == null)
            {
                _left = condition;
            }
            else
            {
                _right = condition;
                closed = true;
            }
            return this;
        }

        public override void Validate()
        {
            if (_left == null)
            {
                throw new QueryException("Missing left predicate.");
            }
            if (_right == null)
            {
                throw new QueryException("Missing right predicate.");
            }
            _left.Validate();
            _right.Validate();
        }
    }

    public class OrCondition<T> : Condition<T>
    {
        private Condition<T> _left;
        public Condition<T> LeftClause
        {
            get { return _left; }
        }

        private Condition<T> _right;
        public Condition<T> RightClause
        {
            get { return _right; }
        }

        public OrCondition<T> Add(Condition<T> condition)
        {
            Contract.Requires(condition != null);
            Contract.Requires(!closed);
            if (_left == null)
            {
                _left = condition;
            }
            else
            {
                _right = condition;
                closed = true;
            }
            return this;
        }

        public override void Validate()
        {
            if (_left == null)
            {
                throw new QueryException("Missing left predicate.");
            }
            if (_right == null)
            {
                throw new QueryException("Missing right predicate.");
            }
            _left.Validate();
            _right.Validate();
        }
    }

    public abstract class ClauseElement
    {
        public abstract void Validate();
    }

    public class ClauseVariable : ClauseElement
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public override void Validate()
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Name)));
            }
            if (Type == null)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Type)));
            }
        }
    }

    public class ClauseValue : ClauseElement
    {
        public object Value { get; set; }

        public override void Validate()
        {
            if (Value == null)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Value)));
            }
        }
    }

    public class NullClauseValue : ClauseValue
    {
        public NullClauseValue()
        {
            Value = TokenConstants.CONST_NULL;
        }
    }

    public enum EMathOperator
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,
        BitAnd,
        BitOr,
        BitNot,
        BitShiftLeft,
        BitShiftRight,
        BitXOr
    }

    public class ClauseOperationElement : ClauseElement
    {
        public ClauseElement LeftElement { get; set; }
        public ClauseElement RightElement { get; set; }
        public EMathOperator Operator { get; set; }

        public ClauseOperationElement()
        {
            Operator = EMathOperator.None;
        }

        public override void Validate()
        {
            if (LeftElement == null)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(LeftElement)));
            }
            if (RightElement == null)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(RightElement)));
            }
            if (Operator == EMathOperator.None)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Operator)));
            }
        }
    }

    public class ClauseFunctionElement : ClauseElement
    {
        public string Funcion { get; set; }
        public List<ClauseElement> Parameters { get; set; }

        public override void Validate()
        {
            if (String.IsNullOrWhiteSpace(Funcion))
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Funcion)));
            }
        }
    }

    public class ClauseListElement : ClauseElement
    {
        public List<ClauseValue> Values { get; set; }

        public override void Validate()
        {
            if (Values == null || Values.Count <= 0)
            {
                throw new QueryException(String.Format("Missing Property : {0}", nameof(Values)));
            }
        }
    }

    public enum EBasicOperator
    {
        None,
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanEquals,
        LessThan,
        LessThanEquals,
        IsNull,
        NotNull
    }

    public enum EListOperator
    {
        None,
        In,
        NotIn,
        InRange,
        NotInRange
    }

    public class BaseCondition<T> : Condition<T>
    {
        public ClauseElement LeftElement { get; set; }
        public ClauseElement RightElement { get; set; }
        
        public override void Validate()
        {
            if (LeftElement == null)
            {
                throw new QueryException("Left Clause Element missing.");
            }
            if (RightElement == null)
            {
                throw new QueryException("Right Clause Element missing.");
            }
        }


    }

    public class BasicCondition<T> : BaseCondition<T>
    {
        public EBasicOperator Operator { get; set; }

        public BasicCondition()
        {
            Operator = EBasicOperator.None;
        }

        public override void Validate()
        {
            base.Validate();
            if (Operator == EBasicOperator.None)
            {
                throw new QueryException("Clause operator not set.");
            }
        }

        public override bool IsClosed()
        {
            if (closed)
            {
                return true;
            }
            else
            {
                if (LeftElement != null && RightElement != null)
                {

                }
            }
            return closed;
        }
    }

    public class ListCondition<T> : BaseCondition<T>
    {
        
        public EListOperator Operator { get; set; }

        public ListCondition()
        {
            Operator = EListOperator.None;
        }

        public void Close()
        {
            Contract.Requires(!closed);
            closed = true;
        }

        public void AddValue(ClauseValue value)
        {
            Contract.Requires(value != null);
            
            if (RightElement == null)
            {
                RightElement = new ClauseListElement();
            }
            ClauseListElement elem = (ClauseListElement)RightElement;
            if (elem.Values == null)
            {
                elem.Values = new List<ClauseValue>();
            }
            elem.Values.Add(value);
        }

        public override void Validate()
        {
            base.Validate();
            if (LeftElement.GetType() != typeof(ClauseVariable))
            {
                throw new QueryException(String.Format("Expected Left Element to be of type [{0}]", typeof(ClauseVariable).FullName));
            }
            if (RightElement.GetType() != typeof(ClauseListElement))
            {
                throw new QueryException(String.Format("Expected Right Element to be of type [{0}]", typeof(ClauseListElement).FullName));
            }
            if (Operator == EListOperator.None)
            {
                throw new QueryException("Clause operator not set.");
            }
        }
    }
}