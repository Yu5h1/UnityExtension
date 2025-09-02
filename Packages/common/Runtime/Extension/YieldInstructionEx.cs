using UnityEngine;


namespace Yu5h1Lib.YieldInstructionExtension
{
    public class WaitForSeconds : CustomYieldInstruction
    {
        private float waitTime;
        private float startTime;

        public WaitForSeconds(float seconds)
        {
            waitTime = seconds;
            startTime = Time.time;
        }
        public override bool keepWaiting => Time.time - startTime < waitTime;
    }    
}
