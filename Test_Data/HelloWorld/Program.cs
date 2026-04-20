using System;

namespace HelloWorld
{
	internal class Program
	{
		static void Main ( string [ ] args )
		{
			UTL.ShowBOJMessage ( );

			int intTime2Wait = 10;
			Console.WriteLine ( $"Pausing for {intTime2Wait} seconds" );
			System.Threading.Thread.Sleep ( intTime2Wait * 1000 );
			Console.WriteLine ( $"Paused for {intTime2Wait} seconds" );
			UTL.ShowEOJMessage ( );
		}   // static void Main
	}   // internal class Program
}   // partial namespace HelloWorld