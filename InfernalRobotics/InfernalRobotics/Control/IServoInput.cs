﻿namespace InfernalRobotics.Control
{
    public interface IServoInput
    {
        string Forward { get; set; }
        string Reverse { get; set; }
    }
}