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
