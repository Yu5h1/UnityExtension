using UnityEngine;
using Type = System.Type;

public class AutoFillAttribute : PropertyAttribute
{
	public Type itemsType;
	public object[] Args;

    public AutoFillAttribute(Type autoFillItemsType,params object[] args)
    {
        itemsType = autoFillItemsType;
        Args = args;

    }

    public abstract class Items
	{
		public abstract string[] Get(params object[] args);
	}
}
