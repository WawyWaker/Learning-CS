using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DataStructures.HashTable;

public class HashTable<TKey, TValue> : IDictionary<TKey, TValue>
{
	private const int    DEFAULT_CAPACITY    = 32;
	private const double DEFAULT_LOAD_FACTOR = 0.75;
	private const double RESIZE_SCALE        = 0.65;

	private LinkedList<KeyValuePair<TKey, TValue>>[] _buckets;
	private int                  _size;
	private int                  _capacity;

	private readonly double              _loadFactor;
	private readonly HashFunction<TKey> _hashFunc;

	public HashTable(IEnumerable<KeyValuePair<TKey, TValue>> kvs,
					  int initialCapacity, 
					  double loadFactor, 
					  HashFunction<TKey> hashFunc)
	{
		if (kvs == null) throw new NullReferenceException(nameof(kvs));
		if (initialCapacity < 0) throw new ArgumentException(nameof(initialCapacity));
		if (loadFactor < 0) throw new ArgumentException(nameof(loadFactor));

		_buckets = new LinkedList<KeyValuePair<TKey, TValue>>[initialCapacity];
		_size = kvs.Count();
        _capacity = new int[] { initialCapacity, DEFAULT_CAPACITY, kvs.Count()}.Max();
        _loadFactor = loadFactor;
        _hashFunc = hashFunc ?? throw new ArgumentNullException(nameof(hashFunc));

		foreach (var kv in kvs) this.Add(kv);
	}

	public HashTable() : this(Enumerable.Empty<KeyValuePair<TKey, TValue>>(),
							  DEFAULT_CAPACITY,
							  DEFAULT_LOAD_FACTOR, new()) {}

	public HashTable(HashFunction<TKey> hf) : this() { _hashFunc = hf; }

	public HashTable(IEnumerable<KeyValuePair<TKey, TValue>> data, 
					 HashFunction<TKey> hf) : this (data, data.Count(), DEFAULT_LOAD_FACTOR, hf) {}

	private int AdjustIndex(int hash) => hash % _capacity;

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		if (item.Key == null) throw new ArgumentNullException(nameof(item.Key));
		Resize();
		
		int hash = _hashFunc.GetHash(item.Key);
		int bucketIndex = AdjustIndex(hash);

		_buckets[bucketIndex] ??= new();
		_buckets[bucketIndex].AddLast(item);

		_size++;
	}

	private void Resize()
	{
		if (_size <= _capacity * _loadFactor && _size <= _capacity * Math.Pow(_loadFactor, 2)) return;

		var tmp = _buckets.Where(b => b != null).SelectMany(b => b).ToArray();

		if (_capacity >= _capacity * _loadFactor) _capacity = (int)(_capacity / _loadFactor / RESIZE_SCALE);
		if (_capacity <= _capacity * _loadFactor * RESIZE_SCALE) _capacity = (int)(_capacity * _loadFactor);

		_buckets = new LinkedList<KeyValuePair<TKey, TValue>>[_capacity];
		_size = 0;

		foreach (var kv in tmp) this.Add(kv);
	}

	public void Clear()
	{
		_size = 0;
		_buckets = new LinkedList<KeyValuePair<TKey, TValue>>[_capacity];
		Resize();
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		if (_size == 0) return false;
		int hash = _hashFunc.GetHash(item.Key);
		int bucketIndex = AdjustIndex(hash);
		return _buckets[bucketIndex].Contains(item);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) 
		=> _buckets.Where(b => b != null).SelectMany(b => b).ToArray().CopyTo(array, arrayIndex);

	public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

	public int  Count      => _size;
	public bool IsReadOnly => false;
	public void Add(TKey key, TValue value)
	{
		throw new NotImplementedException();
	}

	public bool ContainsKey(TKey key)
	{
		if (key == null) throw new ArgumentNullException(nameof(key));

		int hash = _hashFunc.GetHash(key);
		int adjustedIndex = AdjustIndex(hash);

		var list = _buckets[adjustedIndex];
		if (list == null) return false;
		return BucketFindKey(list, key) != null;
	}

	public bool Remove(TKey key)
	{
		if (key == null) throw new ArgumentNullException(nameof(key));

		int hash          = _hashFunc.GetHash(key);
		int adjustedIndex = AdjustIndex(hash);

		var list          = _buckets[adjustedIndex];
		if (list == null) return false;

		var node          = BucketFindKey(list, key);
		if (node == null) return false;

		_size--;

		list.Remove(node);
		return true;
	}
	
	private LinkedListNode<KeyValuePair<TKey, TValue>>? BucketFindKey(LinkedList<KeyValuePair<TKey, TValue>> list, TKey key)
	{
		if (key == null) throw new ArgumentNullException(nameof(key));
		if (list == null) throw new ArgumentNullException(nameof(key));

		for (var node = list.First; node != null; node = node.Next) 
			if (EqualityComparer<TKey>.Default.Equals(node.Value.Key, key)) return node;
		return null;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		bool comp = ContainsKey(key);
		value = (comp ? this[key] : default)!;
		return comp;
	}

	public TValue this[TKey key]
	{
		get
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			int hash = _hashFunc.GetHash(key);
			int adjustedHash = AdjustIndex(hash);

			if (_buckets[adjustedHash] == null) throw new KeyNotFoundException(nameof(key));

			var ret = _buckets[adjustedHash].FirstOrDefault(node => EqualityComparer<TKey>.Default.Equals(node.Key, key)).Value;
			if (EqualityComparer<TValue>.Default.Equals(ret, default(TValue))) throw new KeyNotFoundException(nameof(key));

			return ret;
		}
		set => Add(new KeyValuePair<TKey, TValue>(key, value));
	}
	
	public ICollection<TKey> Keys => _buckets.Where(b => b != null).SelectMany(b => b.ToArray()).Select(kv => kv.Key).ToList();

	public ICollection<TValue> Values => _buckets.Where(b => b != null).SelectMany(b => b.ToArray()).Select(kv => kv.Value).ToList();
	
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		var enumerable = _buckets.Where(b => b != null).SelectMany(b => b.ToArray());
		return enumerable.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

