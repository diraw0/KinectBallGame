# KinectAssist360

KinectAssist360 is a desktop application that controls the tilt motor of the Kinect v1 using a potentiometer connected to an Arduino.
## Requirements

### Hardware
- Kinect for Xbox 360 (V1)
- Kinect power adapter with USB connection
- Arduino Uno or Nano
- 10k ohm potentiometer
- USB cable for Arduino

### Software
- Windows 10 or 11 (64-bit)
- Kinect SDK 1.8
- .NET 6.0 SDK or higher
- Visual Studio 2022 (Community or higher)
- Arduino IDE

## Installation

1. Install Kinect SDK 1.8 from Microsoft’s official website.  
2. Connect the Kinect to the USB port and make sure it’s detected by the SDK.  
3. Open the project `KinectAssist360.sln` in Visual Studio.  
4. Restore NuGet packages if needed.  
5. Connect the Arduino and check its COM port number.  
6. Upload the Arduino code from the `/Arduino` folder.  
7. Update the COM port in `KinectManager.cs` if needed.  
8. Build and run the project (`Ctrl + F5`).

## Running the App

When the application starts:
- The Kinect initializes automatically.
- The RGB and Depth cameras will appear in the interface.
- The tilt motor will move according to the potentiometer’s position.

## Notes
- If the cameras don’t show any image, check the Kinect’s USB connection.
- If the motor doesn’t move, check the COM port or potentiometer wiring. (code)
