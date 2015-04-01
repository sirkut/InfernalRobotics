using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Control.Servo
{
    internal class RotatingMechanism : MechanismBase
    {
        public RotatingMechanism(MuMechToggle rawServo) :base (rawServo)
        {
        }

        public override float MinPositionLimit
        {
            get { return RawServo.minTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.rotateMin, RawServo.rotateMax);
                RawServo.minTweak = clamped;
            }
        }

        public override float MaxPositionLimit
        {
            get { return RawServo.maxTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.rotateMin, RawServo.rotateMax);
                RawServo.maxTweak = clamped;
            }
        }

        public override float DefaultSpeed
        {
            get { return RawServo.keyRotateSpeed; }
        }
    }
}