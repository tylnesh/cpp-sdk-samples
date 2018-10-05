#include "PlottingImageListener.h"
#include "StatusListener.h"
#include "LocationsConfig.h"

#include <Platform.h>
#include <FrameDetector.h>
#include <SyncFrameDetector.h>

#include <opencv2/highgui/highgui.hpp>
#include <boost/filesystem.hpp>
#include <boost/program_options.hpp>

#include <iostream>
#include <iomanip>

using namespace std;
using namespace affdex;

class VideoReader {
public:
    VideoReader(const boost::filesystem::path& file_path, const unsigned int sampling_frame_rate) :
        sampling_frame_rate(sampling_frame_rate) {

        if (!(sampling_frame_rate == -1 || sampling_frame_rate > 0))
            throw runtime_error("Specified sampling rate is <= 0");

        last_timestamp_ms = 0 - 1 / sampling_frame_rate; // Initialize it so we still get timestamp 0 with sampling


        std::set<boost::filesystem::path> SUPPORTED_EXTS = {
            // Videos
            boost::filesystem::path(".avi"),
            boost::filesystem::path(".mov"),
            boost::filesystem::path(".flv"),
            boost::filesystem::path(".webm"),
            boost::filesystem::path(".wmv"),
            boost::filesystem::path(".mp4"),

            // Supported image formats
            // check https://docs.opencv.org/2.4/modules/highgui/doc/reading_and_writing_images_and_video.html#imread
            boost::filesystem::path(".bmp"),
            boost::filesystem::path(".jpeg"),
            boost::filesystem::path(".jpg"),
            boost::filesystem::path(".png"),
            boost::filesystem::path(".pbm"),
            boost::filesystem::path(".pgm"),
            boost::filesystem::path(".sr"),
            boost::filesystem::path(".ras"),
            boost::filesystem::path(".tiff"),
            boost::filesystem::path(".tif")
        };

        boost::filesystem::path ext = file_path.extension();
        if (SUPPORTED_EXTS.find(ext) == SUPPORTED_EXTS.end()) {
            throw runtime_error("Unsupported file extension: " + ext.string());
        }

        cap.open(file_path.string());
        if (!cap.isOpened())
            throw runtime_error("Error opening video/image file: " + file_path.string());
    }

    bool GetFrame(cv::Mat &bgr_frame, timestamp& timestamp_ms) {
        bool frame_data_loaded;

        do {
            frame_data_loaded = GetFrameData(bgr_frame, timestamp_ms);
        } while ((sampling_frame_rate > 0)
            && (timestamp_ms > 0)
            && ((timestamp_ms - last_timestamp_ms) < 1000 / sampling_frame_rate)
            && frame_data_loaded);

        last_timestamp_ms = timestamp_ms;
        return frame_data_loaded;
    }

    bool GetFrameData(cv::Mat &bgr_frame, timestamp& timestamp_ms) {
        static const int MAX_ATTEMPTS = 2;
        timestamp prev_timestamp_ms = cap.get(::CV_CAP_PROP_POS_MSEC);
        bool frame_found = cap.grab();
        bool frame_retrieved = cap.retrieve(bgr_frame);
        timestamp_ms = cap.get(::CV_CAP_PROP_POS_MSEC);

        // Two conditions result in failure to decode (grab/retrieve) a video frame (timestamp reports 0):
        // (1) error on a particular frame
        // (2) end of the video file
        //
        // This workaround double-checks that a subsequent frame can't be decoded, in the absence
        // of better reporting on which case has been encountered.
        //
        // In the case of reading an image, first attempt will not return a new frame, but the second one will
        // succeed. So as a workaround, the new timestamp must be greater than the previous one.
        int n_attempts = 0;
        while (!(frame_found && frame_retrieved) && n_attempts++ < MAX_ATTEMPTS) {
            frame_found = cap.grab();
            frame_retrieved = cap.retrieve(bgr_frame);
            timestamp_ms = cap.get(::CV_CAP_PROP_POS_MSEC);
        }

        if (frame_found && frame_retrieved && n_attempts > 0) {
            if (timestamp_ms <= prev_timestamp_ms) {
                frame_found = false;
            }
        }

        return frame_found && frame_retrieved;
    }

private:

