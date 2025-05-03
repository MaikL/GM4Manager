# GM4Manager

GM4Manager is a modern WPF application designed to efficiently manage groups and members. The application features a user-friendly interface and powerful functionality, built on .NET 8.

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

1. Clone the repository:
   
2. Open the solution in Visual Studio 2022.
3. Ensure all dependencies are installed.
4. Build and run the application.

## Usage

1. Start the application.
2. Manage groups and members through the main interface.
3. Use the buttons to add or remove members.

## Project Structure

- **`App.xaml` and `App.xaml.cs`**: Entry point of the application.
- **`SplashScreenWindow.xaml`**: Defines the splash screen.
- **`Manager.xaml`**: Main view for managing groups and members.
- **`Ressources`**: Contains icons and images for the application.

## Known Issues

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