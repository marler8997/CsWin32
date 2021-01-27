# Converts the win32metadata winmd file to JSON files

I've taken the `CsWin32` "projection" that generates `C#` bindings for the Win32 API and created a branch with additional code to reinterpret the Win32 API metadata into JSON files.

The underlying win32metadata project that's used to generate the bindings is in its early stages so I expect it to change rapidly.  The plan is to leverage the development work on the `C#` projection to maintain the code this JSON generator as the win32metadata project evolves.

# Build Instructions

The JsonWin32Generator.sln file should work with Visual Studio 2019.  If you don't want to install Visual Studio, then the following explains how to build without it.  As far as I know, these instructions should install any and all dependencies.

> NOTE: this repository has a git submodules, however, none of them are required to build this project

## 1. Install PowerShell Core

`CONTRIBUTING.md` recommends using "PowerShell Core" as a best practice.  Download it from here: https://github.com/PowerShell/PowerShell/releases

> I'm currently using version 7.0.4: https://github.com/PowerShell/PowerShell/releases/download/v7.0.4/PowerShell-7.0.4-win-x64.zip

## 2. Setup Powershell Build Console

From "Powershell Core" (pwsh), run the `init.ps1` script to install any dependencies and setup your powershell environment to be able to build.

> NOTE: it shouldn't matter what directory you are in when you run `init.ps`.

> TIP: if you have alot of console windows, you can use `$host.UI.RawUI.WindowTitle = "CsWin32"` to set the window title to easily find them.

## 3. Building/Running

```
# from the Powershell Core console that was setup above
cd jsongen

# build with
dotnet build

# run with (also builds)
dotnet run

# run but only generate code from the metadata that matches the given filters:
dotnet run [FILTERS...]
# i.e. you can specify methods/types/constants
dotnet run CreateFile
dotnet run WNDCLASS
dotnet run WS_OVERLAPPEDWINDOW
# supposedly you can also specify an api or module with Name.*, but I haven't found an example that works yet

```
