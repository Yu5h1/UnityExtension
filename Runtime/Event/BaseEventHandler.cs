using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class BaseEventHandler : MonoBehaviour
    {
        public void Log(string content) => content.print();
    }
}
