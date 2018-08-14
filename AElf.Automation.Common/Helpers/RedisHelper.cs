using NServiceKit.Redis;
using NServiceKit.Redis.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AElf.Automation.Common.Helpers
{
    public interface ICache
    {
        object Get(string key);
        T GetT<T>(string key) where T : class;
        object GetWithDelete(string key);
        T GetWithDelete<T>(string key) where T : class;
        bool Set(string key, object value);
        bool Set(string key, object value, DateTime expireDate);
        bool SetT<T>(string key, T value) where T : class;
        bool SetT<T>(string key, T value, DateTime expire) where T : class;
        bool Remove(string key);
    }

    public class RedisHelper : ICache, IDisposable
    {
        /// <summary>
        /// redis客户端连接池信息
        /// </summary>
        private PooledRedisClientManager prcm;
        public string Host;
        public int Port;

        public RedisHelper(string host, int port = 6379)
        {
            Host = host;
            Port = port;
            CreateManager();
        }
        
        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        private void CreateManager()
        {
            try
            {
                // ip1：端口1,ip2：端口2
                var serverlist = new string[]{ $"{Host}:{Port}"};
                prcm = new PooledRedisClientManager(serverlist, serverlist,
                                 new RedisClientManagerConfig
                                 {
                                     MaxWritePoolSize = 32,
                                     MaxReadPoolSize = 32,
                                     AutoStart = true
                                 });
                //    prcm.Start();
            }
            catch (Exception e)
            {
#if DEBUG
                throw;
#endif
            }
        }
        
        /// <summary>
        /// 客户端缓存操作对象
        /// </summary>
        public IRedisClient GetClient()
        {
            if (prcm == null)
                CreateManager();

            return prcm.GetClient();
        }
        
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            using (var client = prcm.GetClient())
            {
                return client.Remove(key);
            }
        }
        
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Get(string key)
        {
            using (var client = prcm.GetClient())
            {
                var bytes = client.Get<byte[]>(key);
                var obj = new ObjectSerializer().Deserialize(bytes);
                return obj;
            }
        }
        
        /// <summary>
        /// 获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetT<T>(string key) where T : class
        {
            //return Get(key) as T;
            using (var client = prcm.GetClient())
            {
                return client.Get<T>(key);
            }
        }
        
        public List<string> GetAllKeys()
        {
            using (var client = prcm.GetClient())
            {
                return client.GetAllKeys();
            }
        }
        
        /// <summary>
        /// 获取到值到内存中，在删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetWithDelete(string key)
        {
            var result = Get(key);
            if (result != null)
                Remove(key);
            return result;
        }
        
        /// <summary>
        /// 获取到值到内存中，在删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetWithDelete<T>(string key) where T : class
        {
            var result = GetT<T>(key);
            if (result != null)
                Remove(key);
            return result;
        }
        
        /// <summary>
        /// 写
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Set(string key, object value)
        {
            using (var client = prcm.GetClient())
            {
                if (client.ContainsKey(key))
                {
                    return client.Set<byte[]>(key, new ObjectSerializer().Serialize(value));
                }
                else
                {
                    return client.Add<byte[]>(key, new ObjectSerializer().Serialize(value));
                }
            }

        }
        
        /// <summary>
        /// 写
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireTime"></param>
        /// <returns></returns>
        public bool Set(string key, object value, DateTime expireTime)
        {
            using (var client = prcm.GetClient())
            {
                if (client.ContainsKey(key))
                {
                    return client.Set<byte[]>(key, new ObjectSerializer().Serialize(value), expireTime);
                }
                else
                {
                    return client.Add<byte[]>(key, new ObjectSerializer().Serialize(value), expireTime);
                }

            }
        }
        
        /// <summary>
        /// 写
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        public bool SetT<T>(string key, T value, DateTime expire) where T : class
        {
            try
            {
                using (var client = prcm.GetClient())
                {
                    return client.Set<T>(key, value, expire);
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 写
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetT<T>(string key, T value) where T : class
        {
            try
            {
                using (var client = prcm.GetClient())
                {
                    return client.Set<T>(key, value);
                }
            }
            catch
            {
                return false;
            }
        }

        public static List<string> GetIntersection(List<string> list1, List<string> list2)
        {
            return list1.Intersect(list2).ToList<string>();
        }

        public static List<string> GetExceptList(List<string> list1, List<string> list2)
        {
            return list1.Except(list2).ToList<string>();
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            var client = prcm.GetClient();
            prcm.Dispose();
        }
    }

    public class Cache
    {
        private static object cacheLocker = new object();//缓存锁对象
        private static ICache cache = null;//缓存接口
        public static string Host;
        public static int Port;

        public Cache(string host, int port=6379)
        {
            Host = host;
            Port = port;
            Load();
        }

        /// <summary>
        /// 加载缓存
        /// </summary>
        /// <exception cref=""></exception>
        private static void Load()
        {
            try
            {
                cache = new RedisHelper(Host, Port);
            }
            catch (Exception ex)
            {
                //Log.Error(ex.Message);
            }
        }

        public static ICache GetCache()
        {
            return cache;
        }

        /// <summary>
        /// 获得指定键的缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值</returns>
        public static object Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            return cache.Get(key);
        }

        /// <summary>
        /// 获得指定键的缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值</returns>
        public static T GetT<T>(string key) where T : class
        {
            return cache.GetT<T>(key);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        public static void Insert(string key, object data)
        {
            if (string.IsNullOrWhiteSpace(key) || data == null)
                return;
            //lock (cacheLocker)
            {
                cache.Set(key, data);
            }
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        public static void InsertT<T>(string key, T data) where T : class
        {
            if (string.IsNullOrWhiteSpace(key) || data == null)
                return;
            //lock (cacheLocker)
            {
                cache.SetT<T>(key, data);
            }
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        /// <param name="cacheTime">缓存过期时间(分钟)</param>
        public static void Insert(string key, object data, int cacheTime)
        {
            if (!string.IsNullOrWhiteSpace(key) && data != null)
            {
                //lock (cacheLocker)
                {
                    cache.Set(key, data, DateTime.Now.AddMinutes(cacheTime));
                }
            }
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        /// <param name="cacheTime">缓存过期时间(分钟)</param>
        public static void InsertT<T>(string key, T data, int cacheTime) where T : class
        {
            if (!string.IsNullOrWhiteSpace(key) && data != null)
            {
                //lock (cacheLocker)
                {
                    cache.SetT<T>(key, data, DateTime.Now.AddMinutes(cacheTime));
                }
            }
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        /// <param name="cacheTime">缓存过期时间</param>
        public static void Insert(string key, object data, DateTime cacheTime)
        {
            if (!string.IsNullOrWhiteSpace(key) && data != null)
            {
                //lock (cacheLocker)
                {
                    cache.Set(key, data, cacheTime);
                }
            }
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">缓存值</param>
        /// <param name="cacheTime">缓存过期时间</param>
        public static void InsertT<T>(string key, T data, DateTime cacheTime) where T : class
        {
            if (!string.IsNullOrWhiteSpace(key) && data != null)
            {
                //lock (cacheLocker)
                {
                    cache.SetT<T>(key, data, cacheTime);
                }
            }
        }

        /// <summary>
        /// 从缓存中移除指定键的缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        public static void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            lock (cacheLocker)
            {
                cache.Remove(key);
            }
        }
    }
}
