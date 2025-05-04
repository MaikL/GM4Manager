# GM4Manager

GM4Manager is a modern WPF application designed to efficiently manage groups and members. 
The application features a user-friendly interface and powerful functionality, built on .NET 8.

The Group Manager is a good solution if you have managers, who wants to alter access to their shared folders.
themselves.
The problem is that there can only be one manager at a time.
GM4Manager solves this problem by setting the (member attribute)[https://learn.microsoft.com/en-us/windows/win32/adschema/a-member] by an administrator.
An administrator who has the authorization to edit this member attribute can also start GM4Manager and simply check the “As Admin” box.
![Screenshot of Add as Admin](/Screenshots/AddAsAdmin.jpg)
Explained with an example. 
A security group “CS_02_Commercial_RW” is created.
A second group is created for this: “CS_02_Commercial_Manager”, this group is added as a manager to the 
group “CS_02_Commercial_RW”.
If you now add a user to the “CS_02_Commercial_Manager” group as an administrator, 
this user then receives editing rights for the “CS_02_Commercial_RW” group and can add and remove users (and groups) there.
If you click on “Add user”, a search dialog for the domain opens.
!(Seach Dialog for adding a user)[/Screenshots/AddUserFromAD.jpg]
If the authorizations are sufficent, the user gets added.
!(adding successful)[/Screenshots/addingSuccessful.jpg]
If you want to run this as another user (e.g. you are logged in as john.doe but your administrator account ist a-john.doe)
there is a solution here : [RunProgramAs](https://github.com/MaikL/RunProgramAs) a smapp PowerShell script to
save Credentials and run a program with these credentials.

## Features

- **Group Management**: Create, edit, and delete groups.
- **Member Management**: Add and remove members within groups.
- **User-Friendly Interface**: Intuitive navigation and modern design.
- **Splash Screen**: Displays a loading screen during application startup.
- **Asynchronous Processing**: Optimized loading times with asynchronous operations.

## Prerequisites

- **.NET 8 SDK**: Ensure the .NET 8 SDK is installed.
- **Development Environment**: Visual Studio 2022 or any IDE that supports WPF.

## Installation

Download [GM4ManagerSetup.exe](Output/GM4ManagerSetup.exe)

## Usage

1. Start the application.
2. Manage groups and members through the main interface.
3. Use the buttons to add or remove members.

## Project Structure

- **`App.xaml` and `App.xaml.cs`**: Entry point of the application.
- **`SplashScreenWindow.xaml`**: Defines the splash screen.
- **`Manager.xaml`**: Main view for managing groups and members.
- **`Ressources`**: Contains icons and images for the application.
- Inno Setup is used as a the installer software

## Known Issues

- The start of the application can take a while, especially if you are in a lot of security groups.
- Ensure all resources (e.g., icons and images) are correctly included. See [Troubleshooting](#troubleshooting).

## Troubleshooting

- **Missing Resources**: Verify that the files are in the correct folder and their `Build Action` is set to `Resource`.
- **Path Issues**: Ensure the paths in the XAML files are correct.

## Contributing

Contributions are welcome! Fork the repository, make your changes, and submit a pull request.

## License

This project is licensed under the [GPL 3.0 License](LICENSE.md).

---

Thank you for using GM4ManagerWPF!   