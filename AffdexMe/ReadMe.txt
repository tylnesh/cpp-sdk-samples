AffdexMe is a windows application that demonstrates the use of the Affdex SDK for Windows.  It uses the 
camera on your Windows PC to view, process and analyze live video of your face.
Start the app and you will see your own face on the screen, and metrics describing your expressions. 

This app includes the following command buttons:
-----------------------------------------------------------------------
    Start - Starts the camera processing
    Reset - 
    ShowPoints/HidePoints  - toggles the display of facial feature points, which Affdex uses to detect expressions. 
    Stop - Stops the camera processing
    Exit - exits the application

This application runs on Windows 7.0, 8.0 and 8.1

To use this project, you will need:
----------------------------------------------------------
- Visual Studio 2013

- Download and install the Windows SDK from Affectiva 

  By default, the Windows SDK is installed to the following location: C:\Program Files (x86)\Affectiva\Affdex SDK

  If you have installed the SDK to a location other than the default, you will have to modify the following String constants
  located in the MainWindow.xaml.cs file:

    AFFDEX_DATA_PATH
    AFFDEX_LICENSE_FILE


- The AffdexMe app can be built and run in either debug or release mode.

  To build and run in Debug Mode
  ----------------------------------------------
   - Through Visual Studio, add browse to and add a reference to Affdex.dll located at the following location:
	C:\Program Files (x86)\Affectiva\Affdex SDK\bin\debug\Affdex.dll

   - Add the following to your system path:
	C:\Program Files (x86)\Affectiva\Affdex SDK\bin\debug

  To build and run in Release Mode:
  ------------------------------------------------
   - Through Visual Studio, add browse to and add a reference to Affdex.dll located at the following location:
	C:\Program Files (x86)\Affectiva\Affdex SDK\bin\release\Affdex.dll

   - Add the following to your system path:
	C:\Program Files (x86)\Affectiva\Affdex SDK\bin\release

- In order to modify the system path, from the Control Panel, navigiate to the following:
        Control Panel -> Advanced System Settings -> Environment Variables -> System Variables -> Path

- Build the project

- Run the app through Visual Studio


Copyright (c) 2015 Affectiva. All rights reserved.