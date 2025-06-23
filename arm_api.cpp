#include "arm_api.h"
#include <vector>
#include <cmath>
#include <iostream>
#include <algorithm>
#define M_PI 3.14159265358979323846

struct AngleLimit {
    double low, high;
    bool   enabled;

    AngleLimit(double l = -M_PI, double h =  M_PI, bool en = true)
        : low(l), high(h), enabled(en) {}

    double apply(double a) const {
        return enabled ? std::clamp(a, low, high) : a;
    }
};

static Vec3D rotX(const Vec3D& p, double a) {
    double c = std::cos(a), s = std::sin(a);
    return { p.x, p.y * c - p.z * s, p.y * s + p.z * c };
}
static Vec3D rotY(const Vec3D& p, double a) {
    double c = std::cos(a), s = std::sin(a);
    return { p.x * c + p.z * s, p.y, -p.x * s + p.z * c };
}
static Vec3D rotZ(const Vec3D& p, double a) {
    double c = std::cos(a), s = std::sin(a);
    return { p.x * c - p.y * s, p.x * s + p.y * c, p.z };
}

class ArmJoint {
public:
    enum Axis { X, Y, Z };

    ArmJoint(Axis ax, double init, AngleLimit lim)
        : axis(ax), angle(init), lims(lim) {}

    void set(double a){
        angle = lims.apply(a); 
    }
    double get() const {
        return angle; 
    }

    Vec3D rotate(const Vec3D& d) const {
        switch (axis) {
            case X: return rotX(d, angle);
            case Y: return rotY(d, angle);
            case Z: return rotZ(d, angle);
        }
        return d;
    }

private:
    Axis axis;
    double angle;
    AngleLimit lims;
};

struct ArmLink {
    double len;
    Vec3D  dir;
    ArmLink(double l, Vec3D d) : len(l), dir(d) {}
};

class ArmManipulator {
public:
    explicit ArmManipulator(Vec3D base = {}) : basePos(base) { configure(); }

    void setAngles(const std::vector<double>&);
    std::vector<double> angles() const;
    std::vector<Vec3D> jointsWorld() const;
    Vec3D endEffector() const { return forward(); }
    bool solveIK(const Vec3D& tgt,std::vector<double>& out);
    size_t count() const { 
        return joints.size();
    }
    void debugPrint() const;

private:
    void configure();
    Vec3D forward() const;
    Vec3D basePos;
    std::vector<ArmJoint> joints;
    std::vector<ArmLink> links;
};

void ArmManipulator::configure() {
    joints.clear(); links.clear();

    joints.push_back(ArmJoint(ArmJoint::Z, 0.0, AngleLimit(-M_PI, M_PI)));
    joints.push_back(ArmJoint(ArmJoint::Y, 0.0, AngleLimit(-M_PI/2, M_PI/2)));
    joints.push_back(ArmJoint(ArmJoint::X, 0.0, AngleLimit(0, 8)));
    joints.push_back(ArmJoint(ArmJoint::Y, 0.0, AngleLimit(-M_PI, M_PI)));

    links.push_back(ArmLink(2.0, Vec3D(0, 1, 0)));
    links.push_back(ArmLink(3.0, Vec3D(0, 1, 0)));
    links.push_back(ArmLink(2.5, Vec3D(0, 1, 0)));
    links.push_back(ArmLink(1.0, Vec3D(0, 1, 0)));
}

void ArmManipulator::setAngles(const std::vector<double>& a) {
    for (size_t i = 0; i < std::min(a.size(), joints.size()); ++i)
        joints[i].set(a[i]);
}

std::vector<double> ArmManipulator::angles() const {
    std::vector<double> v;
    for (const auto& j : joints) v.push_back(j.get());
    return v;
}

Vec3D ArmManipulator::forward() const {
    Vec3D pos = basePos;
    
    for (size_t i = 0; i < joints.size(); ++i) {
        Vec3D linkDir = links[i].dir;
        for (size_t j = 0; j <= i; ++j) {
            linkDir = joints[j].rotate(linkDir);
        }
        
        pos = pos + linkDir * links[i].len;
    }
    return pos;
}

std::vector<Vec3D> ArmManipulator::jointsWorld() const {
    std::vector<Vec3D> out;
    out.push_back(basePos);
    
    Vec3D pos = basePos;
    
    for (size_t i = 0; i < joints.size(); ++i) {
        Vec3D linkDir = links[i].dir;
        for (size_t j = 0; j <= i; ++j) {
            linkDir = joints[j].rotate(linkDir);
        }
        
        pos = pos + linkDir * links[i].len;
        out.push_back(pos);
    }
    return out;
}

