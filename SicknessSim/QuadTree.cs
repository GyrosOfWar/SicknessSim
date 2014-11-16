using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SicknessSim {
    internal class QuadTree {
        private const int OBJECTS_PER_NODE = 32;
        private const int MAX_DEPTH = 5;
        private readonly QuadTree[] children;

        private readonly int level;
        private readonly List<Person> objects;
        private Rect bounds;

        public QuadTree(int level, Rect bounds) {
            this.level = level;
            this.bounds = bounds;
            objects = new List<Person>();
            children = new QuadTree[4];
        }

        public void Clear() {
            objects.Clear();

            foreach (var child in children.Where(child => child != null)) {
                child.Clear();
            }
        }

        private void Split() {
            var subWidth = (int) bounds.Width / 2;
            var subHeight = (int) bounds.Height / 2;
            var x = (int) bounds.X;
            var y = (int) bounds.Y;
            children[0] = new QuadTree(level + 1, new Rect(x + subWidth, y, subWidth, subHeight));
            children[1] = new QuadTree(level + 1, new Rect(x, y, subWidth, subHeight));
            children[2] = new QuadTree(level + 1, new Rect(x, y + subHeight, subWidth, subHeight));
            children[3] = new QuadTree(level + 1, new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
        }

        private int getIndex(Vector point) {
            var index = -1;
            var verticalMidpoint = bounds.X + (bounds.Width / 2);
            var horizontalMidpoint = bounds.Y + (bounds.Height / 2);

            // Object can completely fit within the top quadrants
            var topQuadrant = (point.Y < horizontalMidpoint);
            // Object can completely fit within the bottom quadrants
            var bottomQuadrant = (point.Y > horizontalMidpoint);

            // Object can completely fit within the left quadrants
            if (point.X < verticalMidpoint) {
                if (topQuadrant) {
                    index = 1;
                }
                else if (bottomQuadrant) {
                    index = 2;
                }
            }
                // Object can completely fit within the right quadrants
            else if (point.X > verticalMidpoint) {
                if (topQuadrant) {
                    index = 0;
                }
                else if (bottomQuadrant) {
                    index = 3;
                }
            }

            return index;
        }

        private int getIndex(Rect rectangle) {
            int index = -1;
            double verticalMidpoint = bounds.X + (bounds.Width / 2);
            double horizontalMidpoint = bounds.Y + (bounds.Height / 2);

            // Object can completely fit within the top quadrants
            bool topQuadrant = (rectangle.Y < horizontalMidpoint && rectangle.Y + rectangle.Height < horizontalMidpoint);
            // Object can completely fit within the bottom quadrants
            bool bottomQuadrant = (rectangle.Y > horizontalMidpoint);

            // Object can completely fit within the left quadrants
            if (rectangle.X < verticalMidpoint && rectangle.X + rectangle.Width < verticalMidpoint) {
                if (topQuadrant) {
                    index = 1;
                } else if (bottomQuadrant) {
                    index = 2;
                }
            }
                // Object can completely fit within the right quadrants
             else if (rectangle.X > verticalMidpoint) {
                if (topQuadrant) {
                    index = 0;
                } else if (bottomQuadrant) {
                    index = 3;
                }
            }

            return index;
        }

        public List<Person> Query(Rect rectangle) {
            int index = getIndex(rectangle);
            var returnObjects = new List<Person>();

            if (index != -1 && children[0] != null) {
                children[index].Query(rectangle);
            }

            returnObjects.AddRange(objects);

            return returnObjects;
        } 

        public void Insert(Person person) {
            if (children[0] != null) {
                var index = getIndex(person.Position);

                if (index != -1) {
                    children[index].Insert(person);
                    return;
                }
            }
            objects.Add(person);

            if (objects.Count > OBJECTS_PER_NODE && level < MAX_DEPTH) {
                if (children[0] == null) {
                    Split();
                }

                var i = 0;
                while (i < objects.Count) {
                    var index = getIndex(objects[i].Position);
                    if (index != -1) {
                        var obj = objects[i];
                        objects.RemoveAt(i);
                        children[index].Insert(obj);
                    }
                    else {
                        i++;
                    }
                }
            }
        }
    }
}