using System.Collections.Generic;

namespace InfernalRobotics.Control
{
    public interface IPresetable
    {
        int Count { get; }

        void MovePrev();
        void MoveNext();
        void MoveTo(int presetIndex);
        void RemoveAt(int presetIndex);
        void Sort(IComparer<float> sorter = null);

        /// <summary>
        /// Persists the current presets to the save file
        /// </summary>
        /// <param name="semmetry">If the part is part of a symmetry group, should the changes get propagated to all parts?</param>
        void Save(bool semmetry = false);
        void Add(float? position = null);
        float this[int index] { get; set; }
    }
}