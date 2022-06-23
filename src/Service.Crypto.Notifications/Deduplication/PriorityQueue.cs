using System;

namespace Service.Crypto.Notifications.Deduplication
{
    public class PriorityQueue<TItem>
    {
        private readonly PriorityQueuItem<TItem>[] _heap;

        private int _counter;

        public PriorityQueue(int size)
        {
            _heap = new PriorityQueuItem<TItem>[size];
        }

        public PriorityQueuItem<TItem> Root => _heap[0];

        public int Parent(int index) => (index - 1) / 2;
        public int LeftChild(int index) => 2 * index + 1;
        public int RightChild(int index) => 2 * index + 2;

        public void Push(TItem item, int priority)
        {
            if (_counter == _heap.Length)
                throw new InvalidOperationException("Heap is full :(");

            _counter++;
            _heap[_counter] = new PriorityQueuItem<TItem>
            {
                Priority = priority,
                Item = item,
            };

            SiftUp(_counter);
        }

        public TItem Pop()
        {
            var root = _heap[0];
            _heap[0] = _heap[_counter];
            SiftDown(0);
            _counter--;

            return root.Item;
        }

        private void SiftUp(int index)
        {
            var parentIndex = Parent(index);
            while (index > 0 && _heap[parentIndex].Priority.CompareTo(_heap[index].Priority) < 0)
            {
                Swap(parentIndex, index);
                index = parentIndex;
            }
        }

        private void SiftDown(int index)
        {
            var maxIndex = index;

            while (true)
            {
                var l = LeftChild(maxIndex);
                var r = RightChild(maxIndex);

                if (l < _counter && _heap[l].Priority.CompareTo(_heap[maxIndex].Priority) > 0)
                    maxIndex = l;

                if (r < _counter && _heap[r].Priority.CompareTo(_heap[maxIndex].Priority) > 0)
                    maxIndex = r;

                if (maxIndex == index)
                    return;

                Swap(maxIndex, index);
                index = maxIndex;
            }
        }

        private void Swap(int from, int to)
        {
            var temp = _heap[from];

            _heap[from] = _heap[to];
            _heap[to] = temp;
        }
    }

    public struct PriorityQueuItem<TItem>
    {
        public TItem Item { get; set; }

        public int Priority { get; init; }
    }
}
