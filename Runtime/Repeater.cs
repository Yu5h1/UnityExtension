using System;
using System.Collections.Generic;
using System.Text;

namespace Yu5h1Lib.Runtime.Source
{
    using System;

    namespace Yu5h1Lib
    {
        [Serializable]
        public class Repeater
        {
            /// <summary>
            /// -1 = infinite
            /// </summary>
            public int count = -1;

            public int current { get; private set; } = 0;
            public bool isInfinite => count < 0;
            public bool isComplete => !isInfinite && current >= count;

            public event Action onRepeat;
            public event Action onComplete;

            public void Reset() => current = 0;

            public void Invoke()
            {
                if (isComplete)
                    return;

                current++;
                onRepeat?.Invoke();

                if (isComplete)
                    onComplete?.Invoke();
            }
        }
    }
}
