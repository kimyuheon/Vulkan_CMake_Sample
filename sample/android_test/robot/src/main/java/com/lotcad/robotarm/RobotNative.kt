package com.lotcad.robotarm

// 공용 로봇 로직(sample/shared/robot_demo) 의 JNI 브릿지 — robot/src/main/cpp/robot_jni.cpp.
// 함수 하나하나는 robot_demo.h 의 RobotDemo_* 와 1:1.
object RobotNative {
    init { System.loadLibrary("lotcad") }

    /** models/demo_arm.urdf 를 불러와 화면에 맞춘다. 엔진이 준비된 뒤에 부른다. */
    external fun load(): Boolean

    // 조작 항목 — 회전 관절은 도(°), 손가락들은 "그리퍼" 하나(%)로 묶여 있다.
    external fun controlCount(): Int
    external fun controlLabel(i: Int): String
    external fun controlUnit(i: Int): String
    external fun controlMin(i: Int): Float
    external fun controlMax(i: Int): Float
    external fun getControl(i: Int): Float
    /** 손으로 움직이면 자동 재생은 멈춘다. */
    external fun setControl(i: Int, v: Float)

    external fun home()
    external fun setPlaying(on: Boolean)
    external fun isPlaying(): Boolean
    /** 매 프레임 nativeTick 직전 — 자동 재생을 진행한다. */
    external fun update(dt: Double)
}
