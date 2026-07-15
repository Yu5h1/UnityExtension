using UnityEngine;

namespace Yu5h1Lib.Data
{
	public class TransformInfoObject : ParameterObject<OptionalTransformInfo>
    {

        public override void ApplyTo(Object target)
        {
            switch (target)
            {
                case Rigidbody rb: value.ApplyTo(rb); break;   // physics authority — write the body, not the transform
                case Transform t:  value.ApplyTo(t);  break;   // plain transform
                default:           base.ApplyTo(target); break;
            }
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

		/// <summary>
		/// Apply to a physics body: pose goes through the Rigidbody (the authority — setting the
		/// Transform would be overwritten next physics step), velocities are zeroed so it doesn't
		/// drift, while scale/parent stay on the Transform (a Rigidbody has neither).
		/// Stored values are local; rb.position/rotation are world, so they're converted via the parent.
		/// </summary>
		public void ApplyTo(Rigidbody rb)
		{
			var t = rb.transform;
			if (parent.enabled) t.SetParent(parent.value);

			var p = t.parent;
			if (position.enabled)
				rb.position = p ? p.TransformPoint(position.value) : position.value;
			if (euler.enabled)
				rb.rotation = (p ? p.rotation : Quaternion.identity) * Quaternion.Euler(euler.value);

			if (position.enabled || euler.enabled)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}

			if (scale.enabled) t.localScale = scale.value;
		}
    }
}
