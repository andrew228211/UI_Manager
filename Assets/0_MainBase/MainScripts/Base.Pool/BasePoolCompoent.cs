using System;
using System.Collections.Generic;
using Base.Singleton;
using UnityEngine;

namespace Base.Pool
{
    public abstract class BasePoolComponent<T, U> : Singleton<BasePoolComponent<T, U>>
        where T : Enum
        where U : Component
    {
        [System.Serializable]
        public class PoolData
        {
            public T name;
            public U prefab;
            public int size = 0;
            public Transform parent;
        }

        public List<PoolData> pools;
        public Dictionary<T, Queue<U>> poolDictionary = new();
        // O(1) metadata lookup — avoids pools.Find linear search on every pool miss
        private Dictionary<T, PoolData> _poolMeta;

        protected override void OnAwake()
        {
            _poolMeta = new Dictionary<T, PoolData>(pools.Count);
            foreach (var pool in pools)
            {
                _poolMeta[pool.name] = pool;
                if (!poolDictionary.ContainsKey(pool.name))
                    poolDictionary[pool.name] = new Queue<U>();

                for (int i = 0; i < pool.size; i++)
                {
                    U obj = Instantiate(pool.prefab, pool.parent);
                    obj.gameObject.SetActive(false);
                    poolDictionary[pool.name].Enqueue(obj);
                }
            }
        }

        public virtual U GetObject(T eType)
        {
            if (!poolDictionary.ContainsKey(eType))
                poolDictionary[eType] = new Queue<U>();

            if (poolDictionary[eType].Count > 0)
            {
                U obj = poolDictionary[eType].Dequeue();
                obj.gameObject.SetActive(true);
                return obj;
            }

            if (!_poolMeta.TryGetValue(eType, out PoolData data) || data.prefab == null)
            {
                Debug.Log($"Object not found {eType}");
                return null;
            }
            return Instantiate(data.prefab, data.parent);
        }
        /// <summary>
        /// Tra object vao trong pool cua no
        /// </summary>
        /// <param name="eType">enum type</param>
        /// <param name="obj">Component cua object</param>
        public virtual void ReturnObject(T eType, U obj) {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            if (!poolDictionary[eType].Contains(obj))
            {
                poolDictionary[eType].Enqueue(obj);
            }
        }
        public List<U> GetActiveObject()
        {
            List<U> list = new List<U>();
            foreach (var pool in poolDictionary.Values)
            {
                foreach(var e in pool)
                {
                    if (e.gameObject.activeSelf)
                    {
                        list.Add(e);
                    }
                }
            }
            return list;
        }
    }

}