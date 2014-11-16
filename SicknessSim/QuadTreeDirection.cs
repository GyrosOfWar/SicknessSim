using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace SicknessSim {
    public interface IQuadObject {
        Rect Bounds { get; }
        event EventHandler BoundsChanged;
    }

    public class QuadTree<T> where T : class, IQuadObject {
        private readonly int maxObjectsPerLeaf;
        private readonly Size minLeafSize;
        private readonly Dictionary<T, int> objectSortOrder = new Dictionary<T, int>();
        private readonly Dictionary<T, QuadNode> objectToNodeLookup = new Dictionary<T, QuadNode>();
        private readonly bool sort;

        private readonly object syncLock = new object();
        private int objectSortId;
        private QuadNode root;

        public QuadTree(Size minLeafSize, int maxObjectsPerLeaf) {
            this.minLeafSize = minLeafSize;
            this.maxObjectsPerLeaf = maxObjectsPerLeaf;
        }

        /// <summary>
        /// </summary>
        /// <param name="minLeafSize">The smallest size a leaf will split into</param>
        /// <param name="maxObjectsPerLeaf">Maximum number of objects per leaf before it forces a split into sub quadrants</param>
        /// <param name="sort">Whether or not queries will return objects in the order in which they were added</param>
        public QuadTree(Size minLeafSize, int maxObjectsPerLeaf, bool sort)
            : this(minLeafSize, maxObjectsPerLeaf) {
            this.sort = sort;
        }

        public QuadNode Root {
            get { return root; }
        }

        public int GetSortOrder(T quadObject) {
            lock (objectSortOrder) {
                if (!objectSortOrder.ContainsKey(quadObject))
                    return -1;
                return objectSortOrder[quadObject];
            }
        }

        public void Insert(T quadObject) {
            lock (syncLock) {
                if (sort & !objectSortOrder.ContainsKey(quadObject)) {
                    objectSortOrder.Add(quadObject, objectSortId++);
                }

                var bounds = quadObject.Bounds;
                if (root == null) {
                    var rootSize = new Size(Math.Ceiling(bounds.Width / minLeafSize.Width),
                                            Math.Ceiling(bounds.Height / minLeafSize.Height));
                    var multiplier = Math.Max(rootSize.Width, rootSize.Height);
                    rootSize = new Size(minLeafSize.Width * multiplier, minLeafSize.Height * multiplier);
                    var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                    var rootOrigin = new Point(center.X - rootSize.Width / 2, center.Y - rootSize.Height / 2);
                    root = new QuadNode(new Rect(rootOrigin, rootSize));
                }

                while (!root.Bounds.Contains(bounds)) {
                    ExpandRoot(bounds);
                }

                InsertNodeObject(root, quadObject);
            }
        }

        public List<T> Query(Rect bounds) {
            lock (syncLock) {
                var results = new List<T>();
                if (root != null)
                    Query(bounds, root, results);
                if (sort)
                    results.Sort((a, b) => objectSortOrder[a].CompareTo(objectSortOrder[b]));
                return results;
            }
        }

        private void Query(Rect bounds, QuadNode node, List<T> results) {
            lock (syncLock) {
                if (node == null) return;

                if (bounds.IntersectsWith(node.Bounds)) {
                    foreach (var quadObject in node.Objects) {
                        if (bounds.IntersectsWith(quadObject.Bounds))
                            results.Add(quadObject);
                    }

                    foreach (var childNode in node.Nodes) {
                        Query(bounds, childNode, results);
                    }
                }
            }
        }

        private void ExpandRoot(Rect newChildBounds) {
            lock (syncLock) {
                var isNorth = root.Bounds.Y < newChildBounds.Y;
                var isWest = root.Bounds.X < newChildBounds.X;

                QTDirection rootQTDirection;
                if (isNorth) {
                    rootQTDirection = isWest ? QTDirection.NW : QTDirection.NE;
                }
                else {
                    rootQTDirection = isWest ? QTDirection.SW : QTDirection.SE;
                }

                var newX = (rootQTDirection == QTDirection.NW || rootQTDirection == QTDirection.SW)
                               ? root.Bounds.X
                               : root.Bounds.X - root.Bounds.Width;
                var newY = (rootQTDirection == QTDirection.NW || rootQTDirection == QTDirection.NE)
                               ? root.Bounds.Y
                               : root.Bounds.Y - root.Bounds.Height;
                var newRootBounds = new Rect(newX, newY, root.Bounds.Width * 2, root.Bounds.Height * 2);
                var newRoot = new QuadNode(newRootBounds);
                SetupChildNodes(newRoot);
                newRoot[rootQTDirection] = root;
                root = newRoot;
            }
        }

        private void InsertNodeObject(QuadNode node, T quadObject) {
            lock (syncLock) {
                if (!node.Bounds.Contains(quadObject.Bounds))
                    throw new Exception("This should not happen, child does not fit within node bounds");

                if (!node.HasChildNodes() && node.Objects.Count + 1 > maxObjectsPerLeaf) {
                    SetupChildNodes(node);

                    var childObjects = new List<T>(node.Objects);
                    var childrenToRelocate = new List<T>();

                    foreach (var childObject in childObjects) {
                        foreach (var childNode in node.Nodes) {
                            if (childNode == null)
                                continue;

                            if (childNode.Bounds.Contains(childObject.Bounds)) {
                                childrenToRelocate.Add(childObject);
                            }
                        }
                    }

                    foreach (var childObject in childrenToRelocate) {
                        RemoveQuadObjectFromNode(childObject);
                        InsertNodeObject(node, childObject);
                    }
                }

                foreach (var childNode in node.Nodes) {
                    if (childNode != null) {
                        if (childNode.Bounds.Contains(quadObject.Bounds)) {
                            InsertNodeObject(childNode, quadObject);
                            return;
                        }
                    }
                }

                AddQuadObjectToNode(node, quadObject);
            }
        }

        private void ClearQuadObjectsFromNode(QuadNode node) {
            lock (syncLock) {
                var quadObjects = new List<T>(node.Objects);
                foreach (var quadObject in quadObjects) {
                    RemoveQuadObjectFromNode(quadObject);
                }
            }
        }

        private void RemoveQuadObjectFromNode(T quadObject) {
            lock (syncLock) {
                var node = objectToNodeLookup[quadObject];
                node.quadObjects.Remove(quadObject);
                objectToNodeLookup.Remove(quadObject);
                quadObject.BoundsChanged -= quadObject_BoundsChanged;
            }
        }

        private void AddQuadObjectToNode(QuadNode node, T quadObject) {
            lock (syncLock) {
                node.quadObjects.Add(quadObject);
                objectToNodeLookup.Add(quadObject, node);
                quadObject.BoundsChanged += quadObject_BoundsChanged;
            }
        }

        private void quadObject_BoundsChanged(object sender, EventArgs e) {
            lock (syncLock) {
                var quadObject = sender as T;
                if (quadObject != null) {
                    var node = objectToNodeLookup[quadObject];
                    if (!node.Bounds.Contains(quadObject.Bounds) || node.HasChildNodes()) {
                        RemoveQuadObjectFromNode(quadObject);
                        Insert(quadObject);
                        if (node.Parent != null) {
                            CheckChildNodes(node.Parent);
                        }
                    }
                }
            }
        }

        private void SetupChildNodes(QuadNode node) {
            lock (syncLock) {
                if (minLeafSize.Width <= node.Bounds.Width / 2 && minLeafSize.Height <= node.Bounds.Height / 2) {
                    node[QTDirection.NW] = new QuadNode(node.Bounds.X, node.Bounds.Y, node.Bounds.Width / 2,
                                                        node.Bounds.Height / 2);
                    node[QTDirection.NE] = new QuadNode(node.Bounds.X + node.Bounds.Width / 2, node.Bounds.Y,
                                                        node.Bounds.Width / 2,
                                                        node.Bounds.Height / 2);
                    node[QTDirection.SW] = new QuadNode(node.Bounds.X, node.Bounds.Y + node.Bounds.Height / 2,
                                                        node.Bounds.Width / 2,
                                                        node.Bounds.Height / 2);
                    node[QTDirection.SE] = new QuadNode(node.Bounds.X + node.Bounds.Width / 2,
                                                        node.Bounds.Y + node.Bounds.Height / 2,
                                                        node.Bounds.Width / 2, node.Bounds.Height / 2);
                }
            }
        }

        public void Remove(T quadObject) {
            lock (syncLock) {
                if (sort && objectSortOrder.ContainsKey(quadObject)) {
                    objectSortOrder.Remove(quadObject);
                }

                if (!objectToNodeLookup.ContainsKey(quadObject))
                    throw new KeyNotFoundException("QuadObject not found in dictionary for removal");

                var containingNode = objectToNodeLookup[quadObject];
                RemoveQuadObjectFromNode(quadObject);

                if (containingNode.Parent != null)
                    CheckChildNodes(containingNode.Parent);
            }
        }


        private void CheckChildNodes(QuadNode node) {
            lock (syncLock) {
                if (GetQuadObjectCount(node) <= maxObjectsPerLeaf) {
                    // Move child objects into this node, and delete sub nodes
                    var subChildObjects = GetChildObjects(node);
                    foreach (var childObject in subChildObjects) {
                        if (!node.Objects.Contains(childObject)) {
                            RemoveQuadObjectFromNode(childObject);
                            AddQuadObjectToNode(node, childObject);
                        }
                    }
                    if (node[QTDirection.NW] != null) {
                        node[QTDirection.NW].Parent = null;
                        node[QTDirection.NW] = null;
                    }
                    if (node[QTDirection.NE] != null) {
                        node[QTDirection.NE].Parent = null;
                        node[QTDirection.NE] = null;
                    }
                    if (node[QTDirection.SW] != null) {
                        node[QTDirection.SW].Parent = null;
                        node[QTDirection.SW] = null;
                    }
                    if (node[QTDirection.SE] != null) {
                        node[QTDirection.SE].Parent = null;
                        node[QTDirection.SE] = null;
                    }

                    if (node.Parent != null)
                        CheckChildNodes(node.Parent);
                    else {
                        // Its the root node, see if we're down to one quadrant, with none in local storage - if so, ditch the other three
                        var numQuadrantsWithObjects = 0;
                        QuadNode nodeWithObjects = null;
                        foreach (var childNode in node.Nodes) {
                            if (childNode != null && GetQuadObjectCount(childNode) > 0) {
                                numQuadrantsWithObjects++;
                                nodeWithObjects = childNode;
                                if (numQuadrantsWithObjects > 1) break;
                            }
                        }
                        if (numQuadrantsWithObjects == 1) {
                            foreach (var childNode in node.Nodes) {
                                if (childNode != nodeWithObjects)
                                    childNode.Parent = null;
                            }
                            root = nodeWithObjects;
                        }
                    }
                }
            }
        }


        private List<T> GetChildObjects(QuadNode node) {
            lock (syncLock) {
                var results = new List<T>();
                results.AddRange(node.quadObjects);
                foreach (var childNode in node.Nodes) {
                    if (childNode != null)
                        results.AddRange(GetChildObjects(childNode));
                }
                return results;
            }
        }

        public int GetQuadObjectCount() {
            lock (syncLock) {
                if (root == null)
                    return 0;
                var count = GetQuadObjectCount(root);
                return count;
            }
        }

        private int GetQuadObjectCount(QuadNode node) {
            lock (syncLock) {
                var count = node.Objects.Count;
                foreach (var childNode in node.Nodes) {
                    if (childNode != null) {
                        count += GetQuadObjectCount(childNode);
                    }
                }
                return count;
            }
        }

        public int GetQuadNodeCount() {
            lock (syncLock) {
                if (root == null)
                    return 0;
                var count = GetQuadNodeCount(root, 1);
                return count;
            }
        }

        private int GetQuadNodeCount(QuadNode node, int count) {
            lock (syncLock) {
                if (node == null) return count;

                foreach (var childNode in node.Nodes) {
                    if (childNode != null)
                        count++;
                }
                return count;
            }
        }

        public List<QuadNode> GetAllNodes() {
            lock (syncLock) {
                var results = new List<QuadNode>();
                if (root != null) {
                    results.Add(root);
                    GetChildNodes(root, results);
                }
                return results;
            }
        }

        private void GetChildNodes(QuadNode node, ICollection<QuadNode> results) {
            lock (syncLock) {
                foreach (var childNode in node.Nodes) {
                    if (childNode != null) {
                        results.Add(childNode);
                        GetChildNodes(childNode, results);
                    }
                }
            }
        }

        public class QuadNode {
            private static int _id;
            public readonly int ID = _id++;

            private readonly QuadNode[] _nodes = new QuadNode[4];

            public ReadOnlyCollection<QuadNode> Nodes;

            public ReadOnlyCollection<T> Objects;
            internal List<T> quadObjects = new List<T>();

            public QuadNode(Rect bounds) {
                Bounds = bounds;
                Nodes = new ReadOnlyCollection<QuadNode>(_nodes);
                Objects = new ReadOnlyCollection<T>(quadObjects);
            }

            public QuadNode(double x, double y, double width, double height)
                : this(new Rect(x, y, width, height)) {
            }

            public QuadNode Parent { get; internal set; }

            public QuadNode this[QTDirection QTDirection] {
                get {
                    switch (QTDirection) {
                        case QTDirection.NW:
                            return _nodes[0];
                        case QTDirection.NE:
                            return _nodes[1];
                        case QTDirection.SW:
                            return _nodes[2];
                        case QTDirection.SE:
                            return _nodes[3];
                        default:
                            return null;
                    }
                }
                set {
                    switch (QTDirection) {
                        case QTDirection.NW:
                            _nodes[0] = value;
                            break;
                        case QTDirection.NE:
                            _nodes[1] = value;
                            break;
                        case QTDirection.SW:
                            _nodes[2] = value;
                            break;
                        case QTDirection.SE:
                            _nodes[3] = value;
                            break;
                    }
                    if (value != null)
                        value.Parent = this;
                }
            }

            public Rect Bounds { get; internal set; }

            public bool HasChildNodes() {
                return _nodes[0] != null;
            }
        }
    }

    public enum QTDirection {
        NW = 0,
        NE = 1,
        SW = 2,
        SE = 3
    }
}