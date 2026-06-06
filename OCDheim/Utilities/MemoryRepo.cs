using OCDheim.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCDheim
{
    public interface IMemoryRepo<K, V>
    {
        V LookUp(K key);
    }

    public interface IRefreshableMemoryRepo<K, V> : IMemoryRepo<K, V>, IRefresherable { }

    public class MemoryRepo<K, TK, V> : IMemoryRepo<K, V>
    {
        protected Func<K, V> producer { get; }
        private Dictionary<TK, V> kvs { get; }
        private Func<K, TK> transformer { get; }

        public MemoryRepo(Func<K, V> producer, Func<K, TK> transformer, int capacity)
        {
            this.producer = producer;
            this.transformer = transformer;
            kvs = new Dictionary<TK, V>(capacity);
        }

        public V LookUp(K key)
        {
            var transformedKey = transformer.Invoke(key);
            var miss = !kvs.ContainsKey(transformedKey);
            Logger.Debug(() => $"[MEMORY REPO {( miss ? "MISS" : "HIT")}] on key: {key}");
            if (miss)
            {
                kvs[transformedKey] = ProduceVal(key, transformedKey);
            }

            return kvs[transformedKey];
        }

        public IRefreshableMemoryRepo<K, V> EvictionPolicy(TimeSpan explosionTime) => new TimeBombMemoryRepo(this, explosionTime);

        protected virtual V ProduceVal(K key, TK _) => producer.Invoke(key);

        private class TimeBombMemoryRepo : IRefreshableMemoryRepo<K, V>
        {
            private MemoryRepo<K, TK, V> innerMemoryRepo { get; }
            private TimeSpan explosionTime { get; }
            private Queue<Timed> timeBomb { get; }

            public TimeBombMemoryRepo(MemoryRepo<K, TK, V> innerMemoryRepo, TimeSpan explosionTime)
            {
                timeBomb = new Queue<Timed>();
                this.explosionTime = explosionTime;
                this.innerMemoryRepo = innerMemoryRepo;
            }

            public V LookUp(K key)
            {
                var transformedKey = innerMemoryRepo.transformer.Invoke(key);
                if (!innerMemoryRepo.kvs.ContainsKey(transformedKey))
                {
                    timeBomb.Enqueue(new Timed(transformedKey, Time.time + (float)explosionTime.TotalSeconds));
                }

                return innerMemoryRepo.LookUp(key);
            }

            public void Refresh()
            {
                while (timeBomb.Count > 0 && timeBomb.Peek().explosionTime < Time.time)
                {
                    var boom = timeBomb.Dequeue();
                    innerMemoryRepo.kvs.Remove(boom.key);
                    Logger.Debug(() => $"[MEMORY REPO EVICTION] on key: {boom.key}");
                }
            }
        }

        private struct Timed
        {
            public float explosionTime { get; }
            public TK key { get; }

            public Timed(TK key, float explosionTime)
            {
                this.key = key;
                this.explosionTime = explosionTime;
            }
        }
    }

    // Alias on MemoryRepo<K, TK, V> that requires one less type parameter, which incidentally is the most common scenario.
    public class MemoryRepo<K, V> : MemoryRepo<K, K, V>
    {
        public MemoryRepo(Func<K, V> producer, Func<K, K> transformer, int capacity) : base(producer, transformer, capacity) { }
        public MemoryRepo(Func<K, V> producer, int capacity) : this(producer, key => key, capacity) { }

        protected override V ProduceVal(K _, K transformedKey) => producer.Invoke(transformedKey);
    }
}
