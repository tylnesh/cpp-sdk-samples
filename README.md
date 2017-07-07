#Sample Apps for Affdex SDK for Windows and Linux

Welcome to our repository on GitHub! Here you will find example code to get you started with our Affdex Linux SDK 3.2, Affdex Windows SDK 3.4 and begin emotion-enabling you own app! Documentation for the SDKs is available on the <a href=http://developer.affectiva.com/>Affectiva's Developer Portal</a>.

*Build Status*
- Windows: [![Build status](https://ci.appveyor.com/api/projects/status/pn2y9h8a3nnkiw41?svg=true)]
(https://ci.appveyor.com/project/ahamino/win-sdk-samples)
- Ubuntu: [![Build Status](https://travis-ci.org/Affectiva/cpp-sdk-samples.svg?branch=master)](https://travis-ci.org/Affectiva/cpp-sdk-samples)

Dependencies
------------

*Windows*
- Affdex SDK 3.4 (64 bit)
- Visual Studio 2013 or higher

*Linux*
- Ubuntu 14.04 or CentOS 7
- Affdex SDK 3.2
- CMake 2.8 or higher
- GCC 4.8

*Additional dependencies*

- OpenCV 2.4
- Boost 1.55
- libuuid
- libcurl
- libopenssl

Installation
------------


*Windows*
- Download Affdex SDK [from here](https://knowledge.affectiva.com/docs/getting-started-with-the-emotion-sdk-for-windows)
- Install the SDK using MSI installer.
- The additional dependencies get installed automatically by NuGet.

*Ubuntu*
- Download Affdex SDK [from here](https://knowledge.affectiva.com/docs/getting-started-with-the-affectiva-sdk-for-linux)

```bashrc
sudo apt-get install build-essential libopencv-dev libboost1.55-all-dev libcurl4-openssl uuid-dev cmake
wget https://download.affectiva.com/linux/affdex-cpp-sdk-3.2-20-ubuntu-xenial-xerus-64bit.tar.gz
mkdir $HOME/affdex-sdk
tar -xzvf affdex-cpp-sdk-3.2-20-ubuntu-xenial-xerus-64bit.tar.gz -C $HOME/affdex-sdk
export AFFDEX_DATA_DIR=$HOME/affdex-sdk/data
git clone https://github.com/Affectiva/cpp-sdk-samples.git $HOME/sdk-samples
mkdir $HOME/build
cd $HOME/build
cmake -DOpenCV_DIR=/usr/ -DBOOST_ROOT=/usr/ -DAFFDEX_DIR=$HOME/affdex-sdk $HOME/sdk-samples
make
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$HOME/affdex-sdk/lib
```

*CentOS*
- Download Affdex SDK [from here](https://knowledge.affectiva.com/docs/getting-started-with-the-affectiva-sdk-for-linux)

```bashrc
sudo yum install libcurl-devel.x86_64 libuuid-devel.x86_64 opencv-devel cmake.x86_64
wget https://sourceforge.net/projects/boost/files/boost/1.55.0/boost_1_55_0.tar.gz/download -O boost_1_55_0.tar.gz
tar -xzvf boost_1_55_0.tar.gz -C $HOME
cd boost_1_55
./bootstrap.sh --with-libraries=log,serialization,system,date_time,filesystem,regex,timer,chrono,thread,program_options
sudo ./b2 link=static install
wget https://download.affectiva.com/linux/affdex-cpp-sdk-3.2-2893-centos-7-64bit.tar.gz
mkdir $HOME/affdex-sdk
tar -xzvf affdex-cpp-sdk-3.2-2893-centos-7-64bit.tar.gz -C $HOME/affdex-sdk
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

Project for demoing the [FrameDetector class](https://knowledge.affectiva.com/docs/analyze-a-video-frame-stream-3). It grabs frames from the camera, analyzes them and displays the results on screen.

The following command line arguments can be used to run it:

    -h [ --help ]                        Display this help message.
    -d [ --data ] arg (=data)            Path to the data folder
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

Project for demoing the Windows SDK [VideoDetector class](https://knowledge.affectiva.com/docs/analyze-a-recorded-video-file) and [PhotoDetector class](https://knowledge.affectiva.com/docs/analyze-a-photo-4). It processs video or image files, displays the emotion metrics and exports the results in a csv file.

The following command line arguments can be used to run it:

    -h [ --help ]                        Display this help message.
    -d [ --data ] arg (=data)            Path to the data folder
    -i [ --input ] arg                   Video or photo file to process.
    --pfps arg (=30)                     Processing framerate.
    --draw arg (=1)                      Draw video on screen.
    --faceMode arg (=1)                  Face detector mode (large faces vs small
                                         faces).
    --numFaces arg (=1)                  Number of faces to be tracked.
    --loop arg (=0)                      Loop over the video being processed.


For an example of how to use Affdex in a C# application .. please refer to [AffdexMe](https://github.com/affectiva/affdexme-win)
