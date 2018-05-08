# Sample apps for analyzing speaker emotion using Automotive SDK for Linux

## Dependencies

#### Affectiva Speech library

[TODO: add details when we have an installer]

#### Boost

See http://www.boost.org/


#### PortAudio: for building the `mic` sample

See http://www.portaudio.com/

* For CentOS : ` yum install -y portaudio-devel.x86_64`

#### LibSndFile: for building the `wav` sample

See http://www.mega-nerd.com/libsndfile/

* For CentOS : `yum install -y libsndfile-devel.x86_64`

###### Build Instruction on Linux

```
wget https://sourceforge.net/projects/boost/files/boost/1.63.0/boost_1_63_0.tar.gz \
		 && tar -xvf boost_1_63_0.tar.gz && rm boost_1_63_0.tar.gz
cd $WORKSPACE/boost_1_63_0 && \
    ./bootstrap.sh &&\
    ./b2 -j $(nproc) cxxflags=-fPIC threading=multi runtime-link=shared \
         --with-log --with-serialization --with-system --with-date_time \
         --with-filesystem --with-regex --with-timer --with-chrono --with-thread \
         --with-program_options --with-python && \
    ./b2 install

```

#### Building with CMake


Specify the the following CMake variables to identify the locations of various dependencies:

Note: for the \_LIBRARY and \_LIBRARY_DEBUG values, you only need to provide one or the other, as appropriate to the type of build you're doing (i.e. Release or Debug, as indicated by CMAKE_BUILD_TYPE).  On Windows, if you omit CMAKE_BUILD_TYPE and specify both values, CMake will produce a .sln file capabile of building both Debug and Release modes.

- **BUILD_MIC**: Build the `mic` sample app.
- **BUILD_WAV**: Build the `wac` sample app.
- **AffdexSpeech_INCLUDE**: path to the folder containing the Speech library header files
- **AffdexSpeech_LIBRARY**: path to the release mode Speech library
- **AffdexSpeech_LIBRARY_DEBUG**: path to the debug mode Speech library
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

```
# Setup BUILD
AffdexSpeech_INCLUDE=$WORKSPACE/auto-sdk/include
BOOST_DIR=$WORKSPACE/boost
AffdexSpeech_LIBRARY=$WORKSPACE/auto-sdk/lib/libaffectiva-speech.so
PortAudio_INCLUDE=/usr/include
PortAudio_LIBRARY=/usr/lib64/libportaudio.so
LibSndFile_INCLUDE=/usr/include
LibSndFile_LIBRARY=/usr/lib64/libsndfile.so

CMAKE_ARGS="-DCMAKE_BUILD_TYPE=Release \
-DBOOST_ROOT=$BOOST_DIR \
-DAffdexSpeech_INCLUDE=$AffdexSpeech_INCLUDE -DAffdexSpeech_LIBRARY=$AffdexSpeech_LIBRARY \
-DBUILD_MIC=ON \
-DPortAudio_INCLUDE=$PortAudio_INCLUDE -DPortAudio_LIBRARY=$PortAudio_LIBRARY \
-DBUILD_WAV=ON \
-DLibSndFile_INCLUDE=$LibSndFile_INCLUDE -DLibSndFile_LIBRARY=$LibSndFile_LIBRARY \
-DCMAKE_INSTALL_PREFIX=$WORKSPACE/install "

mkdir -p $WORKSPACE/build/
cd $WORKSPACE/build
cmake $CMAKE_ARGS -DCMAKE_CXX_FLAGS="-pthread" $WORKSPACE/src
make -j4
make install
```
