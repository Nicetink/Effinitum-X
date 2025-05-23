# Frequently Asked Questions (FAQ)

This section contains answers to frequently asked questions about Effinitum X.

## General Questions

### What is Effinitum X?
Effinitum X is a multifunctional program for optimizing and configuring the Windows operating system. It allows you to clean disk space from unnecessary files, manage processes and startup programs, optimize the system for better performance, manage system animations, and much more.

### Is the program safe to use?
Yes, Effinitum X is designed with your system's safety in mind. The program does not make irreversible changes to the system without your permission and warns about potentially dangerous operations. Critical system files are protected from changes, and it is recommended to create a system restore point before performing important operations.

### Are administrator rights required for the program to work?
Yes, administrator rights are required for most of the program's functions. This is necessary for accessing system settings, managing Windows services, working with the registry, and performing other administrative tasks.

### Is the program compatible with antivirus software?
Effinitum X is compatible with most antivirus programs. However, some antivirus programs may falsely identify the program as potentially dangerous due to functions related to changing system settings. In this case, it is recommended to add the program to the antivirus exclusion list.

### How often are updates released?
Developers release updates as new features are added and bugs are fixed. On average, major updates are released every 2-3 months, and minor fixes as needed. The program can automatically check for updates at startup (this option can be enabled in the settings).

## Functionality

### How to create a system restore point?
1. Open Effinitum X
2. Go to the "Tools" or "Additional" section
3. Click the "Create Restore Point" button
4. Enter a name for the restore point and click "Create"

### Can changes made by the program be undone?
Most changes made by the program can be undone. Before making significant changes, it is recommended to create a system restore point. If the changes led to undesirable consequences, you can:
1. Use the built-in undo function (if available for the specific operation)
2. Restore the system from a previously created restore point
3. Use the "System Management" section to return to standard settings

### How often should system cleanup be performed?
It is recommended to perform basic system cleanup approximately once every 2-4 weeks for normal computer use. With intensive use (installing/uninstalling programs, working with large files), cleanup can be performed more frequently. Deep cleaning is recommended less frequently, approximately once every 2-3 months.

### What to do if important files disappeared after system cleanup?
The program should not delete user files with standard cleanup settings. If this happened:
1. Check the Recycle Bin - the files may be there
2. Restore files from a previously created system restore point
3. Use data recovery programs
4. Contact the developers, describing the situation in maximum detail

### Does disabling animations affect performance?
Yes, disabling system animations can significantly improve performance on computers with limited resources. A particularly noticeable effect is observed on older computers and devices with integrated graphics. On modern powerful computers, the difference may be insignificant.

### How to optimize startup?
1. Open Effinitum X
2. Go to the "Startup Management" section
3. Click "Load Startup Items"
4. Disable unnecessary programs using the toggles
5. Or click "Optimize Startup" for automatic optimization

### Is it safe to disable Windows services?
Disabling some Windows services may affect system functionality. The program marks critical services and warns before disabling them. It is recommended to:
1. Create a restore point before disabling services
2. Do not disable services marked as "System" or "Important"
3. Disable one service at a time and check system operation

## Troubleshooting

### The program doesn't start or crashes
1. Check if .NET 7.0 Runtime or newer is installed
2. Run the program as administrator
3. Check error logs in the program folder (critical_error.log and unhandled_exceptions.log files)
4. Try reinstalling the program

### The "Bypass Blocks" function doesn't work
1. Make sure the program is running with administrator rights
2. Check the installation status of the Zapret component in the program
3. Temporarily disable antivirus or firewall
4. Check internet connection

### Some functions are unavailable or don't work
1. Make sure the program is running with administrator rights
2. Check compatibility with your Windows version
3. Update the program to the latest version
4. Check error logs in the program folder

### Problems with dark theme display
1. Make sure your Windows version supports dark theme (Windows 10 version 1809 or newer)
2. Check system theme settings
3. Restart the program after changing theme settings
4. Disable and re-enable transparency support in Windows settings

### System became unstable after optimization
1. Restore the system from a previously created restore point
2. Open Effinitum X and go to the "System Restore" section
3. Apply standard system settings
4. If the problem persists, seek help in the GitHub Issues section

## Development and Project Participation

### Can I suggest a new feature?
Yes, you can suggest a new feature by creating an Issue on GitHub in the project repository. Describe the feature in as much detail as possible, explain its usefulness and, if possible, suggest an implementation method.

### How to report a bug?
To report a bug:
1. Go to the project's [Issues page](https://github.com/Nicetink/Effinitum-X/issues)
2. Click "New Issue"
3. Select the bug report template
4. Fill in all necessary information:
   - Program version
   - Operating system version
   - Detailed description of the bug
   - Steps to reproduce
   - Screenshots (if possible)
   - Error logs (if available)

### How to become a project contributor?
If you want to contribute to the development of Effinitum X:
1. Familiarize yourself with the [contribution guidelines](https://github.com/Nicetink/Effinitum-X/blob/main/CONTRIBUTING.md)
2. Review open Issues and choose one you would like to work on
3. Fork the repository and make your changes
4. Create a Pull Request with a description of your changes

### Are there plans to develop versions for other operating systems?
Currently, Effinitum X is being developed only for Windows, as many functions are closely tied to the specifics of this operating system. In the long term, similar utilities for Linux and macOS may be created, but there are no specific plans yet.

## Additional Information

### Where are the program settings stored?
Program settings are stored in the config.json file in the folder:
- For the installed version: `%AppData%\Effinitum-X\`
- For the portable version: in the program folder

### Does the program affect computer warranty?
Using Effinitum X does not affect your computer's warranty, as the program does not make changes at the hardware level and does not modify device firmware.

### Does the program transmit any data to developers?
No, Effinitum X does not collect or transmit any personal data or information about your system to developers or third parties. The program works completely locally on your computer.

### Does the program have a paid version with additional features?
No, Effinitum X is completely free and open source. All features are available to all users without restrictions. Development is supported by the community and voluntary donations. 