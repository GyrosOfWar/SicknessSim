using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SicknessSim {
    internal class Simulation {
        //private readonly List<Person> Population;
        private readonly Random rng;
        private int time;
        private double averageNeighbors;
        private double numSamples;
        private QuadTree quadTree;

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
                var person = new Person(new Vector(xPos, yPos), Status.Infectious, rng);
                person.TimeInfected = 0;
                Population.Add(person);
            }

            time = 0;
            SimulationFinished = false;
            averageNeighbors = 1;
            numSamples = 1;

            quadTree = new QuadTree();
            foreach (var person in Population) {
                quadTree.Insert(person);
            }
        }

        public bool SimulationFinished { get; private set; }

        public int Time {
            get { return time; }
        }

        public List<Person> Persons {
            get { return quadTree.AllPersons; }
        }

        public Random Rng { get { return rng; } }

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

        private IEnumerable<Person> findPersonsWithoutQuadtree(Person p) {
            return quadTree.AllPersons.Where(person =>
                person.Status == Status.Healthy &&
                person.DistanceTo(p) <= Constants.InfluenceRadius);
        }

        double approxRollingAverage(double avg, double new_sample) {
            avg -= avg / numSamples;
            avg += new_sample / numSamples;

            return avg;
        }

        public void Tick() {
            var stopwatch = Stopwatch.StartNew();

            quadTree.Refresh();

            //Parallel.ForEach(Population, person => {
            foreach (var person in quadTree.AllPersons) {
                person.Tick(time);

                if (person.Status != Status.Healthy) {
                    var test = findPersonsWithoutQuadtree(person).ToList();
                    var influenced = findPersonsInInfluenceRadius(person).ToList();
                    foreach (var p in test) {
                        Debug.Assert(influenced.Contains(p));
                    }

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
                    var c = influenced.Count;
                    numSamples += c;
                    averageNeighbors = approxRollingAverage(averageNeighbors, c);

                    foreach (var p in influenced) {
                        var t = rng.NextDouble();

                        if (t <= rate) {
                            p.Status = Status.Infectious;
                            p.TimeInfected = time;
                        }
                    }

                    if (person.Status == Status.Dead && time >= person.TimeDied + Constants.RemoveDeadAfter) {
                        //Console.WriteLine("Removing person {0}", person.Id);
                        person.ToBeRemoved = true;
                    }
                }
            }

            var infectedCount = quadTree.AllPersons.Count(p => p.Status != Status.Healthy);
            if (infectedCount == 0) {
                SimulationFinished = true;
                return;
            }

            if (quadTree.AllPersons.Count == 0) {
                SimulationFinished = true;
                return;
            }
            time++;
            stopwatch.Stop();

            if (time % 50 == 0) {
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                Console.WriteLine("Average neighbors: {0}", averageNeighbors);
            }
        }
    }
}