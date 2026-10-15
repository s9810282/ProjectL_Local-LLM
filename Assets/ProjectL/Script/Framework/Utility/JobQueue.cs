using System;
using System.Collections.Generic;

namespace ProjectL.Framework.Utility
{
    /// <summary>
    /// 직접 정렬 기준을 주입해서 사용하는 Job Queue.
    /// 
    /// 정렬 규칙:
    /// comparer.Compare(a, b) < 0 이면 a가 b보다 먼저 실행된다.
    /// comparer.Compare(a, b) > 0 이면 b가 a보다 먼저 실행된다.
    /// comparer.Compare(a, b) == 0 이면 먼저 Enqueue된 Job이 먼저 실행된다.
    /// 
    /// 내부 구현:
    /// Binary Heap.
    /// Enqueue / Dequeue: O(log n)
    /// Remove / Refresh: O(n + log n)
    /// </summary>
    public sealed class JobQueue<TJob>
    {
        private struct Entry
        {
            public TJob Job;
            public long Sequence;

            public Entry(TJob job, long sequence)
            {
                Job = job;
                Sequence = sequence;
            }
        }

        private readonly List<Entry> _heap;
        private IComparer<TJob> _sortRule;
        private long _nextSequence;

        public int Count
        {
            get { return _heap.Count; }
        }

        public bool IsEmpty
        {
            get { return _heap.Count == 0; }
        }

        public JobQueue(IComparer<TJob> sortRule)
            : this(sortRule, 0)
        {
        }

        public JobQueue(IComparer<TJob> sortRule, int capacity)
        {
            if (sortRule == null)
                throw new ArgumentNullException(nameof(sortRule));

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _sortRule = sortRule;
            _heap = new List<Entry>(capacity);
            _nextSequence = 0;
        }

        public void Enqueue(TJob job)
        {
            Entry entry = new Entry(job, _nextSequence);
            _nextSequence++;

            _heap.Add(entry);
            SiftUp(_heap.Count - 1);
        }

        public void EnqueueRange(IEnumerable<TJob> jobs)
        {
            if (jobs == null)
                throw new ArgumentNullException(nameof(jobs));

            foreach (TJob job in jobs)
            {
                Entry entry = new Entry(job, _nextSequence);
                _nextSequence++;
                _heap.Add(entry);
            }

            Rebuild();
        }

        public TJob Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("JobQueue is empty.");

            return DequeueEntry().Job;
        }

        public bool TryDequeue(out TJob job)
        {
            if (_heap.Count == 0)
            {
                job = default(TJob);
                return false;
            }

            job = DequeueEntry().Job;
            return true;
        }

        public TJob Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("JobQueue is empty.");

