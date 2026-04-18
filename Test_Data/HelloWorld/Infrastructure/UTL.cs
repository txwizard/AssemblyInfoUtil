using System;
using System.Reflection;

namespace HelloWorld
{
	internal static class UTL
	{
		/// <summary>
		/// Use this standard format string to display current local time in
		/// logs and on console.
		/// </summary>
		public const string CURRENT_TIME_FORMAT = @"yyyy/MM/dd HH:mm:ss";


		/// <summary>
		/// Initialize assembly name and version information for use in BOJ and
		/// EOJ messages.
		/// </summary>
		static UTL ( )
		{
			Assembly asm = Assembly.GetExecutingAssembly ( );
			AssemblyName name = asm.GetName ( );
			s_asmName = name.Name;
			s_ver = name.Version;
		}   // static UTL ( ) constructor


		/// <summary>
		/// Display a message on the console indicating the name and version of
		/// the assembly and the time the program started.
		/// </summary>
		/// <returns>
		/// The return value is the banner that was just printed via
		/// Console.WriteLine ( ) to the console. This is returned for use logs.
		/// </returns>
		internal static string  ShowBOJMessage ( )
		{
			string rstrBanner = $"{s_asmName} {s_ver.Major}.{s_ver.Minor}.{s_ver.Build}{Environment.NewLine}Started {ShowUtcTimeAsLocal ( s_dtmStarted )}{Environment.NewLine}";
			Console.WriteLine ( rstrBanner );

			return rstrBanner;
		}   // internal static string  ShowBOJMessage


		/// <summary>
		/// Display a message on the console indicating the name of the assembly
		/// and the time the program ended, along with the total running time.
		/// </summary>
		/// <returns>
		/// The return value is the banner that was just printed via
		/// Console.WriteLine ( ) to the console. This is returned for use logs.
		/// </returns>
		internal static string ShowEOJMessage ( )
		{
			DateTime dtmNow = DateTime.UtcNow;
			string rstrBanner = $"{s_asmName} Ended {ShowUtcTimeAsLocal ( dtmNow )}{Environment.NewLine}Running Time = {( dtmNow - s_dtmStarted ):hh\\:mm\\:ss\\.fff}{Environment.NewLine}";
			Console.WriteLine ( rstrBanner );

			return rstrBanner;
		}   // internal static void ShowEOJMessage


		/// <summary>
		/// Convert the specified UTC time to local time and return it as a
		/// string formatted according to the CURRENT_TIME_FORMAT constant.
		/// </summary>
		/// <param name="pdtm">
		/// This populated DateTime structure is UTC time to convert and format
		/// for display.
		/// </param>
		/// <returns>
		/// The return value is a string representation of the local time
		/// corresponding to the specified UTC time, formatted according to the
		/// CURRENT_TIME_FORMAT constant.
		/// </returns>
		internal static string ShowUtcTimeAsLocal ( DateTime pdtm ) => pdtm.ToLocalTime ( ).ToString ( CURRENT_TIME_FORMAT );

		//	--------------------------------------------------------------------
		//	The following three static readonly fields are initialized in the
		//	static constructor and are used in the BOJ and EOJ messages.
		//	--------------------------------------------------------------------

		static readonly DateTime s_dtmStarted = DateTime.UtcNow;
		static readonly Version s_ver;
		static readonly string s_asmName;
	}   // internal static class UTL
}   // partial namespace HelloWorld