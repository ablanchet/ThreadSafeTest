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
        readonly string _cachePath;
        readonly object _readlock;
        readonly object _writeLock;

        #region Singleton business
        private Cache()
        {
            if( !Directory.Exists( Configuration.ResourceDirectory ) ) Directory.CreateDirectory( Configuration.ResourceDirectory );

            _cachePath = Path.Combine( Configuration.ResourceDirectory, Configuration.CacheFileName );
            _readlock = new object();
            _writeLock = new object();
        }

        // type initializer
        static Cache()
        {
            Instance = new Cache();
        }

        public static Cache Instance { get; private set; }
        #endregion

        public void Clean()
        {
            // double check locking around
            // the cache deletion
            if( IsCacheReady() )
            {
                // this lock avoid deleting cache while requesting it
                // and avoid deleting the cache while it's already been deleting by another thread
                lock( _readlock )
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

        public string GetContent( FakeDatabase database )
        {
            // double check locking around 
            // the cache building
            if( !IsCacheReady() )
            {
                // this lock avoid building an already in built cache
                lock( _writeLock )
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


        private bool IsCacheReady()
        {
            return File.Exists( _cachePath );
        }

        private void Build( FakeDatabase database )
        {
            Thread.Sleep( 5000 );
            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "Cache built" );
        }

        private string RetrieveData()
        {
            // lock the cache while reading it
            // to avoid other thread to delete it while reading
            lock( _readlock )
            {
                Console.WriteLine( "Cache accessed" );
                return File.ReadAllText( _cachePath );
            }
        }
    }
}
