using UnityEngine;

namespace Yu5h1Lib
{
    public class BaseMonoBehaviour : MonoBehaviour
    {
        public virtual void Log(string msg) => msg.print();
    }
}
