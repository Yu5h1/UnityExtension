using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class KeyCodeEvent
    {
        public KeyCode Key;
        public KeyState State;
        public UnityEvent<KeyCodeEvent> Event;

        public override int GetHashCode() => ((int)State << 16) | (int)Key;


        public static KeyCode Parse(int combined,out KeyState state)
        {
            state = (KeyState)(combined >> 16);
            return (KeyCode)(combined & 0xFFFF);
        }
    }
}
