#pragma once


#include <iostream>
#include <memory>
#include <chrono>
#include <thread>
#include <mutex>
#include <fstream>
#include <boost/filesystem.hpp>
#include <boost/timer/timer.hpp>

#include "Visualizer.h"
#include "ImageListener.h"

using namespace affdex;

class PlottingImageListener : public vision::ImageListener
{

    std::mutex mMutex;
    std::deque<std::pair<vision::Frame, std::map<vision::FaceId, vision::Face> > > mDataArray;

    double mCaptureLastTS;
    double mCaptureFPS;
    double mProcessLastTS;
    double mProcessFPS;
    std::ofstream &fStream;
    std::chrono::time_point<std::chrono::system_clock> mStartT;

    Visualizer viz;

public:


    PlottingImageListener(std::ofstream &csv)
    : mCaptureLastTS(-1.0f), mCaptureFPS(-1.0f), mProcessLastTS(-1.0f), mProcessFPS(-1.0f), fStream(csv), mStartT(std::chrono::system_clock::now())
    {

        fStream << "TimeStamp,faceId,interocularDistance,glasses,age,ethnicity,gender,dominantEmoji,";
        for (auto angle : viz.HEAD_ANGLES) fStream << angle.second << ",";
        for (auto emotion : viz.EMOTIONS) fStream << emotion.second << ",";
        for (auto expression : viz.EXPRESSIONS) fStream << expression.second << ",";
        fStream << std::endl;
        fStream.precision(4);
        fStream << std::fixed;
    }

    double getProcessingFrameRate()
    {
        std::lock_guard<std::mutex> lg(mMutex);
        return mProcessFPS;
    }

    double getCaptureFrameRate()
    {
        std::lock_guard<std::mutex> lg(mMutex);
        return mCaptureFPS;
    }

    int getDataSize()
    {
        std::lock_guard<std::mutex> lg(mMutex);
        return mDataArray.size();

    }

    std::pair<vision::Frame, std::map<vision::FaceId, vision::Face>> getData()
    {
        std::lock_guard<std::mutex> lg(mMutex);
        std::pair<vision::Frame, std::map<vision::FaceId, vision::Face>> dpoint = mDataArray.front();
        mDataArray.pop_front();
        return dpoint;
    }

    void onImageResults(std::map<vision::FaceId, vision::Face> faces, vision::Frame image) override
    {
        std::lock_guard<std::mutex> lg(mMutex);
        mDataArray.push_back(std::pair<vision::Frame, std::map<vision::FaceId, vision::Face>>(image, faces));
        std::chrono::time_point<std::chrono::system_clock> now = std::chrono::system_clock::now();
        std::chrono::milliseconds milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(now - mStartT);
        double seconds = milliseconds.count() / 1000.f;
        mProcessFPS = 1.0f / (seconds - mProcessLastTS);
        mProcessLastTS = seconds;
    };

    void onImageCapture(vision::Frame image) override
    {
        std::lock_guard<std::mutex> lg(mMutex);
        mCaptureFPS = 1.0f / (image.getTimestamp() - mCaptureLastTS);
        mCaptureLastTS = image.getTimestamp();
    };

    void outputToFile(const std::map<vision::FaceId, vision::Face> faces, const double timeStamp)
    {
        if (faces.empty())
        {
            fStream << timeStamp << ",nan,nan,no,unknown,unknown,unknown,unknown,";
            for (auto angle : viz.HEAD_ANGLES) fStream << "nan,";
            for (auto emotion : viz.EMOTIONS) fStream << "nan,";
            for (auto expression : viz.EXPRESSIONS) fStream << "nan,";
            fStream << std::endl;
        }
        for (auto & face_id_pair : faces)
        {
            vision::Face f = face_id_pair.second;

            fStream << timeStamp << ","
            << f.getId() << ","
            << f.getConfidence() << ","
            << f.getMeasurements()[vision::Measurement::INTEROCULAR_DISTANCE] << ","
            << viz.GLASSES_MAP[f.getAppearances().glasses] << ","
            << viz.AGE_MAP[f.getAppearances().age] << ","
            << viz.ETHNICITY_MAP[f.getAppearances().ethnicity] << ","
            << viz.GENDER_MAP[f.getAppearances().gender] << ","
            << affdex::vision::emojiToString(f.getDominantEmoji()) << ",";

            auto measurements = f.getMeasurements();
            for (auto m: viz.HEAD_ANGLES) {
                fStream << measurements.at(m.first) << ",";
            }

            auto emotions = f.getEmotions();
            for (auto emo: viz.EMOTIONS) {
                fStream << emotions.at(emo.first) << ",";
            }

            auto expressions = f.getExpressions();
            for (auto exp: viz.EXPRESSIONS) {
                fStream << expressions.at(exp.first) << ",";
            }

            fStream << f.getQualities().at(vision::Quality::BRIGHTNESS) << ",";

            fStream << std::endl;
        }
    }

    void draw(const std::map<vision::FaceId, vision::Face> faces, vision::Frame image)
    {
        std::shared_ptr<unsigned char> imgdata = image.getBGRByteArray();
        cv::Mat img = cv::Mat(image.getHeight(), image.getWidth(), CV_8UC3, imgdata.get());
        viz.updateImage(img);

        for (auto & face_id_pair : faces)
        {
            vision::Face f = face_id_pair.second;

            // Draw Facial Landmarks Points
            viz.drawPoints(f.getFacePoints());

            // Draw bounding box
            auto bbox = f.getBoundingBox();
            float valence = f.getEmotions().at(vision::Emotion::VALENCE);
            viz.drawBoundingBox(bbox, valence);

            // Draw a face on screen
            viz.drawFaceMetrics(f);
        }

        viz.showImage();
        std::lock_guard<std::mutex> lg(mMutex);
    }

};
