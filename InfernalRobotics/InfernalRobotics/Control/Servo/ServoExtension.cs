﻿using System.Collections.Generic;
using System.Linq;
using InfernalRobotics.Module;

namespace InfernalRobotics.Control.Servo
{
    public static class ServoExtension
    {
        public static IList<IServo> ToServos(this Vessel vessel)
        {
            var toggles = vessel.FindPartModulesImplementing<MuMechToggle>();
            return BuildServos(toggles);
        }

        public static IList<IServo> ToServos(this Part part)
        {
            var toggles = part.Modules.OfType<MuMechToggle>();
            return BuildServos(toggles);
        }

        public static IList<IServo> GetChildServos(this Part part)
        {
            var toggles = part.GetComponentsInChildren<MuMechToggle>();

            return BuildServos(toggles);
        }

        public static IEnumerable<AvailablePart> InfernalParts(this List<AvailablePart> parts)
        {
            return (from avPart in parts.Where(p => p.partPrefab) 
                    let moduleItem = avPart.partPrefab.GetComponent<MuMechToggle>() 
                    where moduleItem 
                    select avPart).ToList();
        }

        private static IList<IServo> BuildServos(IEnumerable<MuMechToggle> toggles)
        {
            return toggles.Select(toggle => new Servo(toggle)).Cast<IServo>().ToList();
        }
    }
}