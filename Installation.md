# Installation and Setup of Effinitum X

This page describes in detail the various ways to install and run Effinitum X.

## Contents
1. [Downloading the Program](#downloading-the-program)
2. [Installation Using the Installer](#installation-using-the-installer)
3. [Using the Portable Version](#using-the-portable-version)
4. [Building from Source Code](#building-from-source-code)
5. [First Launch](#first-launch)
6. [Launch Specifics on Different Windows Versions](#launch-specifics-on-different-windows-versions)
7. [Installation Troubleshooting](#installation-troubleshooting)

## Downloading the Program

The latest version of Effinitum X can be downloaded from the [official releases page](https://github.com/Nicetink/Effinitum-X/releases).

The following files are available on the releases page:
- **Effinitum-X-Setup-vX.X.X.exe** - program installer
- **Effinitum-X-Portable-vX.X.X.zip** - portable version in an archive
- **Source code (zip/tar.gz)** - program source code

Choose the option that suits your preferences.

## Installation Using the Installer

Installation using the installer is recommended for most users:

1. Download the **Effinitum-X-Setup-vX.X.X.exe** file
2. Run the downloaded file
3. If a Windows security warning appears, click "More info" → "Run anyway"
4. In the installation wizard window, click "Next"
5. Select the installation folder or keep the default one
6. Select additional options:
   - Create a desktop shortcut
   - Launch at Windows startup
   - Add to context menu
7. Click "Install"
8. After installation is complete, click "Finish"

The program will be installed in the selected folder and added to the Start menu.

## Using the Portable Version

The portable version doesn't require installation and can run from a USB drive:

1. Download the **Effinitum-X-Portable-vX.X.X.zip** file
2. Extract the archive to any folder on your computer or USB drive
3. Run the **SystemOptimizer.exe** file from the extracted folder

**Note**: When first launching on a new computer, administrator rights may be required for full functionality.

## Building from Source Code

For developers and advanced users, building from source code is available:

### Build Requirements
- Visual Studio 2022 or newer (or JetBrains Rider)
- .NET 7.0 SDK (or newer)
- Git (optional)

### Build Process

1. Get the source code in one of these ways:
   - Clone the repository: `git clone https://github.com/Nicetink/Effinitum-X.git`
   - Or download the source code archive and extract it

2. Open the solution file `SystemOptimizer.sln` in Visual Studio or Rider

3. Restore NuGet packages:
   - In Visual Studio: right-click on the solution → "Restore NuGet Packages"
   - In Rider: right-click on the solution → "Restore NuGet Packages"

4. Select the build configuration:
   - Debug: for debugging and development
   - Release: for the final version

5. Build the project:
   - In Visual Studio: Build → Build Solution (F7)
   - In Rider: Build → Build Solution (F6)

6. Run the program:
   - Built files are located in the `bin\Debug\net7.0-windows` or `bin\Release\net7.0-windows` folder
   - Run the `SystemOptimizer.exe` file

## First Launch

When launching the program for the first time:

1. You will be prompted to select the interface language (currently Russian and English are available)
2. The program will request administrator rights - this is necessary for accessing system functions
3. A welcome window will appear with basic information about the program
4. It is recommended to go to the "Settings" section and configure the program according to your preferences

## Launch Specifics on Different Windows Versions

### Windows 10
- All program functions are supported
- Dark theme requires version 1809 or newer
- It is recommended to install the latest updates for proper operation of all functions

### Windows 11
- Full support for all functions
- Improved integration with dark theme
- Automatic detection of hard drive types (SSD/HDD)

### Windows 8.1 and older versions
- Limited support for some functions
- No support for UWP applications on Windows 7
- Possible interface appearance issues

## Installation Troubleshooting

### Program doesn't launch after installation
1. Make sure .NET 7.0 Runtime is installed:
   - Download and install from the [official Microsoft website](https://dotnet.microsoft.com/download/dotnet/7.0)
2. Check for administrator rights:
   - Right-click on the program shortcut → "Run as administrator"
3. Disable antivirus during installation and first launch

### "Cannot find the specified file" error
1. Make sure all program files are installed
2. Check for possible file blocks:
   - Right-click on the .exe file → Properties → Uncheck "Block"
3. Reinstall the program

### "Application blocked for security reasons" error
1. Right-click on the installer file → Properties
2. Click "Unblock" at the bottom of the properties window
3. Run the installation again

### Other problems
- Check the error log in the program folder (critical_error.log and unhandled_exceptions.log files)
- Make sure your system meets the minimum requirements
- Try running the program in compatibility mode with a previous Windows version

If the problem persists, seek help by creating an [issue](https://github.com/Nicetink/Effinitum-X/issues) in the project repository with a detailed description of the problem and screenshots. 