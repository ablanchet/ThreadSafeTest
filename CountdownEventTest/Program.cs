using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CountdownEventTest
{
    class Program
    {
        static void Main( string[] args )
        {
            CountdownEvent ev = new CountdownEvent( 1 );

            Console.WriteLine( "How many increment do you want to do ?" );
            int incCount = int.Parse( Console.ReadLine() );
            for( int i = 0; i < incCount; i++ )
            {
                Console.WriteLine( "Increment CountdownEvent" );
                ev.AddCount();
                Console.WriteLine( "CountdownEvent now at {0}", ev.CurrentCount );
            }

            Task.Factory.StartNew( () =>
            {
                Console.WriteLine( "Task started" );
                Console.WriteLine( "Task task waiting for countdown" );
                ev.Wait();
                Console.WriteLine( "Task finished" );
            } );

            while( ev.CurrentCount > 0 )
            {
                Console.WriteLine( "Decrement by pressing any key" );
                Console.ReadKey();
                ev.Signal();
                Console.WriteLine( "CountdownEvent now at {0}", ev.CurrentCount );
            }

            Console.WriteLine( "End of program" );
            Console.Read();
        }
    }
}
