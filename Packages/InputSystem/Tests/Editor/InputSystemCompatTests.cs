using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Verifies legacy <see cref="KeyCode"/> to Input System <see cref="Key"/> mappings in <see cref="InputSystemCompat"/>.
    /// </summary>
    public class InputSystemCompatTests
    {
        [Test]
        public void Numlock_MapsToInputSystemNumLock()
        {
            Assert.IsTrue(InputSystemCompat.IsKeyCodeValid(KeyCode.Numlock), "KeyCode.Numlock must map to a valid InputSystem Key.");
            Assert.AreEqual(Key.NumLock, InputSystemCompat.KeyCodeToKey(KeyCode.Numlock));
        }
    }
}
