using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SicknessSim;
using Vector = SicknessSim.Vector;

namespace SicknessSimTest {
    [TestClass]
    public class QuadTreeTest {
        const int xSize = 600;
        const int ySize = 600;
        readonly Random rng = new Random();

        [TestMethod]
        public void TestQuerySimple() {
            var tree = new QuadTree(xSize, ySize);

            for (var i = 0; i < 100; i++) {
                tree.Insert(new Person(new Vector(rng.Next(xSize), rng.Next(ySize)), Status.Healthy, rng));
            }

            var query = tree.Query(new Rect(0, 0, xSize, ySize));
            var all = tree.AllPersons;

            Assert.IsTrue(all.All(query.Contains));
            Assert.IsTrue(query.All(all.Contains));

            tree.Refresh();

            query = tree.Query(new Rect(0, 0, xSize, ySize));
            all = tree.AllPersons;
            Assert.IsTrue(all.All(query.Contains));
            Assert.IsTrue(query.All(all.Contains));
        }
        [TestMethod]
        public void TestQuery() {
            var tree = new QuadTree(xSize, ySize);

            for (var i = 0; i < 1000; i++) {
                tree.Insert(new Person(new Vector(rng.Next(50, 270), rng.Next(50, 270)), Status.Healthy, rng));
            }

            var actual1 = new List<Person>() {
                new Person(new Vector(10, 10), Status.Healthy, rng),
                new Person(new Vector(15, 8), Status.Healthy, rng),
                new Person(new Vector(19, 5), Status.Healthy, rng),
                new Person(new Vector(2, 19), Status.Healthy, rng),
                new Person(new Vector(2, 2), Status.Healthy, rng)
            };

            var actual2 = new List<Person>() {
                new Person(new Vector(300, 300), Status.Healthy, rng),
                new Person(new Vector(280, 290), Status.Healthy, rng),
                new Person(new Vector(305, 300), Status.Healthy, rng),
                new Person(new Vector(304, 302), Status.Healthy, rng),
                new Person(new Vector(301, 288), Status.Healthy, rng)
            };

            foreach (var person in actual1.Concat(actual2)) {
                tree.Insert(person);
            }

            var query1 = tree.Query(new Rect(0, 0, 20, 20));
            var query2 = tree.Query(new Rect(280, 280, 30, 30));

            Assert.IsTrue(query1.All(actual1.Contains));
            Assert.IsTrue(query2.All(actual2.Contains));

            Assert.IsTrue(actual1.All(query1.Contains));
            Assert.IsTrue(actual2.All(query2.Contains));

            tree.Refresh();

            query1 = tree.Query(new Rect(0, 0, 20, 20));
            query2 = tree.Query(new Rect(280, 280, 30, 30));

            Assert.IsTrue(query1.All(actual1.Contains));
            Assert.IsTrue(query2.All(actual2.Contains));

            Assert.IsTrue(actual1.All(query1.Contains));
            Assert.IsTrue(actual2.All(query2.Contains));
        }
        [TestMethod]
        public void TestVisit() {
            var tree = new QuadTree(xSize, ySize);
            var actual1 = new List<Person>() {
                new Person(new Vector(10, 10), Status.Healthy, rng),
                new Person(new Vector(15, 8), Status.Healthy, rng),
                new Person(new Vector(19, 5), Status.Healthy, rng),
                new Person(new Vector(2, 19), Status.Healthy, rng),
                new Person(new Vector(2, 2), Status.Healthy, rng)
            };

            var actual2 = new List<Person>() {
                new Person(new Vector(300, 300), Status.Healthy, rng),
                new Person(new Vector(280, 290), Status.Healthy, rng),
                new Person(new Vector(305, 300), Status.Healthy, rng),
                new Person(new Vector(304, 302), Status.Healthy, rng),
                new Person(new Vector(301, 288), Status.Healthy, rng)
            };

            foreach (var person in actual1.Concat(actual2)) {
                tree.Insert(person);
            }

            var query = new List<Person>();
            tree.Visit((p, _) => query.Add(p));
            var all = actual1.Concat(actual2).ToList();
            Assert.IsTrue(all.All(query.Contains));

            tree.Refresh();

            query = new List<Person>();
            tree.Visit((p, _) => query.Add(p));

            Assert.IsTrue(all.All(query.Contains));
        }

        [TestMethod]
        public void TestInfluenceRadius() {
            var tree = new QuadTree(xSize, ySize);

            for (var i = 0; i < 1000; i++) {
                tree.Insert(new Person(new Vector(rng.Next(50, 270), rng.Next(50, 270)), Status.Healthy, rng));
            }
            var set1 = new List<Person>() {
                new Person(new Vector(10, 10), Status.Healthy, rng),
                new Person(new Vector(15, 8), Status.Healthy, rng),
                new Person(new Vector(19, 5), Status.Healthy, rng),
                new Person(new Vector(2, 19), Status.Healthy, rng),
                new Person(new Vector(2, 2), Status.Healthy, rng)
            };

            var set2 = new List<Person>() {
                new Person(new Vector(300, 300), Status.Healthy, rng),
                new Person(new Vector(280, 290), Status.Healthy, rng),
                new Person(new Vector(305, 300), Status.Healthy, rng),
                new Person(new Vector(304, 302), Status.Healthy, rng),
                new Person(new Vector(301, 288), Status.Healthy, rng)
            };

            foreach (var person in set1.Concat(set2)) {
                tree.Insert(person);
            }

            var allPersons = set1.Concat(set2);
            var root1 = set1[4];
            const int radius = 5;

            var actual = allPersons.Where(p => p.Status == Status.Healthy && p.DistanceTo(root1) <= radius);
  
            var origin = root1.Position - radius;
            const int length = radius * 2;
            var points = tree.Query(new Rect(origin.X, origin.Y, length, length));

            var query =  points.Where(person =>
                //  Only healthy persons can get infected
                person.Status == Status.Healthy &&
                person.DistanceTo(root1) <= radius);

            Assert.IsTrue(actual.All(query.Contains));

        }
    }
}
