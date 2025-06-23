#pragma once
#include <iostream>
#include <vector>
#ifdef _WIN32
    #define ARM_API __declspec(dllexport)
#else
    #define ARM_API __attribute__((visibility("default")))
#endif

struct Vec3D {
    double x, y, z;
    Vec3D(double x_ = 0, double y_ = 0, double z_ = 0) : x(x_), y(y_), z(z_) {}

    Vec3D operator+(const Vec3D& other) const { 
        return Vec3D(x + other.x, y + other.y, z + other.z); 
    }
    Vec3D operator*(double k) const { 
        return Vec3D(x * k,y * k, z * k); 
    }

    void print() const { 
        std::cout << "(" << x << ", " << y << ", " << z << ")"; 
    }
};


extern "C" {
    ARM_API void* Arm_Create (double base_x, double base_y, double base_z);
    ARM_API void Arm_SetAngles(void* arm, const double* angles, int count);
    ARM_API void Arm_GetJointPos (void* arm, double* positions, int* count);
    ARM_API int Arm_GetJointCount(void* arm);
    ARM_API void Arm_Destroy (void* arm);
    ARM_API int Arm_SolveIK (void* arm,double target_x, double target_y, double target_z, double* angles, int count);
    ARM_API void Arm_Debug(void* arm);
}
