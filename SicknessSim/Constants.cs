using System.Windows.Media;

namespace SicknessSim {
    internal static class Constants {
        public const int IniitialInfected = 50;
        // Likelihood that you infect someone else in your influence radius
        public const double InfectiousInfectionRate = 0.02;
        public const double SickInfectionRate = 0.08;
        public const double DeadInfectionRate = 0.15;
        public const double RoomSize = 800.0;
        public const double InfluenceRadius = 25.0;
        public const int PopulationSize = 250;
        public const double MoveDistance = 2.0;
        public const int ChangeDirectionAfter = 10;
        public const int TimeInfectious = 50;
        public const int TimeSick = 50;
        public const int RemoveDeadAfter = 80;
        public const double DieRate = 0.1;

        public static readonly Color HealthyColor = Colors.Green;
        public static readonly Color InfectiousColor = Colors.Orange;
        public static readonly Color SickColor = Colors.Red;
        public static readonly Color DeadColor = Colors.Black;
    }
}