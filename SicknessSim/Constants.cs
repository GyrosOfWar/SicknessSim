using System.Windows.Media;

namespace SicknessSim {
    internal static class Constants {
        public const int PopulationSize = 2000;
        public const int IniitialInfected = 5;
        public const int InfluenceRadius = 25;

        public const double InfectiousInfectionRate = 0.01;
        public const double SickInfectionRate = 0.01;
        public const double DeadInfectionRate = 0.01;

        public const int RoomSize = 800;
        public const int MoveDistance = 2;

        public const int ChangeDirectionAfter = 10;
        public const int TimeInfectious = 50;
        public const int TimeSick = 20;
        public const int RemoveDeadAfter = 20;
        public const double DieRate = 0.0001;

        public static readonly Color HealthyColor = Colors.Green;
        public static readonly Color InfectiousColor = Colors.Orange;
        public static readonly Color SickColor = Colors.Red;
        public static readonly Color DeadColor = Colors.White;
    }
}