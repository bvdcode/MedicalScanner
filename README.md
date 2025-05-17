# Medical Scanner

## Overview

Medical Scanner is a cross-platform mobile application built with .NET MAUI that demonstrates Bluetooth Low Energy (BLE) scanning functionality. This app is specifically focused on scanning for medical BLE devices and can be used for testing or as a foundation for more advanced medical device connectivity applications.

## Features

- Scan for nearby Bluetooth Low Energy devices
- Display discovered device names in a list
- Request necessary permissions for Bluetooth scanning
- Cross-platform support (Android, iOS, MacCatalyst, Windows)
- Automated build and release process

## Technical Details

The app is built using:

- .NET MAUI (Multi-platform App UI)
- [Plugin.BLE](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le) for Bluetooth functionality
- GitHub Actions for CI/CD

## Project Structure

```
MedicalScanner/
├── .github/workflows/    # CI/CD configuration
├── Sources/              # Application source code
│   ├── Platforms/        # Platform-specific implementations
│   ├── Resources/        # App resources (images, fonts, styles)
│   ├── App.xaml          # Application definition
│   ├── MainPage.xaml     # Main UI
│   ├── MauiProgram.cs    # App initialization
│   └── MedicalScanner.csproj  # Project configuration
└── README.md             # This file
```

## Setup Instructions

### Prerequisites

- Visual Studio 2022 with .NET MAUI workload
- For Android: Android SDK
- For iOS/MacCatalyst: Mac with Xcode
- For Windows: Windows 10/11

### Building the Application

1. Clone the repository
2. Open MedicalScanner.sln in Visual Studio
3. Select your target platform
4. Press F5 to build and run the app

## Using the App

1. Launch the application
2. Click the "Click to scan" button
3. Grant location permissions when prompted (required for BLE scanning)
4. The app will start scanning and display the names of discovered BLE devices

## Automated Build Process

The project includes a GitHub Actions workflow that:

1. Builds the Android APK when changes are pushed to the main branch
2. Automatically increments the app version number
3. Publishes the APK as a GitHub release

The workflow file can be found at apk-file.yml.

## Next Steps

This application serves as a demonstration and starting point for BLE medical device scanning. For a production application, consider adding:

- Device filtering to show only medical devices
- Device connection capabilities
- Data reading and interpretation from connected devices
- User authentication and data security features
- Cloud synchronization for medical data
