# Generate Zig Bindings

I've taken the `CsWin32` "projection" that generates `C#` bindings for the Win32 API and created a branch with additional code to generate bindings for the Zig Programming Language.

The underlying win32metadata project that's used to generate the bindings is in its early stages so I expect it to change rapidly.  The plan is to leverage the development work on the `C#` projection to maintain the Zig projection as the win32metadata project evolves.  At some point, the Zig community can implement a pure Zig solution once a `winmd` parser is created (i.e. https://github.com/microsoft/winmd).

# Build Instructions

As far as I know, these instructions should install any and all dependencies.

## 1. Download Submodules

This project uses git submodules, so download them with

```
git submodule update --init --recursive
```

## 2. Install PowerShell Core

`CONTRIBUTING.md` recommends using "PowerShell Core" as a best practice.  Download it from here: https://github.com/PowerShell/PowerShell/releases

> I'm currently using version 7.0.4: https://github.com/PowerShell/PowerShell/releases/download/v7.0.4/PowerShell-7.0.4-win-x64.zip

## 3. Setup Powershell Build Console

From "Powershell Core" (pwsh), run the `init.ps1` script to install any dependencies and setup your powershell environment to be able to build.

> NOTE: it shouldn't matter what directory you are in when you run `init.ps`.

> TIP: if you have alot of console windows, you can use `$host.UI.RawUI.WindowTitle = "CsWin32"` to set the window title to easily find them.

## 4. Building/Running

```
# from the Powershell Core console that was setup above
cd zig
# build with
dotnet build
# run with
dotnet run
```
