#include <iostream>
#include <memory>
#include <chrono>
#include <fstream>
#include <boost/filesystem.hpp>
#include <boost/timer/timer.hpp>
#include <boost/program_options.hpp>
#include <boost/algorithm/string.hpp>

#include "Frame.h"
#include "Face.h"
#include "FrameDetector.h"
#include "AffdexException.h"

#include "AFaceListener.hpp"
#include "PlottingImageListener.hpp"
#include "StatusListener.hpp"

using namespace std;
using namespace affdex;

/// <summary>
/// Project for demoing the Windows SDK CameraDetector class (grabbing and processing frames from the camera).
/// </summary>
int main(int argsc, char ** argsv)
{
    namespace po = boost::program_options; // abbreviate namespace

    std::cerr << "Hit ESCAPE key to exit app.." << endl;
    shared_ptr<FrameDetector> frameDetector;

    try{

        const std::vector<int> DEFAULT_RESOLUTION{ 640, 480 };

        affdex::path DATA_FOLDER;

        std::vector<int> resolution;
        int process_framerate = 30;
        int camera_framerate = 15;
        int buffer_length = 2;
        int camera_id = 0;
        unsigned int nFaces = 1;
        bool draw_display = true;
        int faceDetectorMode = (int)FaceDetectorMode::LARGE_FACES;

        float last_timestamp = -1.0f;
        float capture_fps = -1.0f;

        const int precision = 2;
        std::cerr.precision(precision);
        std::cout.precision(precision);

        po::options_description description("Project for demoing the Affdex SDK CameraDetector class (grabbing and processing frames from the camera).");
        description.add_options()
            ("help,h", po::bool_switch()->default_value(false), "Display this help message.")
#ifdef _WIN32
            ("data,d", po::wvalue< affdex::path >(&DATA_FOLDER)->default_value(affdex::path(L"data"), std::string("data")), "Path to the data folder")
#else //  _WIN32
            ("data,d", po::value< affdex::path >(&DATA_FOLDER)->default_value(affdex::path("data"), std::string("data")), "Path to the data folder")
#endif // _WIN32
            ("resolution,r", po::value< std::vector<int> >(&resolution)->default_value(DEFAULT_RESOLUTION, "640 480")->multitoken(), "Resolution in pixels (2-values): width height")
            ("pfps", po::value< int >(&process_framerate)->default_value(30), "Processing framerate.")
            ("cfps", po::value< int >(&camera_framerate)->default_value(30), "Camera capture framerate.")
            ("bufferLen", po::value< int >(&buffer_length)->default_value(30), "process buffer size.")
            ("cid", po::value< int >(&camera_id)->default_value(0), "Camera ID.")
            ("faceMode", po::value< int >(&faceDetectorMode)->default_value((int)FaceDetectorMode::LARGE_FACES), "Face detector mode (large faces vs small faces).")
            ("numFaces", po::value< unsigned int >(&nFaces)->default_value(1), "Number of faces to be tracked.")
            ("draw", po::value< bool >(&draw_display)->default_value(true), "Draw metrics on screen.")
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

        if (!boost::filesystem::exists(DATA_FOLDER))
        {
            std::cerr << "Folder doesn't exist: " << std::string(DATA_FOLDER.begin(), DATA_FOLDER.end()) << std::endl << std::endl;;
            std::cerr << "Try specifying the folder through the command line" << std::endl;
            std::cerr << description << std::endl;
            return 1;
        }
        if (resolution.size() != 2)
        {
            std::cerr << "Only two numbers must be specified for resolution." << std::endl;
            return 1;
        }
        else if (resolution[0] <= 0 || resolution[1] <= 0)
        {
            std::cerr << "Resolutions must be positive number." << std::endl;
            return 1;
        }

        std::ofstream csvFileStream;

        std::cerr << "Initializing Affdex FrameDetector" << endl;
        shared_ptr<FaceListener> faceListenPtr(new AFaceListener());
        shared_ptr<PlottingImageListener> listenPtr(new PlottingImageListener(csvFileStream, draw_display));    // Instanciate the ImageListener class
        shared_ptr<StatusListener> videoListenPtr(new StatusListener());
        frameDetector = make_shared<FrameDetector>(buffer_length, process_framerate, nFaces, (affdex::FaceDetectorMode) faceDetectorMode);        // Init the FrameDetector Class

        //Initialize detectors
        frameDetector->setDetectAllEmotions(true);
        frameDetector->setDetectAllExpressions(true);
        frameDetector->setDetectAllEmojis(true);
        frameDetector->setDetectAllAppearances(true);
        frameDetector->setClassifierPath(DATA_FOLDER);
        frameDetector->setImageListener(listenPtr.get());
        frameDetector->setFaceListener(faceListenPtr.get());
        frameDetector->setProcessStatusListener(videoListenPtr.get());

        cv::VideoCapture webcam(camera_id);    //Connect to the first webcam
        webcam.set(CV_CAP_PROP_FPS, camera_framerate);    //Set webcam framerate.
        webcam.set(CV_CAP_PROP_FRAME_WIDTH, resolution[0]);
        webcam.set(CV_CAP_PROP_FRAME_HEIGHT, resolution[1]);
        std::cerr << "Setting the webcam frame rate to: " << camera_framerate << std::endl;
        auto start_time = std::chrono::system_clock::now();
        if (!webcam.isOpened())
        {
            std::cerr << "Error opening webcam!" << std::endl;
            return 1;
        }

        std::cout << "Max num of faces set to: " << frameDetector->getMaxNumberFaces() << std::endl;
        std::string mode;
        switch (frameDetector->getFaceDetectorMode())
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

        //Start the frame detector thread.
        frameDetector->start();

        do{
            cv::Mat img;
            if (!webcam.read(img))    //Capture an image from the camera
            {
                std::cerr << "Failed to read frame from webcam! " << std::endl;
                break;
            }

            //Calculate the Image timestamp and the capture frame rate;
            const auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now() - start_time);
            const double seconds = milliseconds.count() / 1000.f;

            // Create a frame
            Frame f(img.size().width, img.size().height, img.data, Frame::COLOR_FORMAT::BGR, seconds);
            capture_fps = 1.0f / (seconds - last_timestamp);
            last_timestamp = seconds;
            frameDetector->process(f);  //Pass the frame to detector

            // For each frame processed
            if (listenPtr->getDataSize() > 0)
            {

                std::pair<Frame, std::map<FaceId, Face> > dataPoint = listenPtr->getData();
                Frame frame = dataPoint.first;
                std::map<FaceId, Face> faces = dataPoint.second;

                // Draw metrics to the GUI
                if (draw_display)
                {
                    listenPtr->draw(faces, frame);
                }

                std::cerr << "timestamp: " << frame.getTimestamp()
                    << " cfps: " << listenPtr->getCaptureFrameRate()
                    << " pfps: " << listenPtr->getProcessingFrameRate()
                    << " faces: " << faces.size() << endl;

                //Output metrics to the file
                //listenPtr->outputToFile(faces, frame.getTimestamp());
            }


        }

#ifdef _WIN32
        while (!GetAsyncKeyState(VK_ESCAPE) && videoListenPtr->isRunning());
#else //  _WIN32
        while (videoListenPtr->isRunning());//(cv::waitKey(20) != -1);
#endif
        std::cerr << "Stopping FrameDetector Thread" << endl;
        frameDetector->stop();    //Stop frame detector thread
    }
    catch (AffdexException ex)
    {
        std::cerr << "Encountered an AffdexException " << ex.what();
        return 1;
    }
    catch (std::runtime_error err)
    {
        std::cerr << "Encountered a runtime error " << err.what();
        return 1;
    }
    catch (std::exception ex)
    {
        std::cerr << "Encountered an exception " << ex.what();
        return 1;
    }
    catch (...)
    {
        std::cerr << "Encountered an unhandled exception ";
        return 1;
    }

    return 0;
}
