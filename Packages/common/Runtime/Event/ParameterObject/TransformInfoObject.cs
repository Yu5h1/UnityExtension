using UnityEngine;

namespace Yu5h1Lib.Data
{
	public class TransformInfoObject : ParameterObject<OptionalTransformInfo>
    {

        public override void ApplyTo(Object target)
        {
            if (target is Transform t)
                value.ApplyTo(t);
			else
				base.ApplyTo(target);
        }
	}
	//public interface ITransformInfo
	//{
	//	Vector3 position { get; }
 //       Vector3 euler { get; }
 //       Vector3 scale { get; }
 //   }

	[System.Serializable]
	public struct OptionalTransformInfo
	{
		public Optional<Transform> parent;
		public Optional<Vector3> position;
		public Optional<Vector3> euler;
		public Optional<Vector3> scale;

		public void ApplyTo(Transform t)
		{
			if (parent.enabled) t.SetParent(parent.value);
            if (position.enabled) t.localPosition = position.value;
			if (euler.enabled) t.localEulerAngles = euler.value;
			if (scale.enabled) t.localScale = scale.value;

        }
    }
}
