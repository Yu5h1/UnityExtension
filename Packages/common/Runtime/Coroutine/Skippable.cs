using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    using WaitForSecondsEx = YieldInstructionExtension.WaitForSeconds;
    public class Skippable : CustomYieldInstruction 
    {
        protected Func<bool> keepWaitingCheck;

        public Skippable(Func<bool> condition)
        {
            this.keepWaitingCheck = condition ?? throw new ArgumentNullException(nameof(condition));
        }
        public override bool keepWaiting => keepWaitingCheck();

        public static Skippable WaitForSeconds(float duration, Func<bool> keepWaitingCheck) 
        {
            return new Skippable<WaitForSecondsEx>(new WaitForSecondsEx(duration),keepWaitingCheck);
        }
 
    }
    public class Skippable<T> : Skippable where T : CustomYieldInstruction
    {
        private T instruction;

        public Skippable(T instruction, Func<bool> keepWaitingCheck) : base(keepWaitingCheck)
        {
            this.instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
        }

        public override bool keepWaiting => keepWaitingCheck() && instruction.keepWaiting;

        public T WrappedInstruction => instruction;
        public bool WasSkipped => keepWaitingCheck() && instruction.keepWaiting;
        public bool IsCompleted => !keepWaiting;

    }
}