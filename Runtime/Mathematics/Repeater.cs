using System;
using System.Collections.Generic;
using System.Text;

namespace Yu5h1Lib
{
    public class Repeater
    {
        public int count;
        public int current { get; private set; }

        public bool IsComplete => count >= 0 && current >= count;

        public event Action OnRepeat;
        public event Action OnComplete;

        public void Trigger()
        {
            if (IsComplete) return;
            current++;
            OnRepeat?.Invoke();
            if (IsComplete) OnComplete?.Invoke();
        }

        public void Reset() => current = 0;
    }
}
