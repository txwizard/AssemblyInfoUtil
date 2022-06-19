# AssemblyInfoUtil ReadMe

This work is derived from the sample published in 2009 by Sergiy Korzh to
accompany "How To Update Assembly Version Number Automatically,"
<https://www.codeproject.com/Articles/31236/How-To-Update-Assembly-Version-Number-Automaticall>.

I acquired a copy around 29 June 2019 and made many improvements that I felt
made the program more useful that the version published by Mr. Korzh already
was. Although I put the program to immediate use, I soon became involved in new
projects that consumed every waking moment, and the program languished in my
tools library until June 2022, when I had an immediate use for it in the new
work. To meet that need, I made further improvements that enable it to skip the
AssemblyInfo.cs of a project that is unchanged since the last version incrment.

Today, we come to version 3.2, and it is high time that I made my improvements
available to the community.

The present work is about 50% Sergiy's work and 50% mine.

## Usage

A typical command line as it might appear in the Pre-Build step of a C# project,
is as follows.

    AssemblyInfoUtil.exe "$(ProjectDir)Properties\AssemblyInfo.cs" -inc:3 -fv -cy -onlywhenmodified

In the above command line, the path name is that of the AssemblyInfo.cs of the
project in question.

The following table lists and describes the remaining command line parameters.

|Library           |Interpretation                                     |
|------------------|---------------------------------------------------|
|-inc:3            |Increment the third component (the build number)   |
|                  |of the version string.                             |
|                  |                                                   |
|-fv               |Apply the increment directive just described to the|
|                  |FileVersion attribute of the assembly.             |
|                  |                                                   |
|-av               |Apply the increment directive just described to the|
|                  |AssemblyVersion attribute of the assembly. (This   |
|                  |parameter is omitted from the command line shown   |
|                  |above, in keeping with the principle that the      |
|                  |AssemblyVersion attribute remains unchanged unless |
|                  |the assembly breaks binary compatibility with its  |
|                  |predecessors.                                      |
|                  |                                                   |
|-cy               |Amend the Copyright Year when the year specifies a |
|                  |range of years, as in 1999-2022. At present, single|
|                  |copyright years are unsupported, but see the Road  |
|                  |Map section.                                       |
|                  |                                                   |
|-onlywhenmodified |This switch is the major feature differentiating   |
|                  |version 3.0 from version 2.0, and is the reason for|
|                  |my renewed interest in this assembly.              |

The foregoing example assumes that `AssemblyInfoUtil.exe` is installed into a
directory that is in the Windows `PATH` list.

Adding to my motivation to make these improvements is that I wanted a version
that was less aggressive in incrementing build numbers so that libraries and
stand-alone assemblies that I contribute to a project that has three developers
spread across three locations on two continents, didn't increment unless there
were code changes to justify incrementing them. This reduced churn in the build
pipeline and eliminated unnecessary and easily avoidable merge conflicts in the
GitHub repository shared by the three of us.

Since the three of us have slighly different machine configurations, we installed
`AssemblyInfoUtil.exe` into a `BuildTools` directory that was already present in
the Visual Studio solution and have the projects that use it call the copy in the
`BuildTools` directory, like so.

    $(SolutionDir)BuildTools\AssemblyInfoUtil.exe

In the above command line, `BuildTools` is the name of the solution folder into
which I installed `AssemblyInfoUtil.exe`, and `$(SolutionDir)` is a standard
MSBuild macro that maps to the directory in which the `.sln` file lives.

It is noteworthy that `$(SolutionDir)` and the other MSBuild macros that expand
to directories in the filesystem on the development or build machine include a
trailing backshlash, although adding one of your own is harmless because the
filename parser built into Windows ignores the extra backslash.

## Road Map

At present, the only item on the road map is implementing support for single
year copyright dates that may in future need to become year spans. At present,
such copyright notices are ignored and left unchanged. Of course, copyright year
updating functions independently of the version incrementing feature.