    cv::VideoCapture cap;
    timestamp last_timestamp_ms;
    unsigned int sampling_frame_rate;
};


int main(int argsc, char ** argsv) {

    const int precision = 2;
    std::cerr << std::fixed << std::setprecision(precision);
    std::cout << std::fixed << std::setprecision(precision);

    // cmd line args
    affdex::path data_dir;
    affdex::path video_path;
    affdex::path locations_file;
    unsigned int sampling_frame_rate;
    unsigned int processing_frame_rate = 0;
    bool draw_display;
    unsigned int num_faces;
    bool sync = false;
    bool loop = false;
    bool draw_id = false;
    bool enable_logging = false;

    namespace po = boost::program_options; // abbreviate namespace

    po::options_description description("Project for demoing the Affectiva FrameDetector class (processing video files).");
    description.add_options()
    ("help,h", po::bool_switch()->default_value(false), "Display this help message.")
#ifdef _WIN32
    ("data,d", po::wvalue<affdex::path>(&data_dir)->default_value(affdex::path(L"data"), std::string("data")), "Path to the data folder")
    ("input,i", po::wvalue<affdex::path>(&video_path)->required(), "Video file to processs")
#else // _WIN32
    ("data,d", po::value< affdex::path >(&data_dir)->default_value(affdex::path("data"), std::string("data")), "Path to the data folder")
    ("input,i", po::value< affdex::path >(&video_path)->required(), "Video file to processs")
#endif // _WIN32
    ("sfps", po::value<unsigned int>(&sampling_frame_rate)->default_value(Detector::DEFAULT_PROCESSING_FRAMERATE), "Input sampling frame rate.")
    ("pfps", po::value<unsigned int>(&processing_frame_rate), "Max processing frame rate.")
    ("draw", po::value<bool>(&draw_display)->default_value(true), "Draw video on screen.")
    ("numFaces", po::value<unsigned int>(&num_faces)->default_value(1), "Number of faces to be tracked.")
    ("sync", po::bool_switch(&sync)->default_value(false), "Process frames synchronously.")
#ifdef _WIN32
    ("locations", po::wvalue< affdex::path >(&locations_file), "Path to the file containing occupant location configurations.")
#else //  _WIN32
        ("locations", po::value< affdex::path >(&locations_file), "Path to the file containing occupant location configurations.")
#endif // _WIN32
    ("loop", po::bool_switch(&loop)->default_value(false), "Loop over the video being processed.")
    ("log", po::bool_switch(&enable_logging)->default_value(false), "Enable logging to console")
    ("face_id", po::bool_switch(&draw_id)->default_value(false), "Draw face id on screen. Note: Drawing to screen should be enabled.")
    ;

    po::variables_map args;

    try {
        po::store(po::command_line_parser(argsc, argsv).options(description).run(), args);
        if (args["help"].as<bool>()) {
            std::cout << description << std::endl;
            LocationsConfig::printHelpMessage();
            return 0;
        }
        po::notify(args);
    }
    catch (po::error& e) {
        std::cerr << "ERROR: " << e.what() << std::endl << std::endl;
        std::cerr << "For help, use the -h option." << std::endl << std::endl;
        return 1;
    }

    // if processing frame rate not specified, default to sampling frame rate
    if (processing_frame_rate ==0) {
        processing_frame_rate = sampling_frame_rate;
    }

    if (sampling_frame_rate > processing_frame_rate) {
        std::cerr << "Warning: sampling frame rate (" << sampling_frame_rate << ") should be <= processing frame rate ("  << processing_frame_rate << ") to avoid dropped frames\n";
    }

    // check the data directory
    if (!boost::filesystem::exists(data_dir)) {
        std::cerr << "Data directory doesn't exist: " << std::string(data_dir.begin(), data_dir.end()) << std::endl;
        std::cerr << description << std::endl;
        return 1;
    }

    if (draw_id && !draw_display) {
        std::cerr << "Can't draw face id while drawing to screen is disabled" << std::endl;
        std::cerr << description << std::endl;
        return 1;
    }

    unique_ptr<vision::Detector> detector;
    try {
        //initialize the output file
        boost::filesystem::path csv_path(video_path);
        csv_path.replace_extension(".csv");
        std::ofstream csv_file_stream(csv_path.c_str());

        if (!csv_file_stream.is_open()) {
            std::cerr << "Unable to open csv file " << csv_path << std::endl;
            return 1;
        }

        // if the sampling rate and the requested processing rate are the same, bump the processing rate
        // by one to overcome potential rounding issues when comparing inter-frame timestamp differences
        // against the processing rate
        if (sampling_frame_rate == processing_frame_rate)
            processing_frame_rate++;


        // create the FrameDetector
        if (sync) {
            detector = std::unique_ptr<vision::Detector>(new vision::SyncFrameDetector(data_dir, processing_frame_rate, num_faces));
        }
        else {
            detector = std::unique_ptr<vision::Detector>(new vision::FrameDetector(data_dir, processing_frame_rate, num_faces));
        }

        // configure the FrameDetector by enabling features
        detector->enable({ vision::Feature::EMOTIONS, vision::Feature::EXPRESSIONS });

        // start the detector
        detector->start();

        do {
            // the VideoReader will handle decoding frames from the input video file
            VideoReader video_reader(video_path, sampling_frame_rate);

            // prepare listeners
            PlottingImageListener image_listener(csv_file_stream, draw_display, enable_logging, draw_id);
            StatusListener status_listener;

            if (!image_listener.validate(detector->getSupportedExpressions()) ||
                !image_listener.validate(detector->getSupportedEmotions()) ||
                !image_listener.validate(detector->getSupportedMeasurements())) {
                return 1;
            }

            // if a locations config file was specified on the command line, parse its contents
            if (!locations_file.empty()) {
                if (!boost::filesystem::exists(locations_file)) {
                    std::cerr << "Locations file doesn't exist: " << std::string(locations_file.begin(), locations_file.end()) << std::endl << std::endl;;
                    std::cerr << description << std::endl;
                    return 1;
                }
                LocationsConfig locations_config(boost::filesystem::path(locations_file), image_listener.getLocationNames());

                for (auto pair : locations_config.locations) {
                    detector->setOccupantLocationRegion(pair.first, pair.second);
                }
            }

            // configure the FrameDetector by assigning listeners
            detector->setImageListener(&image_listener);
            detector->setProcessStatusListener(&status_listener);

            cv::Mat mat;
            timestamp timestamp_ms;
            while (video_reader.GetFrame(mat, timestamp_ms)) {
                // create a Frame from the video input and process it with the FrameDetector
                vision::Frame f(mat.size().width, mat.size().height, mat.data, vision::Frame::ColorFormat::BGR, timestamp_ms);
                if (sync) {
                    dynamic_cast<vision::SyncFrameDetector *>(detector.get())->process(f);
                }
                else {
                    dynamic_cast<vision::FrameDetector *>(detector.get())->process(f);
                }

                // Since a FrameDetector processes frames asynchronously, and video decoding frame rates are typically
                // faster than FrameDetector processing frame rates, some intervention is needed if we want to avoid
                // sending frames to the FrameDetector much faster than it can process them, resulting in a lot of dropped
                // frames.  So, if the video sampling frame rate <= the processing frame rate, we infer
                // that the intention is to process all frames, which we can ensure by waiting for each frame to be processed
                // before sending to the next one.

                if (!sync && sampling_frame_rate <= processing_frame_rate) {
                    image_listener.waitForResult();
                }

                image_listener.processResults();
            }

            cout << "******************************************************************" << endl
            << "Processed Frame count: " << image_listener.getProcessedFrames() << endl
            << "Frames w/faces: " << image_listener.getFramesWithFaces() << endl
            << "Percent of frames w/faces: " << image_listener.getFramesWithFacesPercent() << "%" << endl
            << "******************************************************************" << endl;

            detector->reset();

        } while (loop);

        detector->stop();
        csv_file_stream.close();

        std::cout << "Output written to file: " << csv_path << std::endl;
    }
    catch (std::exception& ex) {
        std::cerr << ex.what();
        detector->stop();
        return 1;
    }

    return 0;
}
