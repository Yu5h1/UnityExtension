using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    using WaitForSecondsEx = YieldInstructionExtension.WaitForSeconds;
    public class Skippable : CustomYieldInstruction 
    {
        protected Func<bool> condition;

        public Skippable(Func<bool> condition)
        {
            this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }
        public override bool keepWaiting => !condition();

        public static Skippable WaitForSeconds(float duration, Func<bool> condition) 
        {
            return new Skippable<WaitForSecondsEx>(new WaitForSecondsEx(duration),condition);
        }
 
    }
    public class Skippable<T> : Skippable where T : CustomYieldInstruction
    {
        private T instruction;

        public Skippable(T instruction, Func<bool> condition) : base(condition)
        {
            this.instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
        }

        public override bool keepWaiting => !condition() && instruction.keepWaiting;

        public T WrappedInstruction => instruction;
        public bool WasSkipped => condition() && instruction.keepWaiting;
        public bool IsCompleted => !keepWaiting;

    }
}