#Sample Apps for Affdex C++ SDK for Windows / Linux

Welcome to our repository on GitHub! Here you will find example code to get you started with our Affdex SDK 3.0 and begin emotion-enabling you own app! Documentation for the SDKs is available on the <a href=http://developer.affectiva.com/>Affectiva's Developer Portal</a>.

[![Build status](https://ci.appveyor.com/api/projects/status/pn2y9h8a3nnkiw41?svg=true)]
(https://ci.appveyor.com/project/ahamino/win-sdk-samples)

Dependencies
------------

*Windows*
- Affdex SDK 3.0 (32 bit)
- Visual Studio 2013 or higher

*Linux*
- Ubuntu 14.04 or higher or CentOS 7 or higher
- Affdex SDK 3.0
- CMake 2.8 or higher
- GCC 4.8 or higher

*Additional dependencies*

- OpenCV 3.1
- Boost 1.59

Installation
------------

- Download Affdex SDK [from here](http://developer.affectiva.com/downloads)
- Sign up for an evaluation license [by submitting this form](http://www.affectiva.com/45-day-free-trial/)

*Windows*
- Install the SDK using MSI installer.
- The additional dependencies get installed automatically by NuGet.

*Ubuntu*

```bashrc
sudo apt-get install build-essential libopencv-dev libboost1.55-all-dev cmake
wget http://developer.affectiva.com/downloads/linux
mkdir $HOME/affdex-sdk
tar -xzvf affdex-cpp-sdk-3.0-linux-64bit.tar.gz -C $HOME/affdex-sdk
export AFFDEX_DATA_DIR=$HOME/affdex-sdk/data
git clone https://github.com/Affectiva/cpp-sdk-samples.git $HOME/sdk-samples
mkdir $HOME/build
cd $HOME/build
cmake -DOpenCV_DIR=/usr/ -DBOOST_ROOT=/usr/ -DAFFDEX_DIR=$HOME/affdex-sdk $HOME/sdk-samples
make
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$HOME/affdex-sdk/lib
```


OpenCV-webcam-demo (c++)
------------------

Project for demoing the [FrameDetector class](http://developer.affectiva.com/v3/windows/analyze-frames/). It grabs frames from the camera, analyzes them and displays the results on screen.

The following command line arguments can be used to run it:

    -h [ --help ]                        Display this help message.
    -d [ --data ] arg (=data)            Path to the data folder
    -l [ --license ] arg (=test.license) License file.
    -r [ --resolution ] arg (=640 480)   Resolution in pixels (2-values): width
                                         height
    --pfps arg (=30)                     Processing framerate.
    --cfps arg (=30)                     Camera capture framerate.
    --bufferLen arg (=30)                process buffer size.
    --cid arg (=0)                       Camera ID.
    --faceMode arg (=0)                  Face detector mode (large faces vs small
                                        faces).
    --numFaces arg (=1)                  Number of faces to be tracked.
    --draw arg (=1)                      Draw metrics on screen.

Video-demo (c++)
----------

Project for demoing the Windows SDK [VideoDetector class](http://developer.affectiva.com/v3/windows/analyze-video/). It processs video files, displays the emotion metrics and exports the results in a csv file.

The following command line arguments can be used to run it:

    -h [ --help ]                        Display this help message.
    -d [ --data ] arg (=data)            Path to the data folder
    -l [ --license ] arg (=test.license) License file.
    -i [ --input ] arg                   Video file to processs
    --pfps arg (=30)                     Processing framerate.
    --draw arg (=1)                      Draw video on screen.
    --faceMode arg (=1)                  Face detector mode (large faces vs small
                                         faces).
    --numFaces arg (=1)                  Number of faces to be tracked.
    --loop arg (=0)                      Loop over the video being processed.


For an example of how to use Affdex in a C# application .. please refer to [AffdexMe](https://github.com/affectiva/affdexme-win)
