#pragma once


#include <iostream>
#include <memory>
#include <chrono>
#include <thread>
#include <mutex>
#include <fstream>
#include <map>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <boost/filesystem.hpp>
#include <boost/timer/timer.hpp>
#include <boost/program_options.hpp>
#include <boost/algorithm/string.hpp>



#include "ImageListener.h"


using namespace affdex;

class PlottingImageListener : public ImageListener
{
    
    std::mutex mMutex;
    std::deque<std::pair<Frame, std::map<FaceId, Face> > > mDataArray;
    
    double mCaptureLastTS;
    double mCaptureFPS;
    double mProcessLastTS;
    double mProcessFPS;
    std::ofstream &fStream;
    std::chrono::time_point<std::chrono::system_clock> mStartT;
    const bool mDrawDisplay;
    const int spacing = 10;
    const float font_size = 0.5f;
    const int font = cv::FONT_HERSHEY_COMPLEX_SMALL;
    
    std::vector<std::string> expressions;
    std::vector<std::string> emotions;
    std::vector<std::string> headAngles;
    
    std::map<affdex::Glasses, std::string> glassesMap;
    std::map<affdex::Gender, std::string> genderMap;
    
public:
    
    
    PlottingImageListener(std::ofstream &csv, const bool draw_display)
    : fStream(csv), mDrawDisplay(draw_display), mStartT(std::chrono::system_clock::now()),
      mCaptureLastTS(-1.0f), mCaptureFPS(-1.0f),
      mProcessLastTS(-1.0f), mProcessFPS(-1.0f)
    {
        expressions = {
            "smile", "innerBrowRaise", "browRaise", "browFurrow", "noseWrinkle",
            "upperLipRaise", "lipCornerDepressor", "chinRaise", "lipPucker", "lipPress",
            "lipSuck", "mouthOpen", "smirk", "eyeClosure", "attention"
        };
        
        emotions = {
            "joy", "fear", "disgust", "sadness", "anger",
            "surprise", "contempt", "valence", "engagement"
        };
        
        headAngles = { "pitch", "yaw", "roll" };
        
        genderMap = std::map<affdex::Gender, std::string> {
            { affdex::Gender::Male, "male" },
            { affdex::Gender::Female, "female" },
            { affdex::Gender::Unknown, "unknown" },
            
        };
        
        glassesMap = std::map<affdex::Glasses, std::string> {
            { affdex::Glasses::Yes, "glasses" },
            { affdex::Glasses::No, "no glasses" }
        };
        
        fStream << "TimeStamp,faceId,interocularDistance,glasses,gender,";
        for (std::string angle : headAngles) fStream << angle << ",";
        for (std::string emotion : emotions) fStream << emotion << ",";
        for (std::string expression : expressions) fStream << expression << ",";
        fStream << std::endl;
        fStream.precision(4);
        fStream << std::fixed;
    }
    
    FeaturePoint minPoint(VecFeaturePoint points)
    {
        VecFeaturePoint::iterator it = points.begin();
        FeaturePoint ret = *it;
        for (; it != points.end(); it++)
        {
            if (it->x < ret.x) ret.x = it->x;
            if (it->y < ret.y) ret.y = it->y;
        }
        return ret;
    };
    
    FeaturePoint maxPoint( VecFeaturePoint points)
    {
        VecFeaturePoint::iterator it = points.begin();
        FeaturePoint ret = *it;
        for (; it != points.end(); it++)
        {
            if (it->x > ret.x) ret.x = it->x;
            if (it->y > ret.y) ret.y = it->y;
        }
        return ret;
    };
    
    
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
    
    std::pair<Frame, std::map<FaceId, Face>> getData()
    {
        std::lock_guard<std::mutex> lg(mMutex);
        std::pair<Frame, std::map<FaceId, Face>> dpoint = mDataArray.front();
        mDataArray.pop_front();
        return dpoint;
    }
    
    void onImageResults(std::map<FaceId, Face> faces, Frame image) override
    {
        std::lock_guard<std::mutex> lg(mMutex);
        mDataArray.push_back(std::pair<Frame, std::map<FaceId, Face>> (image, faces) );
        std::chrono::time_point<std::chrono::system_clock> now = std::chrono::system_clock::now();
        std::chrono::milliseconds milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(now - mStartT);
        double seconds = milliseconds.count() / 1000.f;
        mProcessFPS = 1.0f / (seconds - mProcessLastTS);
        mProcessLastTS = seconds;
    };
    
    void onImageCapture(Frame image) override
    {
        std::lock_guard<std::mutex> lg(mMutex);
        mCaptureFPS = 1.0f / (image.getTimestamp() - mCaptureLastTS);
        mCaptureLastTS = image.getTimestamp();
    };
    
