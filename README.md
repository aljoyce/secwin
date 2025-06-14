# SecWin

The windows service written in C# can be found here [secwin_service](./secwin_service/secwin_srv/)  
The library used by the service can be found here [secwin_lib](./secwin_service/secwin_lib/)

The UI written in dart with flutter can be found here [secwin_ui](./secwin_ui/)

The installation file for **Windows 11** can be found here [install_files](./install_files/)

## Important Notes

I do not have a windows machine, so the development was done on a Mac.  
I was able to get the UI working on a Windows 11 VM, but have not been able to get the
windows service working on that same VM.

Everything is working fine on a Mac, the port to windows however did not go well.

## Installation Instructions

To install the application, run the installer [secwin_setup.msi](./install_files/secwin_setup.msi)

To uninstall, go to Add/Remove programs and search for Centripetal Secwin

## Run From Code

To run the applications from the source the following is needed

- Flutter VSCode Extension
- Flutter SDK
- .NET SDK

**Run the service code from the terminal:**

```sh
cd secwin_service/secwin_con
dotnet run
```

**Run the flutter appliation from the terminal:**

- For windows you will need Visual Studio, with the C++ Desktop Development functionality installed, in addition to the Flutter SDK.  
- For macOS you will need XCode installed.

```sh
cd secwin_ui
flutter pub get
flutter build macos
flutter run
# select the device to run from the list
```

> If running on a windows machine, change `macos` to `windows`
