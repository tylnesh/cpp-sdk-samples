# Sample app for analyzing facial emotion using Automotive SDK for Linux

Additional Dependencies
------------

- OpenCV 2.4
- Boost 1.63
- libuuid
- libcurl
- libopenssl
- CMake minimum version v3.5

Installation
------------
*Installation Guide for dependencies CMake v3.8.1 and Boost v1.63 for Ubuntu 16.04*

- Boost

```
$ wget https://sourceforge.net/projects/boost/files/boost/1.63.0/boost_1_63_0.tar.gz
$ tar -xzvf boost_1_63_0.tar.gz -C $HOME
$ cd boost_1_63_0
$ ./bootstrap.sh
$ sudo ./b2 -j $(nproc) cxxflags=-fPIC threading=multi runtime-link=shared \
      --with-log --with-serialization --with-date_time \
      --with-filesystem --with-regex --with-timer --with-thread \
      --with-program_options install
```

- Building the SDK on Ubuntu 16.04

```bashrc
$ sudo apt-get install -y build-essential libopencv-dev libcurl4-openssl uuid-dev cmake
$ export AFFDEX_DATA_DIR=$HOME/auto-sdk/data/vision
$ git clone https://github.com/Affectiva/cpp-sdk-samples.git $HOME/sdk-samples
$ mkdir $HOME/build
$ cd $HOME/build
$ cmake -DOpenCV_DIR=/usr/ -DBOOST_ROOT=/usr/ -DAFFDEX_DIR=$HOME/auto-sdk $HOME/sdk-samples
$ make
$ export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$HOME/auto-sdk/lib

# The SDK statically links a forked version of OpenCV, so if you run into double free or corruption error
# then you will need to preload the OpenCV library installed from package manager
# Use this command to find the path of libopencv_core.so.2.4

$ ldconfig -p | grep libopencv_core.so.2.4
$ export LD_PRELOAD=/path/to/libopencv_core.so.2.4

```

Frame-detector-webcam-demo (c++)
------------------

Project for demoing the [FrameDetector class](https://auto.affectiva.com/docs/vision-create-detector). It grabs frames from the camera, analyzes them and displays the results on screen.

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

Docker Build Instructions
-------------------------

The Dockerfile's are located in the [docker](docker) directory. To build the docker image please refer to the files for instructions.
