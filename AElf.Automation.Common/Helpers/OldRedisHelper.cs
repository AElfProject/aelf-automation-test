using ServiceStack.Redis;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Automation.Common.Helpers
{
    public class OldRedisHelper
    {
        public RedisManagerPool Manager { get; set; }
        public PooledRedisClientManager Client { get; set; }

        public OldRedisHelper(string host, int port = 6379)
        {
            Manager = new RedisManagerPool($"{host}:{port}");
            Client = new PooledRedisClientManager(0, $"{host}:{port}");
        }

        public T ReadObject<T>(string key)
        {
            using (var client = Manager.GetClient())
            {
                return client.Get<T>(key);
            }
         }

        public List<string> GetAllKeys()
        {
            using (var client = Manager.GetClient())
            {
                return client.GetAllKeys();
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
    }
}
