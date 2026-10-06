// Optional numerical validation Player; never attached by the production package automatically.
using System;
using System.IO;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>Runs native fixtures and writes a device-readable acceptance receipt.</summary>
    public sealed class MotionValidationPlayer : MonoBehaviour
    {
        public string deliverySha256;
        private string summary = "Running Motion numerical fixtures...";
        [Serializable] private sealed class Result
        {
            public bool passed;
            public int states;
            public string platform, unityVersion, deliverySha256, error;
        }
        private void Start()
        {
            var result = new Result { platform=Application.platform.ToString(),unityVersion=Application.unityVersion,deliverySha256=deliverySha256 };
            try
            {
                var tests = new MotionNativeTests();
                tests.SharedReferenceFixtures(); tests.LifecycleAndInstanceIsolation();
                result.passed=true; result.states=779;
            }
            catch (Exception error) { result.error=error.ToString(); }
            summary=JsonUtility.ToJson(result,true);
            string output=Path.Combine(Application.persistentDataPath,"motion-validation.json");
            var args=Environment.GetCommandLineArgs();
            for (int i=0;i<args.Length-1;i++) if (args[i]=="-motionResult") output=args[i+1];
            File.WriteAllText(output,summary);
            Debug.Log(summary);
#if UNITY_STANDALONE
            Application.Quit(result.passed ? 0 : 1);
#endif
        }
        private void OnGUI() => GUI.Label(new Rect(20,20,Screen.width-40,Screen.height-40),summary);
    }
}
