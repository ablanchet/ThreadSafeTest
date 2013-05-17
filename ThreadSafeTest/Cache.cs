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

        #region Singleton business
        private Cache()
        {
            if( !Directory.Exists( Configuration.ResourceDirectory ) )
            {
                Directory.CreateDirectory( Configuration.ResourceDirectory );
            }
            _cachePath = Path.Combine( Configuration.ResourceDirectory, Configuration.CacheFileName );
        }

        // type initializer
        static Cache()
        {
            Instance = new Cache();
        }

        public static Cache Instance { get; private set; }
        #endregion

        public void CleanCache()
        {
            // double check locking around
            // the cache deletion
            if( IsCacheReady() )
            {
                lock( Instance )
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

        public string GetCacheContent( FakeDatabase database )
        {
            // double check locking around 
            // the cache building
            if( !IsCacheReady() )
            {
                lock( Instance )
                {
                    if( !IsCacheReady() )
                    {
                        BuildCache( database );
                    }
                }
            }
            // now we are sure the cache exists
            // then let the thread access the cache 
            return AccessCache();
        }

        bool IsCacheReady()
        {
            return File.Exists( _cachePath );
        }

        void BuildCache( FakeDatabase database )
        {
            Thread.Sleep( 5000 );
            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "Cache built" );
        }

        string AccessCache()
        {
            Console.WriteLine( "Cache accessed" );
            return File.ReadAllText( _cachePath );
        }
    }
}
