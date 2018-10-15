#pragma once

#include <Face.h>
#include <Detector.h>
#include <boost/filesystem/path.hpp>
#include <iostream>
#include <fstream>
#include <sstream>

class LocationsConfig {
public:
    LocationsConfig(const boost::filesystem::path & locations_file, std::map<affdex::vision::OccupantLocation, std::string> location_names) {
        std::ifstream location_stream(locations_file.c_str());
        std::string line;
        std::getline(location_stream, line); // read and discard the first line with the headers

        std::string s_location;
        affdex::vision::OccupantLocation loc;
        while (std::getline(location_stream, line)) {
            loc = affdex::vision::OccupantLocation::UNKNOWN;
            std::istringstream ss(line);
            std::getline(ss, s_location, ',');
            for (auto pair : location_names) {
                if (pair.second == s_location) {
                    loc = pair.first;
                    break;
                }
            }
            if (loc == affdex::vision::OccupantLocation::UNKNOWN) {
                std::cerr << "invalid location name (" << s_location << ") in location config file\n";
                exit(1);
            }

            char comma;
            float tl_x, tl_y, br_x, br_y;
            ss >> tl_x >> comma >> tl_y >> comma >> br_x >> comma >> br_y;

            locations[loc] = affdex::vision::BoundingBox({ { tl_x, tl_y },{ br_x,br_y } });
        }
    }

    static void printHelpMessage() {
        std::cout 
            << "When using the locations command line option, specify a configuration file\n"
            << "containing OccupantLocation regions.  This file should contain comma separated\n"
            << "values, with a header row followed by one or more data rows, each specifying the\n"
            << "following:\n\n"
            << "-an OccupantLocation enum value name\n"
            << "-the top left X and Y coordinates of the location bounding box\n"
            << "-the bottom right X and Y coordinates of the location bounding box\n\n"
            << "For example:\n\n"
            << "location,upperLeftX,upperLeftY,lowerRightX,lowerRightY\n"
            << "FIRST_ROW_DRIVER_SIDE,852,512,1280,768\n"
            << "FIRST_ROW_CENTER,426,512,852,768\n"
            << "FIRST_ROW_PASSENGER_SIDE,0,512,426,768\n"
            << "SECOND_ROW_LEFT,852,256,1280,512\n"
            << "SECOND_ROW_CENTER,426,256,852,512\n"
            << "SECOND_ROW_RIGHT,0,256,426,512\n"
            << "THIRD_ROW_LEFT,852,0,1280,256\n"
            << "THIRD_ROW_CENTER,426,0,852,256\n"
            << "THIRD_ROW_RIGHT,0,0,426,256\n";
    }

    std::map<affdex::vision::OccupantLocation, affdex::vision::BoundingBox> locations;
};
