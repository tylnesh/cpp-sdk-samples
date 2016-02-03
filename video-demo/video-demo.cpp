#include <iostream>
#include <memory>
#include <chrono>
#include <fstream>

#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <boost/filesystem.hpp>
#include <boost/timer/timer.hpp>
#include <boost/program_options.hpp>

#include "VideoDetector.h"
#include "AffdexException.h"

#include "AFaceListener.hpp"
#include "PlottingImageListener.hpp"
#include "StatusListener.hpp"


using namespace std;
using namespace affdex;

/// <summary>
/// Project demos how to use the Affdex Windows SDK VideoDetector
/// </summary>
int main(int argsc, char ** argsv)
{

    //Defaults, overridden by the command line parameters
    affdex::path DATA_FOLDER;
    affdex::path LICENSE_PATH;
    affdex::path videoPath;
    int process_framerate = 30;
    bool draw_display = true;
    bool loop = false;
    unsigned int nFaces = 1;
    int faceDetectorMode = (int)FaceDetectorMode::SMALL_FACES;
    
    const int precision = 2;
    std::cerr.precision(precision);
    std::cout.precision(precision);
    
    namespace po = boost::program_options; // abbreviate namespace
    po::options_description description("Project for demoing the Windows SDK VideoDetector class (processing video files).");
    description.add_options()
    ("help,h", po::bool_switch()->default_value(false), "Display this help message.")
#ifdef _WIN32
    ("data,d", po::wvalue< affdex::path >(&DATA_FOLDER)->default_value(affdex::path(L"data"), std::string("data")), "Path to the data folder")
    ("license,l", po::wvalue< affdex::path >(&LICENSE_PATH)->default_value(affdex::path(L"test.license"), std::string("test.license")), "License file.")
    ("input,i", po::wvalue< affdex::path >(&videoPath)->required(), "Video file to processs")
#else // _WIN32
    ("data,d", po::value< affdex::path >(&DATA_FOLDER)->default_value(affdex::path("data"), std::string("data")), "Path to the data folder")
    ("license,l", po::value< affdex::path >(&LICENSE_PATH)->default_value(affdex::path("test.license"), std::string("test.license")), "License file.")
    ("input,i", po::value< affdex::path >(&videoPath)->required(), "Video file to processs")
#endif // _WIN32
    ("pfps", po::value< int >(&process_framerate)->default_value(30), "Processing framerate.")
    ("draw", po::value< bool >(&draw_display)->default_value(true), "Draw video on screen.")
    ("faceMode", po::value< int >(&faceDetectorMode)->default_value((int)FaceDetectorMode::SMALL_FACES), "Face detector mode (large faces vs small faces).")
    ("numFaces", po::value< unsigned int >(&nFaces)->default_value(1), "Number of faces to be tracked.")
    ("loop", po::value< bool >(&loop)->default_value(false), "Loop over the video being processed.")
    ;
    po::variables_map args;
    try
    {
        po::store(po::command_line_parser(argsc, argsv).options(description).run(), args);
        if (args["help"].as<bool>())
        {
            std::cout << description << std::endl;
            return 0;
        }
        po::notify(args);
    }
    catch (po::error& e)
    {
        std::cerr << "ERROR: " << e.what() << std::endl << std::endl;
        std::cerr << "For help, use the -h option." << std::endl << std::endl;
        return 1;
    }
    
    // Parse and check the data folder (with assets)
    if (!boost::filesystem::exists(DATA_FOLDER))
    {
        std::cerr << "Data folder doesn't exist: " << std::string(DATA_FOLDER.begin(), DATA_FOLDER.end()) << std::endl;
        std::cerr << "Try specifying the folder through the command line" << std::endl;
        std::cerr << description << std::endl;
        return 1;
    }
    try
    {
        //Initialize the video file detector
        VideoDetector videoDetector(process_framerate, nFaces, (affdex::FaceDetectorMode) faceDetectorMode);
        
        //Initialize out file
        boost::filesystem::path csvPath(videoPath);
        csvPath.replace_extension(".csv");
        std::ofstream csvFileStream(csvPath.c_str());
        
        if (!csvFileStream.is_open())
        {
            std::cerr << "Unable to open csv file " << csvPath << std::endl;
            return 1;
        }
        
        
        
        std::cout << "Max num of faces set to: " << videoDetector.getMaxNumberFaces() << std::endl;
        std::string mode;
        switch (videoDetector.getFaceDetectorMode())
        {
            case FaceDetectorMode::LARGE_FACES:
                mode = "LARGE_FACES";
                break;
            case FaceDetectorMode::SMALL_FACES:
                mode = "SMALL_FACES";
                break;
            default:
                break;
        }
        
        std::cout << "Face detector mode set to: " << mode << std::endl;
        shared_ptr<PlottingImageListener> listenPtr(new PlottingImageListener(csvFileStream, draw_display));
        
        //Activate all the detectors
        videoDetector.setDetectAllEmotions(true);
        videoDetector.setDetectAllExpressions(true);
        videoDetector.setDetectGender(true);
        videoDetector.setDetectGlasses(true);
        //Set the location of the data folder and license file
        videoDetector.setClassifierPath(DATA_FOLDER);
        videoDetector.setLicensePath(LICENSE_PATH);
        //Add callback functions implementations
        videoDetector.setImageListener(listenPtr.get());
        
        
        videoDetector.start();    //Initialize the detectors .. call only once
        
        do
        {
            shared_ptr<StatusListener> videoListenPtr = std::make_shared<StatusListener>();
            videoDetector.setProcessStatusListener(videoListenPtr.get());
            videoDetector.process(videoPath); //Process a video
            
            //For each frame processed
            while (videoListenPtr->isRunning())
            {
                if (listenPtr->getDataSize() > 0)
                {
                    std::pair<Frame, std::map<FaceId, Face> > dataPoint = listenPtr->getData();
                    Frame frame = dataPoint.first;
                    std::map<FaceId, Face> faces = dataPoint.second;
                    
                    
                    //Draw on the GUI
                    if (draw_display)
                    {
                        listenPtr->draw(faces, frame);
                    }
                    
                    std::cerr << "timestamp: " << frame.getTimestamp()
                    << " cfps: " << listenPtr->getCaptureFrameRate()
                    << " pfps: " << listenPtr->getProcessingFrameRate()
                    << " faces: "<< faces.size() << endl;
                    
                    //Output metrics to file
                    listenPtr->outputToFile(faces, frame.getTimestamp());
                }
            }
        } while(loop);
        
        videoDetector.stop();
        csvFileStream.close();
        
        std::cout << "Output written to file: " << csvPath << std::endl;
    }
    catch (AffdexException ex)
    {
        std::cerr << ex.what();
    }
    
    return 0;
}
