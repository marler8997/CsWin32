#pragma warning disable SA1636 // File header copyright text should match
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Project Scope currently limited English only", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "This code is not meant to be used as a public library", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "This code is not meant to be used as a public library", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "This seems to require changing the global stylecop.json file if I want to use a custom header, however, I want to avoid modifying any files outside the jsongen directory.", Scope = "module")]
