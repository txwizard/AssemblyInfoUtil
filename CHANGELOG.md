# AssemblyInfoUtil ReadMe

This file is a chronological history of development of `AssemblyInfoUtil.exe`
from the point at which the original author, Sergiy Korzh, published it along
with an article that he published on _The Code Project_.

As I do with _every_ ChangeLog that I publish, revisions appear most recent
first, so that the latest changes are visible without scrolling.

## 2022/11/24, Version 3.6

1. Add a line break to the shutdown message, so that there is always one in the
output stream.

2. Update the NuGet packages to the new versions that target framework 4.8.

## 2022/11/24, Version 3.5
## 2022/07/10, Version 3.4

1. Fix a bug that prevented existing AssemblyInformationalVersion being updated,
causing instead a duplicate string to be inserted, which resulted in a fatal
syntax error on the next build.

2. Move most of the remaining hard coded strings into the string table.

## 2022/07/09, Version 3.3

Implement Support for AssemblyInformationalVersion.

AssemblyInformationalVersion is an assembly attribute that is used only by NuGet.
Since the NuGet versioning convention is SemVer, the AssemblyInformationalVersion
is a three-part version string, and the fourth part may be, and often is, a
string that is used to designate a beta or other pre-release package.

In the interest of maximum flexibility, this program behaves as follows with
respect to this assembly attribute.

1. When AssemblyInformationalVersion exists, its first three substrings, all of
which must be numeric, are updated to match the corresponding values in the
AssemblyFileVersion attribute when it is updated.

2. When the AssemblyInformationalVersion attribute is missing, it is added and
set to the Semantic Version per the AssemblyFileVersion attribute.

3) When the AssemblyFileVersion remains unchanged, so does the
AssemblyInformationalVersion. Moreover, the program dispenses with checking for its
presence unless the AssemblyFileVersion attribute is being updated.

## 2022/06/19, Version 3.2

Display details about changes made to the version and copyright notice attributes, including notes when one or the other is skipped.

## 2022/06/14, Version 3.1

Leave AssemblyInfo.cs unchanged when everything in its directory and its parent directory is unchanged.

## 2022/06/05, Version 3.0

Implement copyright year fixup for AssemblyCopyright.

While I was at it, I upgraded the target framework from 3.5 Client Profile to 4.8.

## 2019/06/29, Version 2.0

This version incorporates many improvements.

1) The change that required me to build the assembly in the first place is that the program didn't handle the white space with which I always surround the attributes in AssemblyInfo.cs. The new version supports the same formatting variance in a Visual Basic module, AssemblyInfo.vb.

2) A bug that crippled support for updating the major version number is fixed and proven.

3) Validation of the version component specified on the "-inc" switch is much more robust. The code now gracefully handles non-numeric version substring ordinals, and enforces the required upper and lower bounds of 0 and 4.

4) I added support for restricting updating to the Assembly or File version, preserving the other. The default, if neither is specified, is the legacy behavior, which updates both.

5) I renamed the class and its source file to Program, so that they conform to the generally accepted naming conventions for the entry point class and its source file.

6) Move most of the strings, including everything that displays on the console, into managed resources.

## 2009/01/28, Version 1.2

This is the version that Sergiy Korzh published with a CodeProject article, "How To Update Assembly Version Number Automatically," <https://www.codeproject.com/Articles/31236/How-To-Update-Assembly-Version-Number-Automaticall>.
