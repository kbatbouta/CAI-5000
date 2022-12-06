using System;
using System.Collections.Generic;
using System.Linq;

namespace CombatAI
{
	public class IBuckets<T> where T : IBucketable
	{
		private int curIndex = 0;

		private readonly List<T>[] buckets;

		private readonly Dictionary<int, int> bucketIndexByIds = new Dictionary<int, int>();

		public readonly int numBuckets;

		public int Index => curIndex;

		public int Count => bucketIndexByIds.Count;

		public List<T> Current => buckets[curIndex];

		public IBuckets(int numBuckets)
		{
			this.numBuckets = numBuckets;
			buckets = new List<T>[numBuckets];
			for (var i = 0; i < buckets.Length; i++) buckets[i] = new List<T>();
		}

		public void Add(T item)
		{
			var id = item.UniqueIdNumber;
			if (!bucketIndexByIds.ContainsKey(id))
			{
				int index;
				buckets[index = item.BucketIndex].Add(item);
				bucketIndexByIds.Add(id, index);
			}
		}

		public void Remove(T item)
		{
			RemoveId(item.UniqueIdNumber);
		}

		public void RemoveId(int id)
		{
			if (bucketIndexByIds.TryGetValue(id, out var index))
			{
				buckets[index].RemoveAll(i => i.UniqueIdNumber == id);
				bucketIndexByIds.Remove(id);
			}
		}

		public bool ContainsId(int id)
		{
			return bucketIndexByIds.ContainsKey(id);
		}

		public List<T> GetBucket(int index)
		{
			return buckets[index];
		}

		public T GetById(int id)
		{
			if (bucketIndexByIds.TryGetValue(id, out var index))
			{
				var bucket = buckets[index];
				T val;
				for (var i = 0; i < bucket.Count; i++)
					if ((val = bucket[i]).UniqueIdNumber == id)
						return val;
			}

			return default;
		}

		public List<T> Next()
		{
			var index = curIndex;
			curIndex = (curIndex + 1) % numBuckets;
			return buckets[index];
		}

		public void Reset()
		{
			curIndex = 0;
		}

		public void Release()
		{
			Clear();
			for (var i = 0; i < buckets.Length; i++) buckets[i] = null;
			bucketIndexByIds.Clear();
		}

		public void Clear()
		{
			for (var i = 0; i < buckets.Length; i++) buckets[i].Clear();
			bucketIndexByIds.Clear();
		}

		public List<T> GetAll()
		{
			var result = new List<T>();
			for (var i = 0; i < buckets.Length; i++) result.AddRange(buckets[i]);
			return result;
		}

		public void GetAll(List<T> listBuffer)
		{
			for (var i = 0; i < buckets.Length; i++) listBuffer.AddRange(buckets[i]);
		}
	}
}