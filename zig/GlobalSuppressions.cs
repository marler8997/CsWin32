#pragma warning disable SA1636 // File header copyright text should match
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "This project only supports English",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1310:Field names should not contain underscore",
    Justification = "This code will be ported to Zig",
    Scope = "module")]
[assembly: SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "This code will be ported to Zig",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1307:Accessible fields should begin with upper-case letter",
    Justification = "This code will be ported to Zig",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:File may only contain a single type",
    Justification = "This code will be ported to Zig",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "This code is not meant to be used as a public library",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1633:File should have header",
    Justification = "Meh",
    Scope = "module")]
[assembly: SuppressMessage(
    "MicrosoftCodeAnalysisReleaseTracking",
    "RS2008:Enable analyzer release tracking",
    Justification = "Don't know what this is, might revisit later",
    Scope = "module")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1308:Normalize strings to uppercase",
    Justification = "I need lowercase",
    Scope = "module")]
[assembly: SuppressMessage(
    "Globalization",
    "CA1305:Specify IFormatProvider",
    Justification = "English only?",
    Scope = "module")]
[assembly: SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "Meh",
    Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Maybe later", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "This code will be ported to Zig and I don't think this is a Zig convention", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1509:Opening braces should not be preceded by blank line", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:Non-private readonly fields should begin with upper-case letter", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1120:Comments should contain text", Justification = "This code will be ported to Zig", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This code will be ported to Zig", Scope = "module")]