            return _heap[0].Job;
        }

        public bool TryPeek(out TJob job)
        {
            if (_heap.Count == 0)
            {
                job = default(TJob);
                return false;
            }

            job = _heap[0].Job;
            return true;
        }

        public void Clear()
        {
            _heap.Clear();
            _nextSequence = 0;
        }

        public bool Contains(TJob job)
        {
            return Contains(job, EqualityComparer<TJob>.Default);
        }

        public bool Contains(TJob job, IEqualityComparer<TJob> equalityComparer)
        {
            if (equalityComparer == null)
                equalityComparer = EqualityComparer<TJob>.Default;

            for (int i = 0; i < _heap.Count; i++)
            {
                if (equalityComparer.Equals(_heap[i].Job, job))
                    return true;
            }

            return false;
        }

        public bool Remove(TJob job)
        {
            return Remove(job, EqualityComparer<TJob>.Default);
        }

        public bool Remove(TJob job, IEqualityComparer<TJob> equalityComparer)
        {
            if (equalityComparer == null)
                equalityComparer = EqualityComparer<TJob>.Default;

            for (int i = 0; i < _heap.Count; i++)
            {
                if (equalityComparer.Equals(_heap[i].Job, job))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Job 내부 값이 바뀌었을 때 해당 Job의 위치를 다시 정렬한다.
        /// 예: Delay, Priority, Speed 같은 정렬 기준 값이 변경된 경우.
        /// </summary>
        public bool Refresh(TJob job)
        {
            return Refresh(job, EqualityComparer<TJob>.Default);
        }

        public bool Refresh(TJob job, IEqualityComparer<TJob> equalityComparer)
        {
            if (equalityComparer == null)
                equalityComparer = EqualityComparer<TJob>.Default;

            for (int i = 0; i < _heap.Count; i++)
            {
                if (equalityComparer.Equals(_heap[i].Job, job))
                {
                    int newIndex = SiftUp(i);
                    SiftDown(newIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 모든 Job을 현재 정렬 기준으로 다시 정렬한다.
        /// 여러 Job의 정렬 기준 값이 한꺼번에 바뀐 경우 사용한다.
        /// </summary>
        public void Rebuild()
        {
            for (int i = (_heap.Count / 2) - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        /// <summary>
        /// 정렬 기준을 교체한다.
        /// 교체 직후 전체 큐를 다시 정렬한다.
        /// </summary>
        public void SetSortRule(IComparer<TJob> sortRule)
        {
            if (sortRule == null)
                throw new ArgumentNullException(nameof(sortRule));

            _sortRule = sortRule;
            Rebuild();
        }

        /// <summary>
        /// 현재 큐의 실행 순서 스냅샷을 반환한다.
        /// 실제 큐는 변경하지 않는다.
        /// UI 타임라인 표시용으로 사용할 수 있다.
        /// </summary>
        public List<TJob> GetSortedSnapshot()
        {
            JobQueue<TJob> copy = new JobQueue<TJob>(_sortRule, _heap.Count);

            for (int i = 0; i < _heap.Count; i++)
            {
                copy._heap.Add(_heap[i]);
            }

            copy._nextSequence = _nextSequence;
            copy.Rebuild();

            List<TJob> result = new List<TJob>(_heap.Count);

            while (copy.TryDequeue(out TJob job))
            {
                result.Add(job);
            }

            return result;
        }

        private Entry DequeueEntry()
        {
            Entry root = _heap[0];

            int lastIndex = _heap.Count - 1;
            Entry last = _heap[lastIndex];

            _heap.RemoveAt(lastIndex);

            if (_heap.Count > 0)
            {
                _heap[0] = last;
                SiftDown(0);
            }

            return root;
        }

        private void RemoveAt(int index)
        {
            int lastIndex = _heap.Count - 1;

            if (index == lastIndex)
            {
                _heap.RemoveAt(lastIndex);
                return;
            }

            Entry last = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            _heap[index] = last;

            int newIndex = SiftUp(index);
            SiftDown(newIndex);
        }

        private int SiftUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (ComesBeforeOrSame(_heap[parentIndex], _heap[index]))
                    break;

                Swap(parentIndex, index);
                index = parentIndex;
            }

            return index;
        }

        private int SiftDown(int index)
        {
            int count = _heap.Count;

            while (true)
            {
                int leftChildIndex = index * 2 + 1;
                int rightChildIndex = leftChildIndex + 1;

                if (leftChildIndex >= count)
                    break;

                int bestChildIndex = leftChildIndex;

                if (rightChildIndex < count &&
                    ComesBefore(_heap[rightChildIndex], _heap[leftChildIndex]))
                {
                    bestChildIndex = rightChildIndex;
                }

                if (ComesBeforeOrSame(_heap[index], _heap[bestChildIndex]))
                    break;

                Swap(index, bestChildIndex);
                index = bestChildIndex;
            }

            return index;
        }

        private bool ComesBefore(Entry left, Entry right)
        {
            int compare = _sortRule.Compare(left.Job, right.Job);

            if (compare < 0)
                return true;

            if (compare > 0)
                return false;

            return left.Sequence < right.Sequence;
        }

        private bool ComesBeforeOrSame(Entry left, Entry right)
        {
            int compare = _sortRule.Compare(left.Job, right.Job);

            if (compare < 0)
                return true;

            if (compare > 0)
                return false;

            return left.Sequence <= right.Sequence;
        }

        private void Swap(int a, int b)
        {
            Entry temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
        }
    }
}
