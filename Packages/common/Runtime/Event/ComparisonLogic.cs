using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public class ComparisonLogic : Logic
    {
        public enum Operator
        {
            Equal,
            NotEqual,
            Greater,
            Less,
            GreaterOrEqual,
            LessOrEqual
        }

        public Operator @operator;
        [Inline(true)] public ParameterObject referenceValue;

        public override bool Evaluate(object value)
        {
            if (value is not IComparable a || referenceValue == null)
                return false;

            var b = referenceValue.GetValueType().IsValueType
                ? Convert.ChangeType(referenceValue, referenceValue.GetValueType())
                : (object)referenceValue;

            if (b is not IComparable comparable)
                return false;

            int result = a.CompareTo(comparable);

            return @operator switch
            {
                Operator.Equal => result == 0,
                Operator.NotEqual => result != 0,
                Operator.Greater => result > 0,
                Operator.Less => result < 0,
                Operator.GreaterOrEqual => result >= 0,
                Operator.LessOrEqual => result <= 0,
                _ => false
            };
        }
    }
}