bool ArmManipulator::solveIK(const Vec3D& tgt, std::vector<double>& out) {
    const double tol = 0.01, lr = 0.01;
    const int    maxIt = 100;
    out = angles();
    Vec3D cur = endEffector();

    auto sqDist = [](const Vec3D& a, const Vec3D& b){
        return (a.x-b.x)*(a.x-b.x) + (a.y-b.y)*(a.y-b.y) + (a.z-b.z)*(a.z-b.z);
    };

    for (int it = 0; it < maxIt; ++it) {
        if (std::sqrt(sqDist(cur, tgt)) < tol) { setAngles(out); return true; }

        std::vector<double> grad(out.size());
        for (size_t i = 0; i < out.size(); ++i) {
            double backup = out[i];
            out[i] += 0.001;
            setAngles(out);
            Vec3D nxt = endEffector();
            grad[i] = (sqDist(nxt,tgt) - sqDist(cur,tgt)) / 0.001;
            out[i] = backup;
        }
        for (size_t i = 0; i < out.size(); ++i) {
            out[i] -= lr * grad[i];
        }
        setAngles(out);
        cur = endEffector();
    }
    return false;
}
void ArmManipulator::debugPrint() const {
    std::cout << "=== ARM DEBUG INFO ===" << std::endl;
    std::cout << "Base position: (" << basePos.x << ", " << basePos.y << ", " << basePos.z << ")" << std::endl;
    
    std::cout << "Joint angles (radians):" << std::endl;
    for (size_t i = 0; i < joints.size(); ++i) {
        std::cout << "  Joint " << i << ": " << joints[i].get() << " rad (" << (joints[i].get() * 180.0 / M_PI) << " deg)" << std::endl;
    }
    
    std::cout << "Link lengths:" << std::endl;
    for (size_t i = 0; i < links.size(); ++i) {
        std::cout << "  Link " << i << ": " << links[i].len << std::endl;
    }
    
    std::cout << "Joint positions:" << std::endl;
    auto positions = jointsWorld();
    for (size_t i = 0; i < positions.size(); ++i) {
        std::cout << "  Position " << i << ": (" << positions[i].x << ", " << positions[i].y << ", " << positions[i].z << ")" << std::endl;
    }
    
    std::cout << "End effector: ";
    auto ee = endEffector();
    std::cout << "(" << ee.x << ", " << ee.y << ", " << ee.z << ")" << std::endl;
    
    double maxReach = 0;
    for (const auto& link : links) {
        maxReach += link.len;
    }
    std::cout << "Maximum reach: " << maxReach << std::endl;
    std::cout << "===================" << std::endl;
}

extern "C" {

void* Arm_Create(double x, double y, double z) {
    return new ArmManipulator(Vec3D(x,y,z));
}

void Arm_SetAngles(void* h, const double* a, int n) {
    if (!h) return;
    std::vector<double> v(a, a+n);
    static_cast<ArmManipulator*>(h)->setAngles(v);
}

void Arm_GetJointPos(void* h, double* p, int* cnt) {
    if (!h) return;
    auto vec = static_cast<ArmManipulator*>(h)->jointsWorld();
    *cnt = static_cast<int>(vec.size()*3);
    for (size_t i = 0; i < vec.size(); ++i) {
        p[i*3]   = vec[i].x;
        p[i*3+1] = vec[i].y;
        p[i*3+2] = vec[i].z;
    }
}

int Arm_GetJointCount(void* h) {
    return h ? static_cast<int>(static_cast<ArmManipulator*>(h)->count()) : 0;
}

void Arm_Destroy(void* h) {
    delete static_cast<ArmManipulator*>(h);
}

int Arm_SolveIK(void* h, double tx, double ty, double tz, double* ang, int n) {
    if (!h || static_cast<size_t>(n) != static_cast<ArmManipulator*>(h)->count()) return 0;
    std::vector<double> res(n);
    bool ok = static_cast<ArmManipulator*>(h)->solveIK({tx,ty,tz}, res);
    if (ok) for (int i = 0; i < n; ++i) ang[i] = res[i];
    return ok ? 1 : 0;
}
void Arm_Debug(void* h) {
        if (!h) return;
        static_cast<ArmManipulator*>(h)->debugPrint();
    }
    
}