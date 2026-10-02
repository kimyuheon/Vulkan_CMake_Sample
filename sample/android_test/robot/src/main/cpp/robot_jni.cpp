// JNI 브릿지 — Kotlin RobotNative(object) ↔ 공용 로봇 로직(sample/shared/robot_demo).
// 패키지: com.vulkancad.robotarm → Java_com_vulkancad_robotarm_RobotNative_<method>
// 엔진 .so(libvulkancad)에 같이 빌드된다 — :engine 의 CMakeLists.txt 참고.
#include <jni.h>

#include "robot_demo.h"

extern "C" {

#define JNI(ret, name) JNIEXPORT ret JNICALL Java_com_vulkancad_robotarm_RobotNative_##name

JNI(jboolean, load)(JNIEnv*, jobject) { return RobotDemo_Load() ? JNI_TRUE : JNI_FALSE; }

JNI(jint, controlCount)(JNIEnv*, jobject) { return RobotDemo_ControlCount(); }
JNI(jstring, controlLabel)(JNIEnv* env, jobject, jint i) { return env->NewStringUTF(RobotDemo_ControlLabel(i)); }
JNI(jstring, controlUnit)(JNIEnv* env, jobject, jint i)  { return env->NewStringUTF(RobotDemo_ControlUnit(i)); }
JNI(jfloat, controlMin)(JNIEnv*, jobject, jint i) { return RobotDemo_ControlMin(i); }
JNI(jfloat, controlMax)(JNIEnv*, jobject, jint i) { return RobotDemo_ControlMax(i); }
JNI(jfloat, getControl)(JNIEnv*, jobject, jint i) { return RobotDemo_GetControl(i); }
JNI(void, setControl)(JNIEnv*, jobject, jint i, jfloat v) { RobotDemo_SetControl(i, v); }

JNI(void, home)(JNIEnv*, jobject) { RobotDemo_Home(); }
JNI(void, setPlaying)(JNIEnv*, jobject, jboolean on) { RobotDemo_SetPlaying(on == JNI_TRUE); }
JNI(jboolean, isPlaying)(JNIEnv*, jobject) { return RobotDemo_IsPlaying() ? JNI_TRUE : JNI_FALSE; }
JNI(void, update)(JNIEnv*, jobject, jdouble dt) { RobotDemo_Update(dt); }

#undef JNI
} // extern "C"
