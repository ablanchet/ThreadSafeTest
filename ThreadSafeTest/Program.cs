using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeTest
{
    public static class Configuration
    {
        public static string ResourceDirectory { get { return "resourcedir"; } }

        public static string CacheFileName { get { return "cache.txt"; } }
    }

    public class FakeDatabase
    {
        public IEnumerable<string> GetData()
        {
            yield return "data1";
            yield return "data2";
            yield return "data3";
            yield return "data4";
            yield return "data5";
        }
    }

    class Program
    {
        static void Main( string[] args )
        {
            FakeDatabase database = new FakeDatabase();

            Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task 1 asking to get the cache" );
                Console.WriteLine( "Content 1 :" + Cache.Instance.GetCacheContent( database ) );
            } );
            Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task 2 asking to get the cache" );
                Console.WriteLine( "Content 2 :" + Cache.Instance.GetCacheContent( database ) );
            } );

            Thread.Sleep( 200 );
            
            Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task asking to clean the cache" );
                Cache.Instance.CleanCache();
            } );

            Console.Read();
        }
    }
}
