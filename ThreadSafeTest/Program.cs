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
            Task task = new Task( () =>
            {
                Console.WriteLine( "{0} :\t Starting task", name );
                action( name );
            } );

            task.ContinueWith( ( t ) => Console.WriteLine( "{0} :\t Task  finished", name ) );
            task.ContinueWith( ( t ) => Console.WriteLine( "{0} :\t Exception in {0} : {1}", name, t.Exception.Flatten() ), TaskContinuationOptions.OnlyOnFaulted );

            return task;
        }

        static void Main( string[] args )
        {
            FakeDatabase database = new FakeDatabase();
            Cache.Clean();

            int exNb = 0;

            Parallel.For( 0, 10, (i) => 
                {
                    try
                    {
                        Calls( database );
                    }
                    catch( Exception ex ) 
                    {
                        Console.WriteLine( "Ex : {0}", ex );
                        Interlocked.Increment( ref exNb );
                    }
                } );

            Console.WriteLine( "!!!!!!!!! NB EXCEPTION : {0} !!!!!!!!!", exNb );

            Console.ReadLine();
        }

        private static void Calls( FakeDatabase database )
        {
            Task r1 = GetNewTask( "Read 1", ( taskName ) =>
            {
                Console.WriteLine( "{0} :\t Asking to get the cache", taskName );
                Console.WriteLine( "{1} :\t Content : {0}", Cache.GetContent( database ), taskName );
            } );

            Task r2 = GetNewTask( "Read 2", ( taskName ) =>
            {
                Console.WriteLine( "{0} :\t Asking to get the cache", taskName );
                Console.WriteLine( "{1} :\t Content : {0}", Cache.GetContent( database ), taskName );
            } );

            Task continuation = r2.ContinueWith( ( t ) =>
            {
                // when the task 2 is finished, try to delete the cache from two tasks in parallel
                Task c2 = GetNewTask( "Clean 2", ( taskName ) =>
                {
                    Console.WriteLine( "{0} :\t Asking to clean the cache", taskName );
                    Cache.Clean();
                } );

                Task c3 = GetNewTask( "Clean 3", ( taskName ) =>
                {
                    Console.WriteLine( "{0} :\t Asking to clean the cache", taskName );
                    Cache.Clean();
                } );

                Parallel.Invoke( () => c2.Start(), () => c3.Start() );
                Task.WaitAll( new Task[] { c2, c3 } );

            } );

            Parallel.Invoke( () => r1.Start(), () => r2.Start() );

            // this will not delete anything because the cache is not built yet
            // there is nothing to delete
            Task c1 = GetNewTask( "Clean 1", ( taskName ) =>
            {
                Console.WriteLine( "{0} :\t Asking to clean the cache", taskName );
                Cache.Clean();
            } );

            c1.Start();

            Task tm1 = Task.Factory.StartNew( () => Console.WriteLine( "{1} :\t Content : {0}", Cache.GetContent( database ), "Main" ) );

            continuation.Wait();

            Task tm2 = Task.Factory.StartNew( () => Console.WriteLine( "{1} :\t Content : {0}", Cache.GetContent( database ), "Main" ) );

            tm1.Wait();
            tm2.Wait();
        }
    }
}
