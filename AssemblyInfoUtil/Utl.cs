/*
    ============================================================================

    Module Name:        Utl.cs

    Namespace Name:     AssemblyInfoUtil

    Class Name:         Utl

    Synopsis:           This static class exposes utility methods for use by the
                        console programs that incorporate it.

    Remarks:            These proven routines were copied from JSON_Jam.

    Author:             David A. Gray, after Sergiy Korzh

    Created:            Friday, 28 June 2019

    ----------------------------------------------------------------------------
    Revision History
    ----------------------------------------------------------------------------

    Date       Version By Synopsis
    ---------- ------- -- ------------------------------------------------------
    2019/06/29 2.0     DG This class makes its first appearance.
    ============================================================================
*/


using System;
using System.Reflection;
using System.Text;


namespace AssemblyInfoUtil
{
    static class Utl
    {
        private static readonly DateTime s_dtmStart = DateTime.UtcNow;


        /// <summary>
        /// Prompt the user to press the RETURN key to end the program, allowing
        /// it to be launched from the File Explorer.
        /// </summary>
        public static void AwaitCarbonUnit ( )
        {
            Console.Error.WriteLine ( Properties.Resources.MSG_AWAIT_CARBON_UNIT );
            Console.ReadLine ( );
        }   // public static void AwaitCarbonUnit ( )


        /// <summary>
        /// Generate a shutdown message that reports the name of the calling
        /// program, the current time per the system clock, and the wall clock
        /// time consumed by the program.
        /// </summary>
        /// <returns>
        /// The return value is a string that can be displayed on the console or
        /// recorded in a log file.
        /// </returns>
        public static string CreateShutdownBanner ( )
        {
            AssemblyName anTheApp = Assembly.GetEntryAssembly ( ).GetName ( );
            DateTime dtmStopping = DateTime.UtcNow;
            TimeSpan tsRunning = dtmStopping - s_dtmStart;

            return string.Format (
                Properties.Resources.MSG_STOP ,             // The format control string contains five substitution tokens.
                new object [ ]								// Since there are more than three, the format items go into a parameter array.
				{
                    anTheApp.Name ,							// Format Item 0 = Program Name
					dtmStopping.ToLocalTime ( ) ,			// Format Item 1 = Local Program Ending Time
					dtmStopping ,							// Format Item 2 = UTC Program Ending Time
					tsRunning ,								// Format Item 3 = Running time
					Environment.NewLine } );                // Format Item 4 = Embedded Newline
        }   // public static string CreateShutdownBanner ( )


        /// <summary>
        /// Create a startup message that reports the name and version (major
        /// and minor) of the calling program and the current time per the
        /// system clock.
        /// </summary>
        /// <returns>
        /// The return value is a string that can be displayed on the console or
        /// recorded in a log file.
        /// </returns>
        public static string CreateStartupBanner ( )
        {
            AssemblyName anTheApp = Assembly.GetEntryAssembly ( ).GetName ( );

            return string.Format (
                Properties.Resources.MSG_START ,            // The format control string contains six substitution tokens.
                new object [ ]								// Since there are more than three, the format items go into a parameter array.
				{
                    anTheApp.Name ,							// Format Item 0 = Program Name
					anTheApp.Version.Major ,				// Format Item 1 = Major Version Number
					anTheApp.Version.Minor ,				// Format Item 2 = Minor Version Number
					s_dtmStart.ToLocalTime ( ) ,			// Format Item 3 = Local Startup Time
					s_dtmStart ,							// Format Item 4 = UTC Startup Time
					Environment.NewLine						// Format Item 5 = Embedded Newline
				} );
        }   // public static string CreateStartupBanner


        /// <summary>
        /// Format a stack trace so that it prints with subsequent entries
        /// aligned under the first entry.
        /// </summary>
        /// <param name="pstrStackTrace">
        /// StackTrace property on an Exception
        /// </param>
        /// <param name="pintLeadingSpaceCount">
        /// Count of leading spaces to insert between trace items
        /// </param>
        /// <returns>
        /// Pretty stack trace for printing vertically alinged
        /// </returns>
        public static string PrettyTrace (
            string pstrStackTrace , 
            int pintLeadingSpaceCount )
        {
            const string STACK_ITEM_PREFIX = @"at ";
            const int STRINGBUILDER_HEADROOM_FACTOR = WizardWrx.MagicNumbers.PLUS_TWO;
            const int SUBSTRING_POS_INDEXOF_NOT_FOUND = WizardWrx.ListInfo.INDEXOF_NOT_FOUND;

            int intPosFirstItem = pstrStackTrace.IndexOf ( STACK_ITEM_PREFIX );
            int intPosLastItem = intPosFirstItem + STACK_ITEM_PREFIX.Length;
            int intPosNextItem = pstrStackTrace.Length;

            StringBuilder rsb = new StringBuilder ( 
                pstrStackTrace.Substring (
                    intPosFirstItem ,
                    intPosLastItem- intPosFirstItem ) ,
                pstrStackTrace.Length * STRINGBUILDER_HEADROOM_FACTOR );
            string strTracePrefix = STACK_ITEM_PREFIX.PadLeft ( pintLeadingSpaceCount );

            while ( intPosNextItem > SUBSTRING_POS_INDEXOF_NOT_FOUND )
            {
                intPosNextItem = pstrStackTrace.IndexOf (
                    STACK_ITEM_PREFIX ,
                    intPosLastItem );

                if ( intPosNextItem > SUBSTRING_POS_INDEXOF_NOT_FOUND )
                {
                    rsb.Append (
                        pstrStackTrace.Substring (
                            intPosLastItem ,
                            intPosNextItem - intPosLastItem ) );
                    rsb.Append ( strTracePrefix );
                    intPosLastItem = intPosNextItem + STACK_ITEM_PREFIX.Length;
                }   // if ( intPosNextItem > SUBSTRING_POS_INDEXOF_NOT_FOUND )
            }   // while ( intPosNextItem > SUBSTRING_POS_INDEXOF_NOT_FOUND )

            rsb.Append (
                pstrStackTrace.Substring (
                    intPosLastItem ,
                    pstrStackTrace.Length - intPosLastItem ) );
            return rsb.ToString ( );
        }   // public static string PrettyTrace
    }   // static class Utl
}   // namespace AssemblyInfoUtil