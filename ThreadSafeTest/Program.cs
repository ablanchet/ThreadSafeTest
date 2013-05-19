using System;
using System.Collections.Concurrent;
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
        static Task GetNewTask( string name, Action<string> action )
        {
            Task task = Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Starting task {0}", name );
                action(name);
            } );

            task.ContinueWith( ( t ) => Console.WriteLine( "Task {0} finished", name ) );
            task.ContinueWith( ( t ) => Console.WriteLine( "Exception in {0} : {1}", name, t.Exception.Flatten() ), TaskContinuationOptions.OnlyOnFaulted );

            return task;
        }

        static void Main( string[] args )
        {
            FakeDatabase database = new FakeDatabase();

            GetNewTask( "Read 1", ( taskName ) =>
            {
                Console.WriteLine( "{0} asking to get the cache", taskName );
                Console.WriteLine( "Content 1 :" + Cache.GetContent( database ) );
            } );

            GetNewTask( "Read 2", ( taskName ) =>
            {
                Console.WriteLine( "{0} asking to get the cache", taskName );
                Console.WriteLine( "Content 2 :" + Cache.GetContent( database ) );
            } ).ContinueWith( ( t ) =>
            {
                // when the task 2 is finished, try to delete the cache from two tasks in parallel
                GetNewTask( "Clean 2", ( taskName ) =>
                {
                    Console.WriteLine( "{0} asking to clean the cache", taskName );
                    Cache.Clean();
                } );

                GetNewTask( "Clean 3", ( taskName ) =>
                {
                    Console.WriteLine( "{0} asking to clean the cache", taskName );
                    Cache.Clean();
                } );

            } );

            // this will not delete anything because the cache is not built yet
            // there is nothing to delete
            GetNewTask( "Clean 1", ( taskName ) =>
            {
                Console.WriteLine( "{0} asking to clean the cache", taskName );
                Cache.Clean();
            } );

            Console.Read();
        }
    }
}
