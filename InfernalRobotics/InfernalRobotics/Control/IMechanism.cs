namespace InfernalRobotics.Control
{
    public interface IMechanism
    {
        float Position { get; }
        float MinPosition { get; }
        float MinPositionLimit { get; set; }

        float MaxPosition { get; }
        float MaxPositionLimit { get; set; }

        bool IsMoving { get; }
        bool IsFreeMoving { get; }
        bool IsLocked { get; set; }
        float AccelerationLimit { get; set; }
        bool IsAxisInverted { get; set; }

        /// <summary>
        /// the speed from part.cfg is used as the default unit of speed
        /// </summary>
        float DefaultSpeed { get; }

        /// <summary>
        /// The current rate of travel, like right now
        /// </summary>
        float CurrentSpeed { get; }

        /// <summary>
        /// The maximum speed that the servo can travel
        /// </summary>
        float MaxSpeed { get; }

        /// <summary>
        /// User setting to limit the speed of the servo
        /// </summary>
        float SpeedLimit { get; set; }

        void MoveLeft();
        void MoveCenter();
        void MoveRight();
        void Stop();
        void MoveTo(float position);
        void MoveTo(float position, float speed);
        void Reconfigure();

        /// <summary>
        /// Used in the editor to reset a parts state
        /// </summary>
        void Reset();
    }
}