﻿using System;
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

        static int _tryRead = 0;
        static int _nbRead = 0;
        static int _nbUpgradeableRead = 0;

        public static string GetContent( FakeDatabase database )
        {
            if( IsCacheReady() )
            {
                _readWriteLock.EnterReadLock();
                try
                {
                    Interlocked.Increment( ref _tryRead );// Only for log

                    if( IsCacheReady() )
                    {
                        try
                        {
                            Interlocked.Increment( ref _nbRead );// Only for log
                            Console.WriteLine( "======> \tParallel TryRead : {0}, Read : {1}", _tryRead, _nbRead );
                            Console.WriteLine( "======> \tThread {0} is reading data", Thread.CurrentThread.ManagedThreadId );
                            return GetCacheObject();
                        }
                        finally
                        {
                            Interlocked.Decrement( ref _nbRead );// Only for log
                        }
                    }

                }
                finally
                {
                    Interlocked.Decrement( ref _tryRead );// Only for log
                    _readWriteLock.ExitReadLock();
                }
            }
            
            Console.WriteLine( "======> \tWait at the Upgradeable read gate" );
            _readWriteLock.EnterUpgradeableReadLock();
            try
            {
                Console.WriteLine( "======> \tEnter Upgradeable read" );
                CreateCache( database );
                Interlocked.Increment( ref _nbUpgradeableRead ); // Only for log
                Console.WriteLine( "======> \tParallel Upgradeable read : {0}", _nbUpgradeableRead );
                Console.WriteLine( "======> \tThread {0} is reading data", Thread.CurrentThread.ManagedThreadId );
                return GetCacheObject();
            }
            finally
            {
                Interlocked.Decrement( ref _nbUpgradeableRead ); // Only for log
                _readWriteLock.ExitUpgradeableReadLock();
            }
        }

        private static string GetCacheObject()
        {
            if( !_readWriteLock.IsReadLockHeld && !_readWriteLock.IsUpgradeableReadLockHeld )
                throw new ApplicationException( "IsReadLockHeld = false && IsUpgradeableReadLockHeld = false : Build method must be in reader lock." );

            return File.ReadAllText( _cachePath );
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
            Thread.Sleep( 3000 );
        }
    }
}
