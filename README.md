# GM4Manager

Have you ever had the problem that you should quickly create a new shared folder
for Fed and George and Lisa (oh and don't forget Tom)?
A short time later, Lisa needs read-only rights and George has moved to another department,
so he is no longer allowed in the folder?
That's why the “Group Manager for Managers” was created. It is a tool, 
with which you create a directory for the managers, and by assigning the “Managed By” authorization 
the manager can then independently create and manage new folders with permissions of their own choosing.
The best thing about GM4Manager, however, is that you can not only add individual users to “Managed By”, 
but also groups. This makes it possible, for example, to have an “Accounting Manager” group. 
There, not only the head of accounting but also his deputy has the right to create folders and change authorizations.


Usually there can only be only one manager at a time as "Managed By".
GM4Manager solves this problem by setting the (member attribute)[https://learn.microsoft.com/en-us/windows/win32/adschema/a-member] by an administrator.
An administrator who has the authorization to edit this member attribute can also start GM4Manager and simply check the “As Admin” box.
![Screenshot of Add as Admin](/Screenshots/AddAsAdmin.jpg)
Explained with an example. 
A security group “CS_02_Commercial_RW” is created.
A second group is created for this: “CS_02_Commercial_Manager”, this group is added as a manager to the 
group “CS_02_Commercial_RW”.
![Screenshot of Managed By](/Screenshots/ManagedBy.jpg)
If you now add a user to the “CS_02_Commercial_Manager” group as an Domain Administrator, 
this user then receives editing rights for the “CS_02_Commercial_RW” group and can add and remove users (and groups) there.
If you click on “Add user”, a search dialog for the domain opens.
![Seach Dialog for adding a user](/Screenshots/AddUserFromAD.jpg)
If the authorizations are sufficent, the user is added.
![adding successful](/Screenshots/AddingSuccessful.jpg)
At the Explorer view, you can see the group and the members of the selected folder.
If the folder has separate share permissions, these are also displayed and you can add / remove user / groups.
You can even change the permissions of the Security Group or user from Readonly to Modify and vice versa.
![Explorer](/Screenshots/Explorer.jpg)
If you want to run this as another user (e.g. you are logged in as john.doe but your administrator account ist a-john.doe)
there is a solution here : [RunProgramAs](https://github.com/MaikL/RunProgramAs) a small PowerShell script to
save Credentials and run a program with these credentials.

## Features

- **Group Management**: Adding and removing users to groups where you are the Manager.
- **Explorer**: Explorer like interface for easy navigation and management of groups and their permissions.
- **Change Permissions**: Modify group permissions directly from the interface.
- **Splash Screen**: Displays a loading screen during application startup.
- **Asynchronous Processing**: Optimized loading times with asynchronous operations.

## Prerequisites

- **.NET 8 SDK**: Ensure the .NET 8 SDK is installed.
- **Development Environment**: Visual Studio 2022 or any IDE that supports WPF.
- https://github.com/Kinnara/ModernWpf

## Installation

Download [GM4ManagerSetup.exe](Output/GM4ManagerSetup.exe)

## Usage

1. Start the application.
2. Manage groups and members through the main interface.
3. Use the buttons to add or remove members.
4. Use the Explorer-like interface to navigate through groups and their members.

## Project Structure

- **`App.xaml` and `App.xaml.cs`**: Entry point of the application.
- **`SplashScreenWindow.xaml`**: Defines the splash screen.
- **`ManagerUC.xaml`**: Main view for managing groups and members.
- **`ExplorerUC.xaml`**: Explorer-like interface for navigating groups.
- **`Ressources`**: Contains icons and images for the application.
- Inno Setup is used as a the installer software

## Known Issues

- The start of the application can take a while, especially if you are in a lot of security groups.
- Ensure all resources (e.g., icons and images) are correctly included. See [Troubleshooting](#troubleshooting).
- If you are on a VPN and in a lot of Security Groups it can take over a minute to launch!

## Troubleshooting

- **Missing Resources**: Verify that the files are in the correct folder and their `Build Action` is set to `Resource`.
- **Path Issues**: Ensure the paths in the XAML files are correct.
- **Performance Issues**: If the application is slow, check the network connection and ensure the domain controller is reachable. If you have a lot of Security Groups it can take a while to load the application.
- **Permissions**: Ensure you have the necessary permissions to manage groups and members in Active Directory.
- **Share Folders**: If you are using this application to manage shared folders, ensure that the user has the necessary permissions to modify group memberships. Especially the share permissions on the share folder itself.
- **As Admin Mode**: If you are using the "As Admin" mode, ensure that the user has the necessary permissions to edit the `member` attribute of the group.

## Contributing

Contributions are welcome! Fork the repository, make your changes, and submit a pull request.

## License

This project is licensed under the [GPL 3.0 License](LICENSE.md).

Short Demo video: [GM4Manager Demo](/Screenshots/GM4Manager.gif)

---

Thank you for using GM4Manager!   