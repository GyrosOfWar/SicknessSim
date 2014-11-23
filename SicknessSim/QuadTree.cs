using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace SicknessSim {
    public class QuadTree {
        private readonly QuadTreeNode root;
        public static int MaxDepth = 12;
        public static int MaxObjectsPerNode = 8;

        public QuadTree(int xSize, int ySize, int maxDepth = 12, int objectsPerNode = 8) {
            root = new QuadTreeNode(0, new Rect(0, 0, xSize, ySize));
            MaxDepth = maxDepth;
            MaxObjectsPerNode = objectsPerNode;
        }

        public List<Person> AllPersons {
            get { return root.Enumerate(); }
        }

        public void Refresh() {
            var persons = new List<Person>();
            root.Visit((p, _) => { if (!p.ToBeRemoved) persons.Add(p); });

            root.Clear();
            Debug.Assert(Count() == 0);
            foreach (var p in persons) {
                root.Insert(p);
            }
        }

        public void Insert(Person person) {
            root.Insert(person);
        }

        public List<Person> Query(Rect rectangle) {
            return root.Query(rectangle);
        }

        public bool Contains(Person p) {
            return root.Contains(p);
        }

        public void Visit(Action<Person, int> callback) {
            root.Visit(callback);
        }

        public int Count() {
            var i = 0;
            Visit((p, _) => i++);
            return i;
        }
    }

    internal class QuadTreeNode {
        private static int idCounter;
        private readonly QuadTreeNode[] children;

        private readonly int level;
        private readonly List<Person> objects;
        private Rect bounds;

        public QuadTreeNode(int level, Rect bounds) {
            Id = idCounter++;
            this.level = level;
            this.bounds = bounds;
            objects = new List<Person>();
            children = new QuadTreeNode[4];
        }

        public int Id { get; private set; }

        public void Clear() {
            VisitNode(n => n.objects.Clear());
            Debug.Assert(Count() == 0);
        }

        public void VisitNode(Action<QuadTreeNode> callback) {
            callback(this);

            foreach (var child in children) {
                if (child != null) {
                    child.VisitNode(callback);
                }
            }
        }

        private int Count() {
            int i = 0;

            Visit((p, _) => i++);

            return i;
        }

        public void Visit(Action<Person, int> callback) {
            foreach (var obj in objects) {
                callback(obj, Id);
            }

            foreach (var child in children) {
                if (child != null) {
                    child.Visit(callback);
                }
            }
        }

        public bool Contains(Person p) {
            var contained = false;

            Visit((q, _) => {
                if (p.Id == q.Id) {
                    contained = true;
                }
            });

            return contained;
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
            Visit((p, id) => list.Add(p));
            return list;
        }

        public bool Insert(Person person) {
            if (!bounds.Contains(person.Position.X, person.Position.Y)) {
                return false;
            }

            if (objects.Count < QuadTree.MaxObjectsPerNode) {
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