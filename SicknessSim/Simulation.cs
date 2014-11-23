using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace SicknessSim {
    internal class Simulation {
        private readonly QuadTree quadTree;
        private readonly Random rng;
        private double timePerTick;

        public Simulation(int popSize) {
            rng = new Random(1234);

            var Population = new List<Person>(popSize);
            for (var i = 0; i < popSize - Constants.IniitialInfected; i++) {
                var xPos = rng.Next(Constants.RoomSize);
                var yPos = rng.Next(Constants.RoomSize);
                var person = new Person(new Vector(xPos, yPos), Status.Healthy, rng);
                Population.Add(person);
            }

            for (var i = 0; i < Constants.IniitialInfected; i++) {
                var xPos = rng.Next(Constants.RoomSize);
                var yPos = rng.Next(Constants.RoomSize);
                var person = new Person(new Vector(xPos, yPos), Status.Infectious, rng) {TimeInfected = 0};
                Population.Add(person);
            }

            Time = 0;
            SimulationFinished = false;
            

            quadTree = new QuadTree(Constants.RoomSize, Constants.RoomSize);
            foreach (var person in Population) {
                quadTree.Insert(person);
            }
        }

        public bool SimulationFinished { get; private set; }

        public int Time { get; private set; }

        public List<Person> Persons {
            get { return quadTree.AllPersons; }
        }

        public Random Rng {
            get { return rng; }
        }

        public void AddPerson(Person p) {
            quadTree.Insert(p);
        }

        private IEnumerable<Person> findPersonsInInfluenceRadius(Person p) {
            const int r = Constants.InfluenceRadius;
            var origin = p.Position - r;
            const int length = r * 2;
            var points = quadTree.Query(new Rect(origin.X, origin.Y, length, length));

            return points.Where(person =>
                                //  Only healthy persons can get infected
                                person.Status == Status.Healthy &&
                                person.DistanceTo(p) <= Constants.InfluenceRadius);
        }

        private double approxRollingAverage(double avg, double new_sample) {
            avg -= avg / Time;
            avg += new_sample / Time;

            return avg;
        }

        public void Tick() {
            var sw = Stopwatch.StartNew();

            quadTree.Refresh();
            quadTree.Visit((person, nodeId) => {
                person.Tick(Time);

                if (person.Status != Status.Healthy) {
                    var influenced = findPersonsInInfluenceRadius(person).ToList();
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
                        default:
                            Debug.Fail("A healthy person is sick?");
                            break;
                    }

                    foreach (var p in influenced) {
                        var t = rng.NextDouble();

                        if (t <= rate) {
                            p.Status = Status.Infectious;
                            p.TimeInfected = Time;
                        }
                    }

                    if (person.Status == Status.Dead && Time >= person.TimeDied + Constants.RemoveDeadAfter) {
                        person.ToBeRemoved = true;
                    }
                }
            });

            var infectedCount = quadTree.AllPersons.Count(p => p.Status != Status.Healthy);
            if (infectedCount == 0) {
                SimulationFinished = true;
                return;
            }

            if (quadTree.AllPersons.Count == 0) {
                SimulationFinished = true;
                return;
            }
            Time++;
            sw.Stop();

            timePerTick = approxRollingAverage(timePerTick, sw.ElapsedMilliseconds);
            if (Time % 30 == 0) {
                Console.WriteLine("t_tick = " + timePerTick);
            }
        }
    }
}