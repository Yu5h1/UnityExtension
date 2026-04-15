using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class Collider2DEx
    {
        public static void IgnoreCollisionWith(this Tags tags, params Collider2D[] colliders)
        {
            foreach (var obj in tags.FindMatchedGameObjects())
                foreach (var tagged in obj.GetComponents<Collider2D>())
                    foreach (var collider in colliders)
                        Physics2D.IgnoreCollision(tagged, collider);
        }
    }
}
