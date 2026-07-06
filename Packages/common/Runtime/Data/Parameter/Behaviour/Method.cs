using UnityEngine;

namespace Yu5h1Lib.Parameter
{
    public class Method : ParameterBehaviour, IParameter
    {
        [SerializeField] private string _memberName;
        public override string memberName { get => _memberName; protected set => _memberName = value; }

        [SerializeField] private Object parameter;

        public override void ApplyTo(Object target)
        {
            var value = GetValue();
            var type = value?.GetType() ?? typeof(object);
            ParameterMember.Apply(target, memberName, value, type);
        }

        public override object GetValue()
        {
            if (parameter is IParameter param)
                return param.GetValue();

            return null;
        }
    }
}
