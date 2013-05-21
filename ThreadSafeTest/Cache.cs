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

        readonly static ReaderWriterLockSlim _readWriteLock;

        static Cache()
        {
            if( !Directory.Exists( Configuration.ResourceDirectory ) ) Directory.CreateDirectory( Configuration.ResourceDirectory );

            _cachePath = Path.Combine( Configuration.ResourceDirectory, Configuration.CacheFileName );

            _readWriteLock = new ReaderWriterLockSlim();
        }

        public static void Clean()
        {
            // double check locking around
            // the cache deletion
            if( IsCacheReady() )
            {
                _readWriteLock.EnterWriteLock();
                try
                {
                    if( IsCacheReady() )
                    {
                        File.Delete( _cachePath );
                        Console.WriteLine( "======> \tCache cleaned" );
                    }
                }
                finally
                {
                    _readWriteLock.ExitWriteLock();
                }
            }
        }

        public static string GetContent( FakeDatabase database )
        {
            _readWriteLock.EnterUpgradeableReadLock();
            try
            {
                CreateCache( database );

                // now we are sure the cache exists
                // then let the thread access the cache

                Console.WriteLine( "======> \tThread {0} is reading data", Thread.CurrentThread.ManagedThreadId );
                return File.ReadAllText( _cachePath );
            }
            finally
            {
                _readWriteLock.ExitUpgradeableReadLock();
            }
        }

        private static void CreateCache( FakeDatabase database )
        {
            // double check locking around 
            // the cache building
            if( !IsCacheReady() )
            {
                _readWriteLock.EnterWriteLock();
                try
                {
                    if( !IsCacheReady() )
                    {
                        Build( database );
                    }
                }
                finally
                {
                    _readWriteLock.ExitWriteLock();
                }
            }
        }

        private static bool IsCacheReady()
        {
            return File.Exists( _cachePath );
        }

        private static void Build( FakeDatabase database )
        {
            if( !_readWriteLock.IsWriteLockHeld ) throw new ApplicationException( "IsWriteLockHeld = false : Build method must be in writer lock." );

            using( var stream = File.CreateText( _cachePath ) )
            {
                foreach( string data in database.GetData() ) stream.Write( data + " " );
            }
            Console.WriteLine( "======> \tCache built" );
        }
    }
}
