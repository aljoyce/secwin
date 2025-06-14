# Coding Exercise: Secure Windows Application

The specifications can be found here [Coding Exercise Secure Windows Application.pdf](./Coding%20Exercise%20Secure%20Windows%20Application.pdf)

## Windows Service Design

The following options are available:

- C++
- C#

C# will be the quickest for a demo application, depending on what's required C++ could
be a better choice for a real application.

### Service Requirements

- Runs at startup
- Publish a request for a google search request over HTTPS
- Has retry logic
- Logs to disk
  - Since we're on windows, the event log can be used
- Manages a secure network connection
- Validate the certificate chain

### Service Implementation

For UI to service communication the choices are

- Sockets
- ReST
- Named Pipes

**Conclusion**  
Sockets in .NET use WinSock under the hood on windows, so sockets will be used for IPC.

## UI Design

The following options are available:

- C# with Windows Forms or WPF
- C++ with MFC
- Flutter with Dart

### UI Requirements

- **User Input**
  - Take user input (&#x2757;)
  - Validate user input (&#x2757;)
- **Basic Configuration**
  - Server host and port to use to connect to the windows service (&#x2757;)
  - Mechanism for connecting / disconnecting from the windows service (&#x2757;)
  - Certificate for the windows service to use ???
- **Live Status Monitoring**
  - Indicator to alert the user if the UI is connected to the windows service (&#x2757;)
    - States are: connected, failed, retrying (&#x2757;)
  - View real time logs for the UI application (&#x2757;)
  - View real time logs from the service, possibly event logs ???
- **Service Communication**
  - Communication with Windows Service application (&#x2757;)
- **UI**
  - A clear easy to use UI (&#x2757;)
  - A responsive UI (&#x2757;)
  - System tray integration and icon (&#x2757;)

### UI Frameworks

**WPF**  
This does not seem to receiving the same level of interest from microsoft.  
It appears to be in a maintenance mode.

**MFC**  
MFC does seem to be getting updated for the new versions of Visual Studio.  

**Flutter**  
Flutter is open source from google, and recent.

**Conclusion**  
Having not used flutter or MFC before, flutter seems the easier option to learn for a
demo with a quick turnaround.