    void outputToFile(const std::map<FaceId, Face> faces, const double timeStamp)
    {
        if (faces.empty())
        {
            fStream << timeStamp << "nan,nan,no glasses,unknown,";
            for (std::string angle : headAngles) fStream << "nan,";
            for (std::string emotion : emotions) fStream << "nan,";
            for (std::string expression : expressions) fStream << "nan,";
            fStream << std::endl;
        }
        for (auto & face_id_pair : faces)
        {
            Face f = face_id_pair.second;
            
            fStream << timeStamp << ","
            << f.id << ","
            << f.measurements.interocularDistance << ","
            << glassesMap[f.appearance.glasses] << ","
            << genderMap[f.appearance.gender] << ",";
            
            float *values = (float *)&f.measurements.orientation;
            for (std::string angle : headAngles)
            {
                fStream << (*values) << ",";
                values++;
            }
            
            values = (float *)&f.emotions;
            for (std::string emotion : emotions)
            {
                fStream << (*values) << ",";
                values++;
            }
            
            values = (float *)&f.expressions;
            for (std::string expression : expressions)
            {
                fStream << (*values) << ",";
                values++;
            }
            fStream << std::endl;
        }
    }
    
    void drawValues(const float * first, const std::vector<std::string> names,
                    const int x, int &padding, const cv::Scalar clr,
                    cv::Mat img)
    {
        for (std::string name : names)
        {
            if ((*first) > 5.0f)
            {
                char m[50];
                sprintf(m, "%s: %3.2f", name.c_str(), (*first));
                cv::putText(img, m, cv::Point(x, padding += spacing), font, font_size, clr);
            }
            first++;
        }
    }
    
    void draw(const std::map<FaceId, Face> faces, Frame image)
    {
        std::shared_ptr<byte> imgdata = image.getBGRByteArray();
        cv::Mat img = cv::Mat(image.getHeight(), image.getWidth(), CV_8UC3, imgdata.get());
        
        const int left_margin = 30;
        
        
        cv::Scalar clr = cv::Scalar(255, 255, 255);
        cv::Scalar header_clr = cv::Scalar(255, 0, 0);
        
        for (auto & face_id_pair : faces)
        {
            Face f = face_id_pair.second;
            VecFeaturePoint points = f.featurePoints;
            for (auto& point : points)    //Draw face feature points.
            {
                cv::circle(img, cv::Point(point.x, point.y), 2.0f, cv::Scalar(0, 0, 255));
            }
            FeaturePoint tl = minPoint(points);
            FeaturePoint br = maxPoint(points);
            
            //Output the results of the different classifiers.
            int padding = tl.y + 10;
            
            cv::putText(img, "APPEARANCE", cv::Point(br.x, padding += (spacing * 2)), font, font_size, header_clr);
            cv::putText(img, genderMap[f.appearance.gender], cv::Point(br.x, padding += spacing), font, font_size, clr);
            cv::putText(img, glassesMap[f.appearance.glasses], cv::Point(br.x, padding += spacing), font, font_size, clr);
            
            
            
            Orientation headAngles = f.measurements.orientation;
            
            char strAngles[100];
            sprintf(strAngles, "Pitch: %3.2f Yaw: %3.2f Roll: %3.2f Interocular: %3.2f",
                    headAngles.pitch, headAngles.yaw, headAngles.roll, f.measurements.interocularDistance);
            
            
            
            char fId[10];
            sprintf(fId, "ID: %i", f.id);
            cv::putText(img, fId, cv::Point(br.x, padding += spacing), font, font_size, clr);
            
            cv::putText(img, "MEASUREMENTS", cv::Point(br.x, padding += (spacing * 2)), font, font_size, header_clr);
            
            cv::putText(img, strAngles, cv::Point(br.x, padding += spacing), font, font_size, clr);
            
            cv::putText(img, "EXPRESSIONS", cv::Point(br.x, padding += (spacing * 2)), font, font_size, header_clr);
            
            drawValues((float *)&f.expressions, expressions, br.x, padding, clr, img);
            
            cv::putText(img, "EMOTIONS", cv::Point(br.x, padding += (spacing * 2)), font, font_size, header_clr);
            
            drawValues((float *)&f.emotions, emotions, br.x, padding, clr, img);
            
        }
        char fps_str[50];
        sprintf(fps_str, "capture fps: %2.0f", mCaptureFPS);
        cv::putText(img, fps_str, cv::Point(img.cols - 110, img.rows - left_margin - spacing), font, font_size, clr);
        sprintf(fps_str, "process fps: %2.0f", mProcessFPS);
        cv::putText(img, fps_str, cv::Point(img.cols - 110, img.rows - left_margin), font, font_size, clr);
        
        cv::imshow("analyze video", img);
        cv::waitKey(5);
    }
    
};
