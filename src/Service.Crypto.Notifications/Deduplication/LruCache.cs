using System;
using System.Collections;
using System.Collections.Generic;

namespace Service.Crypto.Notifications.Deduplication
{
    public class LruCache<TKey, TNodeValue> : IEnumerable<TNodeValue>
    {
        private readonly Dictionary<TKey, LinkedListNode<TNodeValue>> _dictionary;
        private readonly LinkedList<TNodeValue> _linkedList = new LinkedList<TNodeValue>();
        private readonly int _capacity;
        private readonly object _locker = new object();
        private readonly Func<TNodeValue, TKey> _keyExtractor;

        public LruCache(int capacity, Func<TNodeValue, TKey> keyExtractor)
        {
            _keyExtractor = keyExtractor;
            _capacity = capacity;
            _dictionary = new Dictionary<TKey, LinkedListNode<TNodeValue>>(_capacity);
        }

        public bool TryGetItemByKey(TKey key, out TNodeValue value)
        {
            lock (_locker)
            {
                if (_dictionary.TryGetValue(key, out var result))
                {
                    value = result.Value;

                    return true;
                }
            }

            value = default(TNodeValue);

            return false;
        }

        public void AddItem(TNodeValue item)
        {
            var key = _keyExtractor(item);

            lock (_locker)
            {
                if (!_dictionary.ContainsKey(key))
                {
                    if (_linkedList.Count == _capacity)
                    {
                        var lastDictKey = _keyExtractor(_linkedList.Last.Value);
                        _linkedList.RemoveLast();
                        _dictionary.Remove(lastDictKey);
                    }

                    var newNode = _linkedList.AddFirst(item);
                    _dictionary.Add(key, newNode);
                }
                else
                {
                    var node = _dictionary[key];
                    node.Value = item;

                    _linkedList.Remove(node);
                    _linkedList.AddFirst(node);
                }
            }
        }

        public IEnumerator<TNodeValue> GetEnumerator()
        {
            return _linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
