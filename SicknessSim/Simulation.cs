using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SicknessSim {
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
                person.TimeInfected = 0;
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
                            default:
                                Debug.Fail("A healthy person is sick?");
                                break;
                        }

                        if (t <= rate) {
                            p.Status = Status.Infectious;
                            p.TimeInfected = time;
                            Console.WriteLine("Person {0} infected {1}", person.Id, p.Id);
                        }
                    }

                    if (person.Status == Status.Dead && time >= person.TimeDied + Constants.RemoveDeadAfter) {
                        Console.WriteLine("Removing person {0}", person.Id);
                        person.ToBeRemoved = true;
                    }
                }
            }


            Population.RemoveAll(p => p.ToBeRemoved);
            //if (time % 50 == 0) {
            //    var numInfected = Population.Count(p => p.Status == Status.Infectious || p.Status == Status.Sick || p.Status == Status.Dead);
            //    Console.WriteLine("#Infected at {0}: {1}", time, numInfected);
            //}
            time++;
        }
    }
}