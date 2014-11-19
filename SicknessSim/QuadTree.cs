using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SicknessSim {
    class QuadTree {
        private readonly QuadTreeNode root;
        private readonly List<Person> allPersons;

        public QuadTree() {
            root = new QuadTreeNode(0, new Rect(0, 0, Constants.RoomSize, Constants.RoomSize));
            allPersons = new List<Person>();
        }

        public void Refresh() {
            root.Clear();
            allPersons.RemoveAll(p => p.ToBeRemoved);
            foreach (var item in allPersons) {
                root.Insert(item);
            }
        }

        public List<Person> AllPersons {
            get { return allPersons; }
        }

        public void Insert(Person person) {
            root.Insert(person);
            allPersons.Add(person);
        }

        public List<Person> Query(Rect rectangle) {
            return root.Query(rectangle);
        }

        public bool Exists(Person p) {
            return root.Exists(p);
        }

        public List<Person> Enumerate() {
            return root.Enumerate();
        }
    }

    internal class QuadTreeNode {
        private const int OBJECTS_PER_NODE =4;
        private const int MAX_DEPTH = 200;
        private readonly QuadTreeNode[] children;

        private readonly int level;
        private readonly List<Person> objects;
        private Rect bounds;

        public QuadTreeNode(int level, Rect bounds) {
            this.level = level;
            this.bounds = bounds;
            objects = new List<Person>();
            children = new QuadTreeNode[4];
          //  System.Console.WriteLine(level);
        }

        public void Clear() {
            objects.Clear();

            foreach (var child in children.Where(child => child != null)) {
                child.Clear();
            }
        }

        public bool Exists(Person p) {
            if (bounds.Contains(p.Position.X, p.Position.Y) && objects.Exists(p.Equals)) {
                return true;
            }

            foreach (var child in children) {
                if (child != null && child.Exists(p)) {
                    return true;
                }
            }
            return false;
        }


        private void Split() {
            var subWidth = (int) bounds.Width / 2;
            var subHeight = (int) bounds.Height / 2;
            var x = (int) bounds.X;
            var y = (int) bounds.Y;
            children[0] = new QuadTreeNode(level + 1, new Rect(x + subWidth, y, subWidth, subHeight));
            children[1] = new QuadTreeNode(level + 1, new Rect(x, y, subWidth, subHeight));
            children[2] = new QuadTreeNode(level + 1, new Rect(x, y + subHeight, subWidth, subHeight));
            children[3] = new QuadTreeNode(level + 1, new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
        }

        public List<Person> Query(Rect rectangle) {
            var returnList = new List<Person>();

            if (!bounds.IntersectsWith(rectangle)) {
                return returnList;
            }

            returnList.AddRange(objects.Where(person => rectangle.Contains(person.Position.X, person.Position.Y)));

            if (children[0] == null) {
                return returnList;
            }

            foreach (var node in children) {
                returnList.AddRange(node.Query(rectangle));
            }

            return returnList;
        }

        public List<Person> Enumerate() {
            var list = new List<Person>();
            list.AddRange(objects);

            foreach (var child in children) {
                if (child != null) {
                    list.AddRange(child.Enumerate());
                }
            }
            

            return list;
        } 

        public bool Insert(Person person) {
            if (!bounds.Contains(person.Position.X, person.Position.Y)) {
                return false;
            }

            if (objects.Count < OBJECTS_PER_NODE) {
                objects.Add(person);
                return true;
            }

            if (children[0] == null) {
                Split();
            }

            return children.Any(child => child.Insert(person));
        }
    }
}