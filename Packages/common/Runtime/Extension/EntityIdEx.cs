using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class EntityIdEx
    {
        public static bool TryParseEntityId(this string id, out EntityId entityId)
        {
            entityId = EntityId.None;

#if UNITY_6000_4_OR_NEWER
            if (!ulong.TryParse(id, out ulong longId))
                return false;
            entityId = EntityId.FromULong(longId);
            return true;
#else
            if (int.TryParse(id , out int rawData))
            {
                entityId = (EntityId)rawData;
                return true;
            }
            return false;
#endif
        }
    }
}
