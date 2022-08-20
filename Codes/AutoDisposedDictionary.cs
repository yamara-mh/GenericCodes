using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

namespace Yamara
{
    public class AutoDisposedDictionary<TKey, TValue> :
        ICollection<KeyValuePair<TKey, TValue>>,
        IEnumerable,
        IDictionary<TKey, TValue>,
        IReadOnlyCollection<KeyValuePair<TKey, TValue>>,
        IReadOnlyDictionary<TKey, TValue>
    {
        public int MaxItemsCount { get; private set; }
        public int AutoDisposeCount;

        private Dictionary<TKey, TValue> _items;
        private Dictionary<TKey, uint> _history;
        private uint _historyCount;
        private Action<TValue> _disposeAction;

        public AutoDisposedDictionary(int maxItemsCount, int autoDisposeCount, Action<TValue> disposeAction = null)
        {
            if (maxItemsCount >= 1)
            {
                MaxItemsCount = maxItemsCount;
                _items = new Dictionary<TKey, TValue>(maxItemsCount);
                _history = new Dictionary<TKey, uint>(maxItemsCount);
            }
            else
            {
                MaxItemsCount = int.MaxValue;
                _items = new Dictionary<TKey, TValue>();
                _history = new Dictionary<TKey, uint>();
            }

            AutoDisposeCount = autoDisposeCount;
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => _items.Keys;
        public ICollection<TValue> Values => _items.Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public bool ContainsKey(TKey key) => _items.ContainsKey(key);

        public bool Contains(KeyValuePair<TKey, TValue> item)
            => _items.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(_items[item.Key], item.Value);

        public TValue this[TKey key]
        {
            get
            {
                SetHistoryCount(key);
                return _items[key];
            }
            set
            {
                SetHistoryCount(key);
                _items[key] = value;
            }
        }

        public void SetMaxItemsCount(int maxItemsCount)
        {
            if (_items.Count > maxItemsCount) RemoveInUnusedOrder(_items.Count - maxItemsCount);
            MaxItemsCount = maxItemsCount;
        }

#region Add & Get & Copy

        public void Add(TKey key, TValue item)
        {
            _items.Add(key, item);
            SetHistoryCount(key);
            if (_items.Count > MaxItemsCount) RemoveInUnusedOrder(AutoDisposeCount);
        }

        public bool TryAdd(TKey key, TValue item)
        {
            if (ContainsKey(key)) return false;
            _items.Add(key, item);
            SetHistoryCount(key);
            if (_items.Count > MaxItemsCount) RemoveInUnusedOrder(AutoDisposeCount);
            return true;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _items.Add(item.Key, item.Value);
            SetHistoryCount(item.Key);
            if (_items.Count > MaxItemsCount) RemoveInUnusedOrder(AutoDisposeCount);
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => pairs.ToList().ForEach(pair => Add(pair.Key, pair.Value));

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_items.ContainsKey(key))
            {
                SetHistoryCount(key);
                value = _items[key];
                return true;
            }
            value = default;
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex >= _items.Count)
            {
                array = new KeyValuePair<TKey, TValue>[0];
                return;
            }
            array = new KeyValuePair<TKey, TValue>[_items.Count - arrayIndex];

            var list = _items.Skip(arrayIndex).ToList();
            Enumerable.Range(0, list.Count).ToList().ForEach(i => array[i] = list[i]);
        }

        private void SetHistoryCount(TKey key)
        {
            _history[key] = ++_historyCount;
            if (_historyCount == uint.MaxValue)
            {
                _historyCount = 0;
                foreach (var k in _history.OrderBy(kv => kv.Value).Select(kv => kv.Key))
                {
                    _history[k] = ++_historyCount;
                }
            }
        }

        #endregion

#region Remove & Clear

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_items.Contains(item))
            {
                RemoveProcess(item.Key);
                return true;
            }
            return false;
        }
        
        public bool Remove(TKey key)
        {
            if (_items.ContainsKey(key))
            {
                RemoveProcess(key);
                return true;
            }
            return false;
        }

        public void RemoveRange(IEnumerable<TKey> keys) => keys.ToList().ForEach(key => Remove(key));

        public void RemoveInUnusedOrder(int removeCount)
        {
            _history
                .OrderBy(item => item.Value)
                .Take(Math.Min(Math.Max(removeCount, 0), _items.Count))
                .ToList()
                .ForEach(item => RemoveProcess(item.Key));
        }

        public void RemoveInUnusedOrder(double removeRate)
            => RemoveInUnusedOrder((int)Math.Round(_items.Count * removeRate));

        private void RemoveProcess(TKey key)
        {
            _disposeAction?.Invoke(_items[key]);
            _items.Remove(key);
            _history.Remove(key);
        }

        public void Clear()
        {
            _items.Clear();
            _history.Clear();
            _historyCount = 0;
        }

#endregion

    }
}
