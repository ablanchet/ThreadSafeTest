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

        static int _readAccessCount;

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
                // this lock avoid deleting cache while requesting it
                // and avoid deleting the cache while it's already been deleting by another thread
                lock( _lock )
                {
                    if( IsCacheReady() )
                    {
                        Thread.Sleep( 2000 );
                        File.Delete( _cachePath );
                        Console.WriteLine( "Cache cleaned" );
                    }
                }
            }
        }

        public static string GetContent( FakeDatabase database )
        {
            // double check locking around 
            // the cache building
            if( !IsCacheReady() )
            {
                // this lock avoid building an already in built cache
                lock( _lock )
                {
                    if( !IsCacheReady() )
                    {
                        Build( database );
                    }
                }
            }
            // now we are sure the cache exists
            // then let the thread access the cache 
            return RetrieveData();
        }


        private static bool IsCacheReady()
        {
            return File.Exists( _cachePath );
        }

        private static void Build( FakeDatabase database )
        {
            Thread.Sleep( 5000 );
            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "Cache built" );
        }

        private static string RetrieveData()
        {
            Console.WriteLine( "Cache accessed" );
            Interlocked.Increment( ref _readAccessCount );
            string data = File.ReadAllText( _cachePath );
            Interlocked.Decrement( ref _readAccessCount );
            return data;
        }
    }
}
