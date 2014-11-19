using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace SicknessSim {
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

    internal class Person: IDisposable {
        private static readonly List<bool> UsedCounter = new List<bool>();
        private static readonly object Lock = new object();
        private readonly Random rng;
        private double currentDieRate;
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
            currentDieRate = Constants.DieRate;
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

            return (Direction) f;
        }

        private Vector moveVector(Direction direction, int distance) {
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
            const int size = Constants.RoomSize;

            var clampedX = newPosition.X;
            var clampedY = newPosition.Y;

            if (clampedX < 0.0) {
                clampedX = size;
            }
            if (clampedY < 0.0) {
                clampedY = size;
            }

            if (clampedX > size) {
                clampedX = 0;
            }
            if (clampedY > size) {
                clampedY = 0;
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

        public int DistanceTo(Person person) {
            return Position.DistanceTo(person.Position);
        }

        public override bool Equals(object obj) {
            var person = obj as Person;
            if (person == null) return false;
            return person.Id == this.Id;
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
                        // Console.WriteLine("Person {0} was infected at {1} and became sick at {2}", Id, TimeInfected, t);
                        Status = Status.Sick;
                        TimeSick = t;
                    }
                    break;
                case Status.Sick:
                    if (t >= TimeSick + Constants.TimeSick) {
                        var u = rng.NextDouble();
                        if (u <= currentDieRate) {
                            //  Console.WriteLine("Person {0} became sick at {1} and died at {2}", Id, TimeSick, t);
                            Status = Status.Dead;
                            TimeDied = t;
                        }
                        else {
                            currentDieRate *= 1.1;
                        }
                    }
                    break;
            }
        }
    }
}