using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace SicknessSim {
    internal static class Constants {
        public const int IniitialInfected = 50;
        // Likelihood that you infect someone else in your influence radius
        public const double InfectiousInfectionRate = 0.02;
        public const double SickInfectionRate = 0.08;
        public const double DeadInfectionRate = 0.1;
        public const double RoomSize = 800.0;
        public const double InfluenceRadius = 25.0;
        public const int PopulationSize = 250;
        public const double MoveDistance = 2.0;
        public const int ChangeDirectionAfter = 10;
        public const int TimeInfectious = 50;
        public const int TimeSick = 100;
        public const int RemoveDeadAfter = 20;

        public static readonly Color HealthyColor = Colors.Green;
        public static readonly Color InfectiousColor = Colors.Orange;
        public static readonly Color SickColor = Colors.Red;
        public static readonly Color DeadColor = Colors.Black;
    }

    // TODO maybe introudce Immune 
    // Healthy -> Infectious -> Sick -> Immune (not everyone dies)
    internal enum Status {
        Healthy,
        Infectious,
        Sick,
        Dead
    }

    internal enum Direction {
        Top = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    internal class Person : IDisposable {
        private static readonly List<bool> UsedCounter = new List<bool>();
        private static readonly object Lock = new object();
        private readonly Random rng;
        private Direction facingDirection;
        private int lastDirectionChange;

        public Person(Vector pos, Status status, Random rng) {
            lock (Lock) {
                var nextIndex = GetAvailableIndex();
                if (nextIndex == -1) {
                    nextIndex = UsedCounter.Count;
                    UsedCounter.Add(true);
                }

                Id = nextIndex;
            }
            Position = pos;
            Status = status;
            this.rng = rng;
            facingDirection = Direction.Right;
            // Change direction right after being created
            lastDirectionChange = -Constants.ChangeDirectionAfter;
            TimeInfected = null;
            TimeSick = null;
            TimeDied = null;
            ToBeRemoved = false;
        }

        public Vector Position { get; private set; }
        public Status Status { get; set; }
        public int Id { get; private set; }

        public int? TimeInfected { get; set; }
        public int? TimeDied { get; private set; }
        public int? TimeSick { get; set; }
        public bool ToBeRemoved { get; set; }

        public void Dispose() {
            lock (Lock) {
                UsedCounter[Id] = false;
            }
        }

        public override string ToString() {
            return Id + ": " + Status;
        }

        private static int GetAvailableIndex() {
            for (var i = 0; i < UsedCounter.Count; i++) {
                if (UsedCounter[i] == false) {
                    return i;
                }
            }

            // Nothing available.
            return -1;
        }

        private Direction randomDirection() {
            var f = rng.Next(4);

            return (Direction)f;
        }

        private Vector moveVector(Direction direction, double distance) {
            var newPosition = Vector.One;
            switch (direction) {
                case Direction.Top:
                    newPosition = Position + new Vector(0, -distance);
                    break;
                case Direction.Right:
                    newPosition = Position + new Vector(distance, 0);
                    break;
                case Direction.Down:
                    newPosition = Position + new Vector(0, distance);
                    break;
                case Direction.Left:
                    newPosition = Position + new Vector(-distance, 0);
                    break;
                default:
                    Debug.Fail("should never happen");
                    break;
            }
            const double size = Constants.RoomSize;

            var clampedX = newPosition.X;
            var clampedY = newPosition.Y;

            if (clampedX < 0.0) {
                clampedX = size;
            }
            if (clampedY < 0.0) {
                clampedY = size;
            }

            if (clampedX > size) {
                clampedX = 0.0;
            }
            if (clampedY > size) {
                clampedY = 0.0;
            }

            var newPositionClamped = new Vector(clampedX, clampedY);

            return newPositionClamped;
        }

        private void Move() {
            switch (Status) {
                case Status.Healthy:
                case Status.Infectious:
                    var newPosition = moveVector(facingDirection, Constants.MoveDistance);
                    Position = newPosition;
                    break;
                // Sick or dead people don't move
                case Status.Sick:
                case Status.Dead:
                    break;
            }
        }

        public double DistanceTo(Person person) {
            return Position.DistanceTo(person.Position);
        }

        public void Tick(int t) {
            if (Status != Status.Sick && Status != Status.Dead) {
                if (t >= lastDirectionChange + Constants.ChangeDirectionAfter) {
                    facingDirection = randomDirection();
                    lastDirectionChange = t;
                }
                Move();
            }

            switch (Status) {
                case Status.Healthy:
                    break;
                case Status.Infectious:
                    if (t >= TimeInfected + Constants.TimeInfectious) {
                        //Console.WriteLine("Person {0} was infected at {1} and became sick at {2}", Id, TimeInfected, t);
                        Console.WriteLine("Time infected: " + (t - TimeInfected));
                        Status = Status.Sick;
                        TimeSick = t;
                    }
                    break;
                case Status.Sick:
                    if (t >= TimeSick + Constants.TimeSick) {
                        //Console.WriteLine("Person {0} became sick at {1} and died at {2}", Id, TimeSick, t);
                        Console.WriteLine("Time died: " + (t - TimeSick));

                        Status = Status.Dead;
                        TimeDied = t;
                    }
                    break;
            }
        }
    }

    internal class Simulation {
        private readonly List<Person> Population;
        private readonly Random rng;
        private int time;

        public Simulation(int popSize) {
            rng = new Random();

            Population = new List<Person>();
            for (var i = 0; i < popSize - Constants.IniitialInfected; i++) {
                var xPos = rng.NextDouble() * Constants.RoomSize;
                var yPos = rng.NextDouble() * Constants.RoomSize;
                var person = new Person(new Vector(xPos, yPos), Status.Healthy, rng);
                Population.Add(person);
            }

            for (var i = 0; i < Constants.IniitialInfected; i++) {
                var xPos = rng.NextDouble() * Constants.RoomSize;
                var yPos = rng.NextDouble() * Constants.RoomSize;
                var person = new Person(new Vector(xPos, yPos), Status.Infectious, rng);
                Population.Add(person);
            }

            time = 0;
        }

        public int Time {
            get { return time; }
        }

        public List<Person> Persons {
            get { return Population; }
        }

        private IEnumerable<Person> findPersonsInInfluenceRadius(Person p) {
            return Population.Where(person =>
                // Only healthy persons can get infected
                person.Status == Status.Healthy &&
                person.DistanceTo(p) <= Constants.InfluenceRadius);
        }

        public void Tick() {
            foreach (var person in Population) {
                person.Tick(time);

                if (person.Status != Status.Healthy) {
                    var influenced = findPersonsInInfluenceRadius(person).ToList();
                    foreach (var p in influenced) {
                        var t = rng.NextDouble();
                        var rate = 0.0;
                        switch (person.Status) {
                            case Status.Infectious:
                                rate = Constants.InfectiousInfectionRate;
                                break;
                            case Status.Sick:
                                rate = Constants.SickInfectionRate;
                                break;
                            case Status.Dead:
                                rate = Constants.DeadInfectionRate;
                                break;
                            default: Debug.Fail("A healthy person is sick?");
                                break;
                        }

                        if (t <= rate) {
                            p.Status = Status.Infectious;
                            p.TimeInfected = time;
                            Console.WriteLine("Person {0} infected {1}", person.Id, p.Id);
                        }
                    }
                }

                if (person.Status == Status.Dead && time >= person.TimeDied + Constants.RemoveDeadAfter) {
                    person.ToBeRemoved = true;
                }
            }


            Population.RemoveAll(p => p.ToBeRemoved);
            if (time % 50 == 0) {
                var numInfected = Population.Count(p => p.Status == Status.Infectious || p.Status == Status.Sick || p.Status == Status.Dead);
                Console.WriteLine("#Infected at {0}: {1}", time, numInfected);
            }
            time++;
        }
    }
}