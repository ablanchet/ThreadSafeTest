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
        static object _cacheBuildLock;
        static object _cacheCleanLock;

        static long _readAccessCount;
        static ManualResetEvent _cleanResetEvent = new ManualResetEvent( true );
        static ManualResetEvent _readResetEvent = new ManualResetEvent( true );

        static Cache()
        {
            if( !Directory.Exists( Configuration.ResourceDirectory ) ) Directory.CreateDirectory( Configuration.ResourceDirectory );

            _cachePath = Path.Combine( Configuration.ResourceDirectory, Configuration.CacheFileName );

            _cacheBuildLock = new object();
            _cacheCleanLock = new object();
        }

        public static void Clean()
        {
            // double check locking around
            // the cache deletion
            if( IsCacheReady() )
            {
                _cleanResetEvent.WaitOne();
                lock( _cacheCleanLock )
                {
                    if( IsCacheReady() )
                    {
                        _readResetEvent.Reset();
                        Thread.Sleep( 2000 );
                        File.Delete( _cachePath );
                        Console.WriteLine( "Cache cleaned" );
                        _readResetEvent.Set();
                    }
                }
            }
        }

        public static string GetContent( FakeDatabase database )
        {
            CreateCache( database );

            _cleanResetEvent.WaitOne();

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
                // this lock avoid building an already in built cache
                lock( _cacheBuildLock )
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
            Thread.Sleep( 5000 );
            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "Cache built" );
        }

        private static string RetrieveData()
        {
            Console.WriteLine( "Cache accessing" );
            Interlocked.Increment( ref _readAccessCount );
            AccessCountChanged();

            string data = File.ReadAllText( _cachePath );

            Interlocked.Decrement( ref _readAccessCount );
            AccessCountChanged();
            Console.WriteLine( "Cache accessed" );

            return data;
        }

        static void AccessCountChanged()
        {
            if( Interlocked.Read( ref _readAccessCount ) == 0 )
                _cleanResetEvent.Set();
            else
                _cleanResetEvent.Reset();
        }
    }
}
