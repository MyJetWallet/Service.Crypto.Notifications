using System;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.Crypto.Notifications.Client;
using Service.Crypto.Notifications.Deduplication;
using Service.Crypto.Notifications.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            var lruCache = new LruCache<string, string>(3, x => x);
            lruCache.AddItem("1");
            lruCache.AddItem("2");
            lruCache.AddItem("3");
            PrintLruCache(lruCache);
            lruCache.AddItem("1");
            PrintLruCache(lruCache);
            lruCache.AddItem("4");
            PrintLruCache(lruCache);

            Console.WriteLine("End");
            Console.ReadLine();
        }

        static void PrintLruCache(LruCache<string, string> cache)
        {
            Console.WriteLine();
            foreach (var cacheItem in cache)
            {
                Console.WriteLine(cacheItem);
            }
            Console.WriteLine();
        }
    }
}
