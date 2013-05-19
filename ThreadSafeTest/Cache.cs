using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeTest
{
    public class Cache
    {
        static string _cachePath;
        static object _lock;

        static CountdownEvent _readCount = new CountdownEvent( 0 );

        static Cache()
        {
            if( !Directory.Exists( Configuration.ResourceDirectory ) ) Directory.CreateDirectory( Configuration.ResourceDirectory );

            _cachePath = Path.Combine( Configuration.ResourceDirectory, Configuration.CacheFileName );

            _lock = new object();
        }

        public static void Clean()
        {
            // double check locking around
            // the cache deletion
            if( IsCacheReady() )
            {
                _readCount.Wait();
                lock( _lock )
                {
                    if( IsCacheReady() )
                    {
                        File.Delete( _cachePath );
                        Console.WriteLine( "Cache cleaned" );
                    }
                }
            }
        }

        public static string GetContent( FakeDatabase database )
        {
            CreateCache( database );

            // now we are sure the cache exists
            // then let the thread access the cache 

            return RetrieveData();
        }

        private static void CreateCache( FakeDatabase database )
        {
            // double check locking around 
            // the cache building
            if( !IsCacheReady() )
            {
                lock( _lock )
                {
                    if( !IsCacheReady() )
                    {
                        Build( database );
                    }
                }
            }
        }

        private static bool IsCacheReady()
        {
            return File.Exists( _cachePath );
        }

        private static void Build( FakeDatabase database )
        {
            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "Cache built" );
        }

        private static string RetrieveData()
        {
            if( !_readCount.TryAddCount() ) _readCount.Reset( 1 );

            Console.WriteLine( "Thread {0} is reading data, read count is at {1}", Thread.CurrentThread.ManagedThreadId, _readCount.CurrentCount );
            string data = File.ReadAllText( _cachePath );
            
            _readCount.Signal();

            return data;
        }
    }
}
