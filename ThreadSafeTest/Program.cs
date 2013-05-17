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
            } ).ContinueWith( ( t ) =>
            {
                Console.WriteLine( "Exception in task 1 : {0}", t.Exception.Flatten() );
            }, TaskContinuationOptions.OnlyOnFaulted );
            Task task2 = Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task 2 asking to get the cache" );
                Console.WriteLine( "Content 2 :" + Cache.Instance.GetCacheContent( database ) );
            } );
            task2.ContinueWith( ( t ) =>
            {
                //asking two parallele deletes
                Task.Factory.StartNew( () =>
                {
                    Console.WriteLine( "Task asking to clean the cache" );
                    Cache.Instance.CleanCache();
                } ).ContinueWith( ( cleanTask ) =>
                {
                    Console.WriteLine( "Exception in task 4 : {0}", cleanTask.Exception.Flatten() );
                }, TaskContinuationOptions.OnlyOnFaulted );

                Task.Factory.StartNew( () =>
                {
                    Console.WriteLine( "Task asking to clean the cache" );
                    Cache.Instance.CleanCache();
                } ).ContinueWith( ( cleanTask ) =>
                {
                    Console.WriteLine( "Exception in task 5 : {0}", cleanTask.Exception.Flatten() );
                }, TaskContinuationOptions.OnlyOnFaulted );
            } );

            task2.ContinueWith( ( t ) =>
            {
                Console.WriteLine( "Exception in task 2 : {0}", t.Exception.Flatten() );
            }, TaskContinuationOptions.OnlyOnFaulted );

            Thread.Sleep( 200 );

            // this will not delete anything
            Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task asking to clean the cache" );
                Cache.Instance.CleanCache();
            } ).ContinueWith( ( t ) =>
            {
                Console.WriteLine( "Exception in task 3 : {0}", t.Exception.Flatten() );
            }, TaskContinuationOptions.OnlyOnFaulted );

            Console.Read();
        }
    }
}
