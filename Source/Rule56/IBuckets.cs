using System.Collections.Generic;
namespace CombatAI
{
    public class IBuckets<T> where T : IBucketable
    {
        private readonly Dictionary<int, int> bucketIndexByIds = new Dictionary<int, int>();
        private readonly List<T>[]            buckets;
        public readonly  int                  numBuckets;

        public IBuckets(int numBuckets)
        {
            this.numBuckets = numBuckets;
            buckets         = new List<T>[numBuckets];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<T>();
            }
        }

        public int Index
        {
            get;
            private set;
        }

        public int Count
        {
            get => bucketIndexByIds.Count;
        }

        public List<T> Current
        {
            get => buckets[Index];
        }

        public void Add(T item)
        {
            int id = item.UniqueIdNumber;
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
            if (bucketIndexByIds.TryGetValue(id, out int index))
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
            if (bucketIndexByIds.TryGetValue(id, out int index))
            {
                List<T> bucket = buckets[index];
                T       val;
                for (int i = 0; i < bucket.Count; i++)
                {
                    if ((val = bucket[i]).UniqueIdNumber == id)
                    {
                        return val;
                    }
                }
            }
            return default(T);
        }

        public List<T> Next()
        {
            int index = Index;
            Index = (Index + 1) % numBuckets;
            return buckets[index];
        }

        public void Reset()
        {
            Index = 0;
        }

        public void Release()
        {
            Clear();
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = null;
            }
            bucketIndexByIds.Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i].Clear();
            }
            bucketIndexByIds.Clear();
        }

        public List<T> GetAll()
        {
            List<T> result = new List<T>();
            for (int i = 0; i < buckets.Length; i++)
            {
                result.AddRange(buckets[i]);
            }
            return result;
        }

        public void GetAll(List<T> listBuffer)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                listBuffer.AddRange(buckets[i]);
            }
        }
    }
}
