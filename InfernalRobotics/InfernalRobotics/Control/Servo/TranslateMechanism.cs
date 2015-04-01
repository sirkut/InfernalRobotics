using InfernalRobotics.Module;
using UnityEngine;

namespace InfernalRobotics.Control.Servo
{
    internal class TranslateMechanism : MechanismBase
    {
        public TranslateMechanism(MuMechToggle rawServo) :base(rawServo)
        {
        }

        public override float MinPositionLimit
        {
            get { return RawServo.minTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.translateMin, RawServo.translateMax);
                RawServo.minTweak = clamped;
            }
        }

        public override float MaxPositionLimit
        {
            get { return RawServo.maxTweak; }
            set
            {
                var clamped = Mathf.Clamp(value, RawServo.translateMin, RawServo.translateMax);
                RawServo.maxTweak = clamped;
            }
        }

        public override float DefaultSpeed
        {
            get { return RawServo.keyTranslateSpeed; }
        }
    }
}