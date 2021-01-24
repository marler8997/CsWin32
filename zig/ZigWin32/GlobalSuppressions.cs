// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

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
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "This code is not meant to be used as a public library",
    Scope = "module")]
