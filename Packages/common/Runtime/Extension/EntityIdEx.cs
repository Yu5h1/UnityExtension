using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class EntityIdEx
    {
          public static ulong ToULong(this EntityId id) => EntityId.ToULong(id);
        public static ulong GetLongId(this Object obj) => obj.GetEntityId().ToULong();
    }
}
