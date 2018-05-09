# Sample apps for analyzing speaker emotion using Affectiva's Automotive SDK for Linux
* the app `mic` demonstrates integration with a microphone as the audio input source
* the app `wav` demonstrates processing audio from a .wav recorded audio file


## Dependencies

#### Affectiva Speech library (needed for both `mic` and `wav`)

The Speech Library is packaged with the Automotive SDK, which is available upon request. To get access, please contact us at <a href=https://auto.affectiva.com/>Affectiva's Developer Portal</a>.

#### Boost (needed for both `mic` and `wav`)

See http://www.boost.org/

###### Build Instruction on Linux

```
mkdir boost-build
cd boost-build
wget https://sourceforge.net/projects/boost/files/boost/1.63.0/boost_1_63_0.tar.gz \
		 && tar -xvf boost_1_63_0.tar.gz && rm boost_1_63_0.tar.gz
cd boost_1_63_0 && \
    ./bootstrap.sh &&\
    ./b2 -j $(nproc) cxxflags=-fPIC threading=multi runtime-link=shared --with-program_options && \
    ./b2 install

```

#### PortAudio: (needed by `mic`)

See http://www.portaudio.com/

* For Ubuntu : `sudo apt-get install portaudio2`

#### LibSndFile: (needed by `wav`)

See http://www.mega-nerd.com/libsndfile/

* For Ubuntu : `sudo apt-get install libsndfile1-dev`



#### Building with CMake


Specify the the following CMake variables to identify the locations of various dependencies:

Note: for the \_LIBRARY and \_LIBRARY_DEBUG values, you only need to provide one or the other, as appropriate to the type of build you're doing (i.e. Release or Debug, as indicated by CMAKE_BUILD_TYPE).  On Windows, if you omit CMAKE_BUILD_TYPE and specify both values, CMake will produce a .sln file capable of building both Debug and Release modes.

- **BUILD_MIC**: Build the `mic` sample app.
- **BUILD_WAV**: Build the `wac` sample app.
- **AFFECTIVA_SDK_DIR**: path to the folder where the Automotive SDK is installed
- **PortAudio_INCLUDE**: path to the folder containing the PortAudio header files
- **PortAudio_LIBRARY**: path to the release mode PortAudio library
- **PortAudio_LIBRARY_DEBUG**: path to the debug mode PortAudio library
- **LibSndFile_INCLUDE**: path to the folder containing the LibSndFile header files
- **LibSndFile_LIBRARY**: path to the release mode LibSndFile library
- **LibSndFile_LIBRARY_DEBUG**: path to the debug mode LibSndFile library
- **BOOST_ROOT** path to the Boost src tree
- **BOOST_LIBRARYDIR** path to the Boost build directory containing its libs


#### Linux

For building under Linux, type the following command:

	cmake . -DCMAKE_BUILD_TYPE=[Release,Debug] <other cmake args, see above>

Example (replace directories starting with `/path/to` as appropriate):
```
# Set up paths used in cmake args
AFFECTIVA_SDK_DIR=/path/to/auto-sdk
BOOST_DIR=/path/to/boost-build
PortAudio_INCLUDE=/usr/include
PortAudio_LIBRARY=/usr/lib/x86_64-linux-gnu/libportaudio.so.2
LibSndFile_INCLUDE=/usr/include
LibSndFile_LIBRARY=/usr/lib/x86_64-linux-gnu/libsndfile.so

CMAKE_ARGS="-DCMAKE_BUILD_TYPE=Release \
-DBOOST_ROOT=$BOOST_DIR \
-DAFFECTIVA_SDK_DIR=$AFFECTIVA_SDK_DIR \
-DBUILD_MIC=ON \
-DPortAudio_INCLUDE=$PortAudio_INCLUDE -DPortAudio_LIBRARY=$PortAudio_LIBRARY \
-DBUILD_WAV=ON \
-DLibSndFile_INCLUDE=$LibSndFile_INCLUDE -DLibSndFile_LIBRARY=$LibSndFile_LIBRARY \
-DCMAKE_INSTALL_PREFIX=/path/to/install "

# create a build directory
mkdir samples-build/
cd samples-build

# run cmake, specifying the path to the CMakeLists.txt file (the same directory
# that this README.md file resides in)
cmake $CMAKE_ARGS -DCMAKE_CXX_FLAGS="-pthread" /path/to/cpp-sdk-samples/speech
make -j4
make install
```
