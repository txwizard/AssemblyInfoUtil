/*
    ============================================================================

    Module Name:        Program.cs

    Namespace Name:     AssemblyInfoUtil

    Class Name:         Program

    Synopsis:           This command line utility appends the modified date to
                        the base name of the file specified in its command line.

    Remarks:            AssemblyInformationalVersion is an assembly attribute
                        that is used only by NuGet. Since the NuGet versioning
                        convention is SemVer, the AssemblyInformationalVersion
                        is a three-part version string, and the fourth part may
                        be, and often is, a string that is used to designate a
                        beta or other pre-release package.

                        In the interest of maximum flexibility, this program
                        behaves as follows with respect to this assembly
                        attribute.

                        1)  When AssemblyInformationalVersion exists, its first
                            three substrings, all of which must be numeric, are
                            updated to match the corresponding values in the
                            AssemblyFileVersion attribute when it is updated.

                        2)  When the AssemblyInformationalVersion attribute is
                            missing, it is added and set to the Semantic Version
                            per the AssemblyFileVersion attribute.

                        3)  When the AssemblyFileVersion remains unchanged, so
                            does the AssemblyInformationalVersion. Moreover, the
                            program dispenses with checking for its presence
                            unless the AssemblyFileVersion attribute is being
                            updated.

                        This class module implements the Program class, which is
                        composed of the static void Main method, its sole public
                        method, and all of its dependent routines, so that the
                        entire assembly is implemented as a single static class.

    Author:             David A. Gray, after Sergiy Korzh

    Created:            January 2009

    ----------------------------------------------------------------------------
    Revision History
    ----------------------------------------------------------------------------

    Date       Version By Synopsis
    ---------- ------- -- ------------------------------------------------------
    2009/01/28 1.2     SK This is the version that Sergiy Korzh published with a
                          CodeProject article.

    2019/06/29 2.0     DG This version incorporates many improvements.

                          1) The change that required me to build the assembly
                             in the first place is that the program didn't
                             handle the white space with which I always surround
                             the attributes in AssemblyInfo.cs. The new version
                             supports the same formatting variance in a Visual
                             Basic module, AssemblyInfo.vb.

                          2) A bug that crippled support for updating the major
                             version number is fixed and proven.

                          3) Validation of the version component specified on
                             the "-inc" switch is much more robust. The code now
                             gracefully handles non-numeric version substring
                             ordinals, and enforces the required upper and lower
                             bounds of 0 and 4.

                          4) I added support for restricting updating to the 
                             Assembly or File version, preserving the other. The
                             default, if neither is specified, is the legacy
                             behavior, which updates both.

                          5) I renamed the class and its source file to Program,
                             so that they conform to the generally accepted
                             naming conventions for the entry point class and
                             its source file.

                          6) Move most of the strings, including everything that
                             displays on the console, into managed resources.

    2022/06/05 3.0     DG Implement copyright year fixup for AssemblyCopyright.

                          While I was at it, I upgraded the target framework
                          from 3.5 Client Profile to 4.8.

    2022/06/14 3.1     DG Leave AssemblyInfo.cs unchanged when everything in its
                          directory and its parent directory is unchanged.

    2022/06/19 3.2     DG Display details about changes made to the version and
                          copyright notice attributes, including notes when one
                          or the other is skipped.

    2022/07/09 3.3     DG Implement Support for AssemblyInformationalVersion.
    ============================================================================
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using WizardWrx;


namespace AssemblyInfoUtil
{
    /// <summary>
    /// Class Program defines the entry point routine of a Console application.
    /// </summary>
    class Program
    {
        const string CMD_FIX_ASSEMBLYVERSION = @"-av";
        const string CMD_FIX_FILEVERSION = @"-fv";
        const string CMD_INCREMENT_VERSION_PART = @"-inc:";
        const string CMD_SET_VERSION = @"-set:";
        const string CMD_STOP_WHEN_DONE = @"-stop";
        const string CMD_COPYRIGHT_YEAR = @"-cy";
        const string CMD_CHECK4MODIFIED = @"-onlywhenmodified";

        const int ERR_RUNTIME = 1;                                                  // 0x01
        const int ERR_NO_FILENAME = ERR_RUNTIME + 1;                                // 0x02
        const int ERR_FILE_NOT_FOUND = ERR_NO_FILENAME + 1;                         // 0x03
        const int ERR_INCREMENT_MUST_BE_NUMERIC = ERR_FILE_NOT_FOUND + 1;           // 0x04
        const int ERR_INCREMENT_OUT_OF_RANGE = ERR_INCREMENT_MUST_BE_NUMERIC + 1;   // 0x05
        const int ERR_INVALID_VERSION_SUBSTRING = ERR_INCREMENT_OUT_OF_RANGE + 1;   // 0x06

        const int LEFT_PADDING_FOR_SETTINGS_MESSAGES = 4;

        private static int s_incParamNum = MagicNumbers.ZERO;

        private static string s_InputFileName = SpecialStrings.EMPTY_STRING;
        private static string s_OutputFileName = SpecialStrings.EMPTY_STRING;
        private static string s_VersionStr = null;
        private static string s_CurrentFileVersionString = null;

        private static int s_AssemblyInformationalVersionIndex = ArrayInfo.ARRAY_INVALID_INDEX;

        private static bool s_FixAssemblyVersion = false;
        private static bool s_FixAssemblyFileVersion = false;
        private static bool s_FixCopyrightYear = false;
        private static bool s_OnlyWhenModified = false;
        private static bool s_StopWhenDone = false;
        private static bool s_isLineUnChanged = false;
        private static bool s_isVB = false;


        /// <summary>
        /// Static method Main is the entry point routine of a Console
        /// application.
        /// </summary>
        /// <param name="args">
        /// Main MAY accept the command line arguments as an array of strings,
        /// which MAY be empty, but never NULL.
        /// </param>
        [STAThread]
        static void Main ( string [ ] args )
        {
            Console.WriteLine ( Utl.CreateStartupBanner ( ) );

            if ( ParseCommandLineArgs ( args ) )
            {
                PerformTask ( );
            }   // if ( ParseCommandLineArgs ( args ) )

            Console.WriteLine ( Utl.CreateShutdownBanner ( ) );

            if ( s_StopWhenDone )
            {
                Utl.AwaitCarbonUnit ( );
            }   // if ( s_StopWhenDone )
        }   // static void Main


        /// <summary>
        /// When static Boolean object member s_FixCopyrightYear is TRUE, caller
        /// ProcessLinePart returns through this method to look after the
        /// copyright year (AssemblyCopyright) attribute.
        /// </summary>
        /// <param name="pstrLineIn">
        /// Calling method ProcessLinePart passes in the string that it is about
        /// to return to its caller, ProcessLine, so that it can process the
        /// AssemblyCopyright attribute.
        /// </param>
        /// <returns>
        /// Unless the string is the AssemblyCopyright attribute, the input is
        /// returned unchanged. Otherwise, the AssemblyCopyright is parsed for a
        /// date range, which, if found, is updated to the current year per the
        /// system clock.
        /// </returns>
        private static string CheckCopyrightYear ( string pstrLineIn )
        {
            const string ATTRIBUTE_NAME = @"AssemblyCopyright";
            const string COPYRIGHT_STRING = @"Copyright";

            if ( string.IsNullOrWhiteSpace ( pstrLineIn ) )
            {
                return SpecialStrings.EMPTY_STRING;
            }   // TRUE (degenerate case) block, if ( string.IsNullOrWhiteSpace ( pstrLineIn ) )
            else
            {
                int intPosAttributeName = pstrLineIn.IndexOf ( ATTRIBUTE_NAME );
                int intPosCopyrightString = pstrLineIn.IndexOf ( COPYRIGHT_STRING );

                if ( intPosAttributeName > ListInfo.INDEXOF_NOT_FOUND && intPosCopyrightString > intPosAttributeName )
                {
                    string strOldCopyrightYear = pstrLineIn;

                    int intPosHyphen = pstrLineIn.IndexOf (
                        SpecialCharacters.HYPHEN ,
                        intPosCopyrightString );

                    if ( intPosHyphen > ListInfo.INDEXOF_NOT_FOUND )
                    {
                        int intPosComma = pstrLineIn.IndexOf (
                            SpecialCharacters.COMMA ,
                            intPosHyphen );
                        int intPosSpace = pstrLineIn.IndexOf (
                            SpecialCharacters.SPACE_CHAR ,
                            intPosHyphen );
                        int intPosEnd = intPosComma > ListInfo.INDEXOF_NOT_FOUND
                            ? intPosComma
                            : intPosSpace > ListInfo.INDEXOF_NOT_FOUND
                                ? intPosSpace
                                : ListInfo.INDEXOF_NOT_FOUND;

                        if ( intPosEnd > ListInfo.INDEXOF_NOT_FOUND )
                        {
                            StringBuilder rsb = new StringBuilder (
                                pstrLineIn.Substring (
                                    ListInfo.BEGINNING_OF_BUFFER ,
                                    ArrayInfo.OrdinalFromIndex ( intPosHyphen ) ) ,
                                pstrLineIn.Length );
                            string strLatestCopyrightYear = pstrLineIn.Substring (
                                ArrayInfo.OrdinalFromIndex (
                                    intPosHyphen ) ,
                                intPosEnd - intPosHyphen - MagicNumbers.PLUS_ONE );

                            if ( int.TryParse ( strLatestCopyrightYear , out int intLastCopyrightYear ) )
                            {
                                int intCurrentYear = DateTime.Now.Year;

                                if ( intCurrentYear > intLastCopyrightYear )
                                {
                                    rsb.Append ( intCurrentYear );
                                    rsb.Append ( pstrLineIn.Substring ( intPosEnd ) );
                                    string strNewCopyrighYear = rsb.ToString ( );
                                    Console.WriteLine (
                                        Properties.Resources.MSG_COPYRIGHT_YEAR_CHANGE ,
                                        strOldCopyrightYear ,                   // Format Item 0: Copyright Year Changed: Old Value = {0}
                                        strNewCopyrighYear ,                    // Format Item 1: New Value = {1}
                                        Environment.NewLine );                  // Format Item 2: At the beginning of the first line and at the end of both lines
                                    return strNewCopyrighYear;
                                }   // TRUE (The last copyright year value is out of date.) block, if ( intCurrentYear > intLastCopyrightYear )
                                else
                                {
                                    Console.WriteLine (
                                        Properties.Resources.MSG_COPYRIGHT_YEAR_UNCHANGED ,
                                        strOldCopyrightYear ,                   // Format Item 0: Copyright Year Unchanged: Current Value = {0}
                                        Environment.NewLine );                  // Format Item 1: At the beginning and enf of the line
                                }   // FALSE (The last copyright year is up to date.) block, if ( intCurrentYear > intLastCopyrightYear )
                            }   // TRUE (The copyright year parsed into an integer.) block, if ( int.TryParse ( strLatestCopyrightYear , out int intLastCopyrightYear ) )
                        }   // TRUE (The delimiters appear to be in order.) block, if ( intPosEnd > ListInfo.INDEXOF_NOT_FOUND )
                    }   // TRUE (The copyright year is a hyphenated range.) block, if ( intPosHyphen > ListInfo.INDEXOF_NOT_FOUND )
                    else
                    {
                        Console.WriteLine (
                            Properties.Resources.MSG_COPYRIGHT_YEAR_IS_SINGLE_YEAR ,
                            pstrLineIn ,                                        // Format Item 0: The copyright year is a single year: {0}
                            Environment.NewLine );                              // Format Item 1: Both ends of the line
                    }   // FALSE (The copyright year is a single year, which is unsupported by the current version.) block, if ( intPosHyphen > ListInfo.INDEXOF_NOT_FOUND )
                }   // TRUE (AssemblyCopyright attribute found and appears to be well formed.) block, if ( intPosAttributeName > ListInfo.INDEXOF_NOT_FOUND && intPosCopyrightString > intPosAttributeName )
            }   // FALSE (standard case) block, if ( string.IsNullOrWhiteSpace ( pstrLineIn ) )}

            return pstrLineIn;
        }   // private static string CheckCopyrightYear


        /// <summary>
        /// Display a short help message and return the status code specified in
        /// <paramref name="pintStatusCode"/>.
        /// </summary>
        /// <param name="pintStatusCode">
        /// This signed integer receives the status code to return to the
        /// operating system.
        /// </param>
        private static void DisplayHelpAndSetStatusCode ( int pintStatusCode )
        {
            Console.WriteLine ( Properties.Resources.MSG_USAGE_1 , Environment.NewLine );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_2 , CMD_SET_VERSION );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_3 , CMD_INCREMENT_VERSION_PART );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_4 , CMD_FIX_ASSEMBLYVERSION );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_5 , CMD_FIX_FILEVERSION );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_6 , CMD_COPYRIGHT_YEAR );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_7 , CMD_CHECK4MODIFIED );
            Console.WriteLine ( Properties.Resources.MSG_USAGE_8 , CMD_STOP_WHEN_DONE );

            Environment.ExitCode = pintStatusCode;
        }   // private static void DisplayHelpAndSetStatusCode


        /// <summary>
        /// <para>
        /// Do whatever is needed to implement AssemblyInformationalVersion.
        /// </para>
        /// <para>
        /// If one exists and the FileVersion string was updated, update it with
        /// the new FileVersion string.
        /// </para>
        /// <para>
        /// If there isn't one yet and an updated FileVersion string exists,
        /// create it and initialize it with the current FileVersion string.
        /// </para>
        /// </summary>
        /// <param name="plstAssemblyInfoLines">
        /// Like the routine that calls it, this routine needs to see the whole
        /// list of assembly attributes.
        /// </param>
        private static void HandleAssemblyInformationalVersion ( List<string> plstAssemblyInfoLines )
        {
            if ( !string.IsNullOrEmpty ( s_CurrentFileVersionString ) )
            {   // Have version. Check for InformationalVersion
                string strSemVerString = String.Format (
                    Properties.Resources.IDS_ASSEMBLYINFORMATIONALVERSION_TEMPLATE ,
                    SemVerStringFromFileVersionString (
                        s_CurrentFileVersionString ).QuoteString ( ) );

                if ( s_AssemblyInformationalVersionIndex > ArrayInfo.ARRAY_INVALID_INDEX )
                {
                    plstAssemblyInfoLines [ s_AssemblyInformationalVersionIndex ] = strSemVerString;
                    Console.WriteLine (
                        Properties.Resources.MSG_ASSEMBLYINFORMATIONALVERSION_UPDATED ,
                        strSemVerString );
                }   // TRUE (An InformationalVersion attribute exists; update it.) block, if ( s_AssemblyInformationalVersionIndex > ArrayInfo.ARRAY_INVALID_INDEX )
                else
                {
                    plstAssemblyInfoLines.Add ( strSemVerString );
                    Console.WriteLine (
                        Properties.Resources.MSG_ASSEMBLYINFORMATIONALVERSION_ADDED ,
                        strSemVerString );
                }   // FALSE (The InformationalVersion attribute is absent; create it.) block, if ( s_AssemblyInformationalVersionIndex > ArrayInfo.ARRAY_INVALID_INDEX )
            }   // if ( !string.IsNullOrEmpty ( s_CurrentFileVersionString ) )
        }   // private static void HandleAssemblyInformationalVersion


        /// <summary>
        /// Evaluate the s_OnlyWhenModified flag, which is TRUE when command
        /// line argument -onlywhenmodified is present. When it is, return TRUE
        /// only when at least one file in the directory that contains the
        /// AssemblyInfo.cs file specified in the command line or its immediate
        /// parent is newer than AssemblyInfo.cs or has its Archive flag set.
        /// </summary>
        /// <returns>
        /// See summary.
        /// </returns>
        private static bool OK2Proceed ( )
        {
            if ( s_OnlyWhenModified )
            {
                FileInfo fiAssemblyInfo = new FileInfo ( s_InputFileName );
                DateTime dtmAssemblyInfoModDate = fiAssemblyInfo.LastWriteTimeUtc;
                DirectoryInfo diAssemblyInfoHome = fiAssemblyInfo.Directory;
                FileInfo [ ] files = diAssemblyInfoHome.GetFiles ( SpecialStrings.ASTERISK , SearchOption.TopDirectoryOnly );

                //  ------------------------------------------------------------
                //  Check the files in the directory that contains AssemblyInfo
                //  for newer files or files that have their Archive flag set.
                //  Return True when either condition obtains.
                //  ------------------------------------------------------------

                for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                          intJ < files.Length ;
                          intJ++ )
                {
                    if ( ( files [ intJ ].Attributes & FileAttributes.Archive ) == FileAttributes.Archive )
                    {   // Stop when we find one modified file.
                        return true;
                    }   // if ( ( files [ intJ ].Attributes & FileAttributes.Archive ) == FileAttributes.Archive )
                }   // for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ; intJ < files.Length ; intJ++ )

                //  ------------------------------------------------------------
                //  Check the files in the immediate parent of the directory
                //  that contains AssemblyInfo for newer files or files that
                //  have their Archive flag set. Return True when either
                //  condition obtains.
                //  ------------------------------------------------------------

                DirectoryInfo diAssemblyInfoParent = diAssemblyInfoHome.Parent;
                files = diAssemblyInfoParent.GetFiles ( SpecialStrings.ASTERISK , SearchOption.TopDirectoryOnly );

                for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                          intJ < files.Length ;
                          intJ++ )
                {
                    if ( ( files [ intJ ].Attributes & FileAttributes.Archive ) == FileAttributes.Archive )
                    {   // Stop when we find one modified file.
                        return true;
                    }   // if ( ( files [ intJ ].Attributes & FileAttributes.Archive ) == FileAttributes.Archive )
                }   // for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ; intJ < files.Length ; intJ++ )

                //  ------------------------------------------------------------
                //  If processing reaches this point, NONE of the files in the
                //  directory that contains the specified AssemblyInfo.cs or its
                //  parent directory meets the conditions for processing.
                //  ------------------------------------------------------------

                return false;
            }   // TRUE (The switch to suppress processing unless files are changed is ENabled.) block, if ( s_OnlyWhenModified )
            else
            {
                return true;
            }   // FALSE (The switch to suppress processing unless files are changed is DISabled.) block, if ( s_OnlyWhenModified )
        }   // private static bool OK2Proceed


        /// <summary>
        /// Parse the command line arguments received by the calling main
        /// routine, storing their values in class-scoped static members.
        /// </summary>
        /// <param name="pastrArgs">
        /// This array of strings returns the array of strings received by the
        /// main routine from the operating system.
        /// </param>
        /// <returns>
        /// This method returns TRUE when all command line arguments are valid
        /// and the input file specified therein as a positional parameter
        /// exists.
        /// </returns>
        private static bool ParseCommandLineArgs ( string [ ] pastrArgs )
        {
            const int VER_PART_MIN_VALUE = 1;
            const int VER_PART_MAX_VALUE = 4;

            for ( int i = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                      i < pastrArgs.Length ;
                      i++ )
            {
                string strCurrArgLC = pastrArgs [ i ].ToLower ( );

                if ( strCurrArgLC.StartsWith ( CMD_INCREMENT_VERSION_PART ) )
                {
                    string strArgSubstring = pastrArgs [ i ].Substring ( CMD_INCREMENT_VERSION_PART.Length );

                    if ( int.TryParse ( strArgSubstring , out s_incParamNum ) )
                    {
                        if ( s_incParamNum < VER_PART_MIN_VALUE || s_incParamNum > VER_PART_MAX_VALUE )
                        {   // The value is out of range.
                            Console.WriteLine (
                                Properties.Resources.MSG_INCREMENT_OUT_OF_RANGE ,
                                VER_PART_MIN_VALUE ,                            // Format Item 0: Increment value must be between {0}
                                VER_PART_MAX_VALUE ,                            // Format Item 1: and {1}
                                s_incParamNum ,                                 // Format Item 2: Specified value = {2}
                                Environment.NewLine );                          // Format Item 3: {3}       Specified
                            DisplayHelpAndSetStatusCode ( ERR_INCREMENT_OUT_OF_RANGE );

                            return false;
                        }   // if ( s_incParamNum < VER_PART_MIN_VALUE || s_incParamNum > VER_PART_MAX_VALUE )
                    }   // TRUE (anticipated outcome) block, if ( int.TryParse ( strArgSubstring , out s_incParamNum ) )
                    else
                    {
                        Console.WriteLine (
                            Properties.Resources.MSG_INCREMENT_MUST_BE_NUMERIC ,
                            strArgSubstring.QuoteString ( ) ,                   // Format Item 0:        Specified value = {0}
                            Environment.NewLine );                              // Format Item 1: Error: Increment value must be numeric.{1}
                        DisplayHelpAndSetStatusCode ( ERR_INCREMENT_MUST_BE_NUMERIC );

                        return false;
                    }   // FALSE (unanticipated outcome) block, if ( int.TryParse ( strArgSubstring , out s_incParamNum ) )
                }   // if ( strCurrArgLC.StartsWith ( CMD_INCREMENT_VERSION_PART ) )
                else if ( strCurrArgLC.StartsWith ( CMD_SET_VERSION ) )
                {
                    s_VersionStr = pastrArgs [ i ].Substring ( CMD_SET_VERSION.Length );
                }   // else if ( strCurrArgLC.StartsWith ( CMD_SET_VERSION ) )
                else if ( strCurrArgLC.Equals ( CMD_FIX_ASSEMBLYVERSION ) )
                {
                    s_FixAssemblyVersion = true;
                }   // else if ( strCurrArgLC.Equals ( CMD_FIX_ASSEMBLYVERSION ) )
                else if ( strCurrArgLC.Equals ( CMD_FIX_FILEVERSION ) )
                {
                    s_FixAssemblyFileVersion = true;
                }   // else if ( strCurrArgLC.Equals ( CMD_FIX_FILEVERSION ) )
                else if ( strCurrArgLC.Equals ( CMD_COPYRIGHT_YEAR ) )
                {
                    s_FixCopyrightYear = true;
                }   // else if ( strCurrArgLC.Equals ( CMD_COPYRIGHT_YEAR ) )
                else if ( strCurrArgLC.Equals ( CMD_CHECK4MODIFIED ) )
                {
                    s_OnlyWhenModified = true;
                }   // else if ( strCurrArgLC.Equals ( CMD_CHECK4MODIFIED ) )
                else if ( strCurrArgLC.Equals ( CMD_STOP_WHEN_DONE ) )
                {
                    s_StopWhenDone = true;
                }   // else if ( strCurrArgLC.Equals ( CMD_STOP_WHEN_DONE ) )
                else
                {
                    s_InputFileName = pastrArgs [ i ];
                }   // FALSE block, else if ( args [ i ].StartsWith ( CMD_SET_VERSION ) )
            }   // for ( int i = ArrayInfo.ARRAY_FIRST_ELEMENT ; i < pastrArgs.Length ; i++ )

            if ( s_InputFileName == SpecialStrings.EMPTY_STRING )
            {   // The required file name parameter was omitted.
                Console.WriteLine (
                    Properties.Resources.MSG_NO_FILENAME ,                      // Format control string
                    Environment.NewLine );                                      // Format Item 0: Error: You must specify the name of the file to process.{0}
                DisplayHelpAndSetStatusCode ( ERR_NO_FILENAME );

                return false;
            }   // if ( s_InputFileName == SpecialStrings.EMPTY_STRING )

            if ( File.Exists ( s_InputFileName ) )
            {   // The specified input file exists.
                s_OutputFileName = string.Concat (
                    s_InputFileName ,
                    Properties.Resources.TEMP_FILENAME_EXTENSION );
                ReportSettings ( );

                return true;
            }   // TRUE (anticpated outcome) block, if ( File.Exists ( s_InputFileName ) )
            else
            {   // The specified input file cannot be found.
                Console.WriteLine (
                    Properties.Resources.MSG_FILE_NOT_FOUND ,
                    s_InputFileName.QuoteString ( ) );                          // Format control string
                DisplayHelpAndSetStatusCode ( ERR_FILE_NOT_FOUND );             // Format Item 0: Can not find file {0}

                return false;
            }   // FALSE (unanticpated outcome) block, if ( File.Exists ( s_InputFileName ) )
        }   // private static void ParseCommandLineArgs


        /// <summary>
        /// The main routine calls this routine to perform the task(s) specified
        /// by the command line switches. Its inputs are object-scoped static
        /// members.
        /// </summary>
        private static void PerformTask ( )
        {
            const int STACKTRACE_LEADING_SPACES = 16;

            try
            {
                if ( OK2Proceed ( ) )
                {   // s_OnlyWhenModified == true and at least one file is modified.
                    // Reading the lines into a list instead of an array permits appending a line to the list.
                    List<string> lstAssemblyInfoLines = new List<string> ( File.ReadAllLines ( s_InputFileName ) );

                    if ( Path.GetExtension ( s_InputFileName ).ToLower ( ) == @".vb" )
                    {   // C# and VB AssemblyVersion file syntax differ slightly.
                        s_isVB = true;
                    }   // if ( Path.GetExtension ( s_fileName ).ToLower ( ) == @".vb" )

                    int intNLines = lstAssemblyInfoLines.Count;                 // Stashing the count in a local scalar eliminates querying the List object on every iteration of the ensuing FOR loop.

                    for ( int intCurrentLine = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                              intCurrentLine < intNLines ;
                              intCurrentLine++ )
                    {   // Each line is processed or skipped. Regardless, each line goes back into the array.
                        lstAssemblyInfoLines [ intCurrentLine ] = ProcessLine (
                            lstAssemblyInfoLines ,
                            intCurrentLine );
                    }   // for ( int intCurrentLine = ArrayInfo.ARRAY_FIRST_ELEMENT ; intCurrentLine < intNLines ; intCurrentLine++ )

                    HandleAssemblyInformationalVersion ( lstAssemblyInfoLines );

                    File.WriteAllLines (
                        s_OutputFileName ,
                        lstAssemblyInfoLines.ToArray ( ) );

                    FileInfo fiInputFile = new FileInfo ( s_InputFileName );
                    FileAttributes enmInputFileAttributss = fiInputFile.Attributes;
                    fiInputFile.FileAttributeReadOnlyClear ( );
                    File.Delete ( s_InputFileName );
                    File.Move (
                        s_OutputFileName ,
                        s_InputFileName );

                    if ( ( enmInputFileAttributss & FileAttributes.ReadOnly ) == FileAttributes.ReadOnly )
                    {   // Restore the read-only attribute.
                        fiInputFile.FileAttributeReadOnlySet ( );
                    }   // if ( ( enmInputFileAttributss & FileAttributes.ReadOnly ) == FileAttributes.ReadOnly )
                }   // TRUE (One or more source files changed.) block, if ( OK2Proceed ( ) )
                else
                {
                    Console.WriteLine (
                        Properties.Resources.MSG_SOURCE_UNCHANGED ,             // Format Control String
                        Environment.NewLine );                                  // Format Item 0: Before and after message text
                }   // FALSE (All source files are unchanged.) block, if ( OK2Proceed ( ) )
            }   // Try block
            catch ( Exception ex )
            {
                Console.WriteLine (
                    Properties.Resources.MSG_ERR_RUNTIME ,  // Format control string
                    new object [ ]
                    {
                        ex.GetType().FullName ,             // Format Item 0: An {0} exception arose.
                        ex.Message ,                        // Format Item 1: Message   : {1}
                        ex.TargetSite ,                     // Format Item 2: TargetSite: {2}
                        ex.Source ,                         // Format Item 3: Source    : {3}
                        Utl.PrettyTrace (                   // Format Item 4: StackTrace:{4}
                            ex.StackTrace ,
                            STACKTRACE_LEADING_SPACES ) ,
                        Environment.NewLine                 // Format Item 5: Platform
                    } );
                Environment.ExitCode = ERR_RUNTIME;
            }   // Catch block

            Console.WriteLine ( Properties.Resources.MSG_PROCESSING_DONE.PadLeft ( LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
        }   // private static void PerformTask


        /// <summary>
        /// This method is called once for each line in the input AssemblyInfo.
        /// </summary>
        /// <param name="plstAssemblyInfoAttributes">
        /// This argument receives a reference to the generic List of strings
        /// that holds the contents of the AsseemblyInfo file.
        /// </param>
        /// <param name="pintLineNumber">
        /// This argument receives a copy of the current indes into the
        /// <paramref name="plstAssemblyInfoAttributes"/> List of assembly
        /// attribute strings.
        /// </param>
        /// <returns>
        /// The return value is the input string, modified as dictated by its
        /// contents and the object-scoped static members that are set according
        /// to the command line arguments.
        /// </returns>
        private static string ProcessLine (
            List<string> plstAssemblyInfoAttributes ,
            int pintLineNumber )
        {
            const string CS_LINE_COMMENT = @"//";
            const string VB_LINE_COMMENT = @"'";

            string strCurrentLine = plstAssemblyInfoAttributes [ pintLineNumber ];

            string rstrLineOut = null;

            if ( strCurrentLine.Length > ListInfo.EMPTY_STRING_LENGTH )
            {
                s_isLineUnChanged = true;

                if ( s_isVB )
                {   // The input file belongs to a Visual Basic project.
                    if ( strCurrentLine.StartsWith ( VB_LINE_COMMENT ) )
                    {   // Comment lines are preserved as is.
                        rstrLineOut = strCurrentLine;
                    }   // TRUE (The current line is a comment. Skip processing, but keep it.) block, if ( pstrLineIn.StartsWith ( VB_LINE_COMMENT ) )
                    else
                    {   // The line is valid source code.
                        if ( s_FixAssemblyVersion )
                        {
                            rstrLineOut = ProcessLinePart (
                                strCurrentLine ,
                                @"<Assembly: AssemblyVersion" );
                        }   // if ( s_FixAssemblyVersion )

                        if ( s_isLineUnChanged && s_FixAssemblyFileVersion )
                        {   // The previous call to ProcessLinePart left the line as is.
                            rstrLineOut = ProcessLinePart (
                                strCurrentLine ,
                                @"<Assembly: AssemblyFileVersion" );
                        }   // if ( s_isLineUnChanged && s_FixFileVersion )

                        if ( s_isLineUnChanged && s_AssemblyInformationalVersionIndex == ArrayInfo.ARRAY_INVALID_INDEX )
                        {   // The previous call to ProcessLinePart left the line as is. If it's an AssemblyInformationalVersion, save the line number.
                            s_AssemblyInformationalVersionIndex = strCurrentLine.StartsWith ( @"<Assembly: AssemblyInformationalVersion" )
                                ? pintLineNumber
                                : ArrayInfo.ARRAY_INVALID_INDEX;
                        }   // if ( s_isLineUnChanged && s_AssemblyInformationalVersionIndex == ArrayInfo.ARRAY_INVALID_INDEX )
                    }   // FALSE (Process the current line.) block, if ( pstrLineIn.StartsWith ( VB_LINE_COMMENT ) )
                }   // TRUE block, if ( s_isVB )
                else
                {   // The input file belongs to a C# project.
                    if ( strCurrentLine.StartsWith ( CS_LINE_COMMENT ) )
                    {   // Comment lines are preserved as is.
                        rstrLineOut = strCurrentLine;
                    }   // TRUE (The current line is a comment. Skip processing, but keep it.) block, if ( pstrLineIn.StartsWith ( CS_LINE_COMMENT ) )
                    else
                    {   // The line is valid source code.
                        if ( s_FixAssemblyVersion )
                        {
                            rstrLineOut = ProcessLinePart (
                                strCurrentLine ,
                                @"[assembly: AssemblyVersion" );
                        }   // if ( s_FixAssemblyVersion )

                        if ( s_isLineUnChanged )
                        {   // The previous call to ProcessLinePart fully processed the line.
                            if ( s_FixAssemblyFileVersion )
                            {
                                rstrLineOut = ProcessLinePart (
                                    strCurrentLine ,
                                    @"[assembly: AssemblyFileVersion" );
                            }   // if ( s_FixFileVersion )
                        }   // if ( s_isLineUnChanged )
                    }   // FALSE (Process the current line.) block, if ( pstrLineIn.StartsWith ( CS_LINE_COMMENT ) )
                }   // FALSE block, if ( s_isVB )
            }   // TRUE (normal case) block, if ( pstrLineIn.Length > ListInfo.EMPTY_STRING_LENGTH )

            if ( rstrLineOut == null )
            {   // Return unchanged lines.
                rstrLineOut = strCurrentLine;
            }   // if ( rstrLineOut == null )

            return rstrLineOut;
        }   // private static string ProcessLine


        /// <summary>
        /// The ProcessLine method calls this string, passing in a copy of the
        /// whole input line as <paramref name="pstrLineIn"/> and the prefix
        /// that identifies its place in the collection of AssemblyInfo 
        /// attributes, which comes in as <paramref name="pstrLinePart"/>. This
        /// routine is dedicated to processing AssemblyVersion attributes.
        /// </summary>
        /// <param name="pstrLineIn">
        /// This string argument receives a copy of the entire line as read from
        /// the input AssemblyInfo.cs file.
        /// </param>
        /// <param name="pstrLinePart">
        /// This string argument receives a copy of the prefix that identifies
        /// the assembly attribute represented by the whole line passed in as
        /// <paramref name="pstrLineIn"/>.
        /// </param>
        /// <returns>
        /// <para>
        /// This method returns the string to write into the AssemblyInfo.cs
        /// file being built in the temporary output file that will eventually
        /// replace the input file.
        /// </para>
        /// <para>
        /// When static object member s_FixCopyrightYear is TRUE, this method
        /// returns through CheckCopyrightYear, another method that handles the
        /// independent task of fixing up the copyright year.
        /// </para>
        /// </returns>
        private static string ProcessLinePart (
            string pstrLineIn ,
            string pstrLinePart )
        {
            const char VERSION_STRING_DELIMITER = '.';

            const string VERSION_PART_WILDCARD = @"*";

            int spos = pstrLineIn.IndexOf ( pstrLinePart );

            if ( spos >= 0 )
            {
                spos = pstrLineIn.IndexOf (
                    SpecialCharacters.DOUBLE_QUOTE ,
                    spos + pstrLinePart.Length );
                spos++;     // Advance past the opening quotation mark, which would otherwise interfere with parsing the major version.
                int epos = pstrLineIn.IndexOf (
                    SpecialCharacters.DOUBLE_QUOTE ,
                    spos + MagicNumbers.PLUS_ONE );
                string oldVersion = pstrLineIn.Substring (
                    spos ,
                    epos - spos );
                StringBuilder sbNewVersion = null;
                bool performChange = false;

                if ( s_incParamNum > 0 )
                {   // Replace one part of the version string.
                    string [ ] astrVersionNumbers = oldVersion.Split ( VERSION_STRING_DELIMITER );

                    if ( astrVersionNumbers.Length >= s_incParamNum && astrVersionNumbers [ s_incParamNum - 1 ] != VERSION_PART_WILDCARD )
                    {   // The list of version substrings has a string at the specified position that is not a wild card.
                        if ( Int64.TryParse ( astrVersionNumbers [ s_incParamNum - 1 ] , out long val ) )
                        {   // The value at the specified position is a long integer.
                            val++;
                            astrVersionNumbers [ s_incParamNum - MagicNumbers.PLUS_ONE ] = val.ToString ( );
                            sbNewVersion = new StringBuilder (
                                astrVersionNumbers [ ArrayInfo.ARRAY_FIRST_ELEMENT ] ,
                                pstrLineIn.Length + MagicNumbers.PLUS_ONE );

                            for ( int i = ArrayInfo.ARRAY_SECOND_ELEMENT ;
                                      i < astrVersionNumbers.Length ;
                                      i++ )
                            {
                                sbNewVersion.Append ( VERSION_STRING_DELIMITER );
                                sbNewVersion.Append ( astrVersionNumbers [ i ] );
                            }   // for ( int i = ArrayInfo.ARRAY_SECOND_ELEMENT ; i < nums.Length ; i++ )

                            performChange = true;
                        }   // TRUE (anticipated outcome) block, if ( Int64.TryParse ( nums [ s_incParamNum - 1 ] , out long val ) )
                        else
                        {   // The value at the specified positon is somethng besides an integer, an illegal value.
                            Console.WriteLine (
                                Properties.Resources.MSG_INVALID_VERSION_SUBSTRING ,
                                s_incParamNum ,                                 // Format Item 0: The version substring at position {0}
                                astrVersionNumbers [ s_incParamNum - 1 ] ,      // Format Item 1: Version substring = {1}
                                Environment.NewLine );                          // Format Item 2:  is invalid.{2}
                            Environment.ExitCode = ERR_INVALID_VERSION_SUBSTRING;
                        }   // FALSE (unanticipated outcome) block, if ( Int64.TryParse ( nums [ s_incParamNum - 1 ] , out long val ) )
                    }   // if ( nums.Length >= s_incParamNum && nums [ s_incParamNum - 1 ] != VERSION_PART_WILDCARD )
                }   // if ( s_incParamNum > 0 )
                else if ( s_VersionStr != null )
                {   // Replace the whole version string.
                    sbNewVersion = new StringBuilder ( s_VersionStr );
                    performChange = true;
                }   // else if ( s_versionStr != null )

                if ( performChange )
                {
                    StringBuilder sb = new StringBuilder ( pstrLineIn );
                    sb.Remove (
                        spos ,
                        epos - spos );
                    sb.Insert (
                        spos ,
                        sbNewVersion );
                    s_CurrentFileVersionString = sbNewVersion.ToString ( );
                    s_isLineUnChanged = false;
                    string strNewVersion = sb.ToString ( );
                    Console.WriteLine (
                        Properties.Resources.MSG_VERSION_CHANGE ,
                        pstrLineIn ,                                        // Format Item 0: FileVersion Changed: Old Value = {0}
                        strNewVersion ,                                     // Format Item 1: New Value = {1}
                        Environment.NewLine );                              // Format Item 2: At the beginning of the first line and the end of both lines

                    if ( s_FixCopyrightYear )
                    {
                        return CheckCopyrightYear ( strNewVersion );
                    }   // TRUE (Also check the copyright year.) block, if ( s_FixCopyrightYear )
                    else
                    {
                        return strNewVersion;
                    }   // FALSE (Legacy features only, which is only modifying version numbers.) block, if ( s_FixCopyrightYear )
                }   // TRUE (The version number changed.) block, if ( performChange )
                else
                {
                    Console.WriteLine (
                        Properties.Resources.MSG_COPYRIGHT_YEAR_UNCHANGED ,
                        pstrLineIn ,                                            // Format Item 0: FileVersion Unchanged:    Current Value = {0}
                        Environment.NewLine );
                }   // FALSE (The version number is unchanged.) block, if ( performChange )
            }   // if ( spos >= 0 )

            if ( s_FixCopyrightYear )
            {
                return CheckCopyrightYear ( pstrLineIn );
            }   // TRUE (Also check the copyright year.) block, if ( s_FixCopyrightYear )
            else
            {
                return pstrLineIn;      // Preserve the original input string.
            }   // FALSE (Legacy features only, which is only modifying version numbers.) block, if ( s_FixCopyrightYear )
        }   // private static string ProcessLinePart


        /// <summary>
        /// The main routine calls this method to display the command line
        /// options. Its inputs are defined as object-scoped static members.
        /// </summary>
        private static void ReportSettings ( )
        {
            Console.WriteLine (
                Properties.Resources.MSG_PROCESSING_BEGIN ,                 // Format control string
                s_InputFileName.QuoteString ( ) );                          // Format Item 0: Processing {0} ... 

            if ( ( !s_FixAssemblyVersion ) && ( !s_FixAssemblyFileVersion ) )
            {   // Fix both if neither is specified.
                s_FixAssemblyVersion = true;
                s_FixAssemblyFileVersion = true;
                Console.WriteLine (
                    Properties.Resources.MSG_UPDATING_ASMVER_AND_ASMFVER.PadLeft (
                        LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
            }   // if ( ( !s_FixAssemblyVersion ) && ( !s_FixFileVersion ) )
            else if ( s_FixAssemblyVersion && s_FixAssemblyFileVersion )
            {   // Both are fixed when both are explicitly stated.
                Console.WriteLine (
                    Properties.Resources.MSG_UPDATING_ASMVER_AND_ASMFVER.PadLeft (
                        LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
            }   // else if ( s_FixAssemblyVersion && s_FixFileVersion )
            else if ( s_FixAssemblyVersion )
            {   // Only the assembly version is
                Console.WriteLine (
                    Properties.Resources.MSG_UPDATING_ASMVER.PadLeft (
                        LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
            }   // TRUE block, else if ( s_FixAssemblyVersion )
            else
            {
                Console.WriteLine (
                    Properties.Resources.MSG_UPDATING_ASMFVER.PadLeft (
                        LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
            }   // FALSE block, else if ( s_FixAssemblyVersion )

            if ( s_FixCopyrightYear )
            {   // The copyright year switch is independent of the others.
                Console.WriteLine (
                    Properties.Resources.MSG_UPDATING_ANSWER_COPYRIGHT_YEAR.PadLeft (
                        LEFT_PADDING_FOR_SETTINGS_MESSAGES ) );
            }   // TRUE (The copyright year switch is set.) block, if ( s_FixCopyrightYear )
        }   // private static void ReportSettings


        /// <summary>
        /// Generate a Semantic Version (SemVer) version string from a 4-part
        /// FileVersion string.
        /// </summary>
        /// <param name="pstrCurrentFileVersionString">
        /// This string is a copy of the FileVersion string from which to derive
        /// a SemVer string.
        /// </param>
        /// <returns>
        /// The return value is a SemVer string.
        /// </returns>
        private static string SemVerStringFromFileVersionString ( string pstrCurrentFileVersionString )
        {
            const int EXPECTED_VERSION_SUBSTRING_COUNT = 4;
            const int SEMVER_VERSION_SUBSTRING_COUNT = 3;
            const char VERSION_SUBSTRING_DELIMITER = SpecialCharacters.FULL_STOP;

            if ( !string.IsNullOrWhiteSpace ( pstrCurrentFileVersionString ) )
            {
                string [ ] astrVersionSubstrings = pstrCurrentFileVersionString.Split ( VERSION_SUBSTRING_DELIMITER );

                if ( astrVersionSubstrings.Length == EXPECTED_VERSION_SUBSTRING_COUNT )
                {
                    StringBuilder sb = new StringBuilder ( MagicNumbers.CAPACITY_10 );

                    for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                              intJ < SEMVER_VERSION_SUBSTRING_COUNT ;
                              intJ++ )
                    {
                        if ( intJ > ArrayInfo.ARRAY_FIRST_ELEMENT )
                        {
                            sb.Append ( VERSION_SUBSTRING_DELIMITER );
                        }   // if ( intJ > ArrayInfo.ARRAY_FIRST_ELEMENT )

                        sb.Append ( astrVersionSubstrings [ intJ ] );
                    }   // for ( int intJ = ArrayInfo.ARRAY_FIRST_ELEMENT ; intJ < SEMVER_VERSION_SUBSTRING_COUNT ; intJ++ )

                    return sb.ToString ( );
                }   // TRUE (anticipated outcome) block, if ( astrVersionSubstrings.Length == EXPECTED_VERSION_SUBSTRING_COUNT )
                else
                {
                    Console.Error.WriteLine (
                        Properties.Resources.ERRMSG_VERSION_STRING_PARTS_COUNT ,// Format Control String
                        System.Reflection.MethodBase.GetCurrentMethod ( ).Name ,// Format Item 0: The format of the version string fed into method {0}
                        EXPECTED_VERSION_SUBSTRING_COUNT ,                      // Format Item 1: The expected number of version substrings is {1}.
                        astrVersionSubstrings.Length ,                          // Format Item 2: The actual number of substrings is {2}.
                        pstrCurrentFileVersionString ,                          // Format Item 3: The version string is {3}.
                        Environment.NewLine );                                  // Format Item 4: Line break at beginning and end of string and between lines
                    return SpecialStrings.EMPTY_STRING;
                }   // FALSE (unanticipated outcome) block, if ( astrVersionSubstrings.Length == EXPECTED_VERSION_SUBSTRING_COUNT )
            }   // TRUE (anticipated outcome) block, if ( !string.IsNullOrWhiteSpace ( pstrCurrentFileVersionString ) )
            else
            {
                return SpecialStrings.EMPTY_STRING;
            }   // FALSE (unanticipated outcome) block, if ( !string.IsNullOrWhiteSpace ( pstrCurrentFileVersionString ) )
        }   // private static string SemVerStringFromFileVersionString
    }   // class Program
}   // namespace AssemblyInfoUtil