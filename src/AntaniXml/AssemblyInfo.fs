﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("AntaniXml")>]
[<assembly: AssemblyProductAttribute("AntaniXml")>]
[<assembly: AssemblyDescriptionAttribute("xml random generator")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"