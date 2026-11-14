using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   First-fit allocator of contiguous index ranges inside a fixed capacity. Freed ranges merge
    ///   with their neighbours, so an allocate-and-free cycle returns <see cref="Used"/> to the same
    ///   value however often it repeats (ParticleWorld WORLD-05).
    /// </summary>
    internal sealed class ParticleRangeAllocator
    {
        readonly List<RangeInt> _free = new List<RangeInt>();

        public ParticleRangeAllocator(int capacity)
        {
            Capacity = Mathf.Max(0, capacity);
            if (Capacity > 0)
                _free.Add(new RangeInt(0, Capacity));
        }

        public int Capacity { get; }
        public int Used { get; private set; }
        public int Remaining => Capacity - Used;

        /// <summary>
        ///   Reserves <paramref name="count"/> contiguous indices. A count of 0 always succeeds with
        ///   an empty range at 0.
        /// </summary>
        public bool TryAllocate(int count, out RangeInt range)
        {
            range = new RangeInt(0, 0);
            if (count == 0)
                return true;
            for (int i = 0; i < _free.Count; i++)
            {
                RangeInt block = _free[i];
                if (block.length < count)
                    continue;
                range = new RangeInt(block.start, count);
                if (block.length == count)
                    _free.RemoveAt(i);
                else
                    _free[i] = new RangeInt(block.start + count, block.length - count);
                Used += count;
                return true;
            }
            return false;
        }

        public void Free(RangeInt range)
        {
            if (range.length == 0)
                return;
            int index = 0;
            while (index < _free.Count && _free[index].start < range.start)
                index++;
            _free.Insert(index, range);
            Used -= range.length;
            if (index + 1 < _free.Count && _free[index].end == _free[index + 1].start)
            {
                _free[index] = new RangeInt(_free[index].start, _free[index].length + _free[index + 1].length);
                _free.RemoveAt(index + 1);
            }
            if (index > 0 && _free[index - 1].end == _free[index].start)
            {
                _free[index - 1] = new RangeInt(_free[index - 1].start, _free[index - 1].length + _free[index].length);
                _free.RemoveAt(index);
            }
        }
    }

    /// <summary>
    ///   Hands out non-zero particle group numbers and reuses released ones.
    /// </summary>
    internal sealed class ParticleGroupAllocator
    {
        readonly Stack<int> _released = new Stack<int>();
        int _next = 1;

        public int Allocate() => _released.Count > 0 ? _released.Pop() : _next++;

        public void Release(int group)
        {
            if (group > 0)
                _released.Push(group);
        }
    }
}
