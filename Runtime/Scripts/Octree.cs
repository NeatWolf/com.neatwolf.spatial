using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace NeatWolf.Spatial.Partitioning
{
    /// <summary>
    /// Represents a single node within an Octree containing a position and associated data.
    /// </summary>
    /// <typeparam name="T">Type of data contained within the node.</typeparam>
    [Serializable]
    public class OctreeNode<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctreeNode{T}"/> class with the specified position and data.
        /// </summary>
        /// <param name="position">The position of the node within the space.</param>
        /// <param name="data">The data contained within the node.</param>
        public OctreeNode(Vector3 position, T data)
        {
            Position = position;
            Data = data;
        }

        /// <summary>
        /// Gets the position of the node within the space.
        /// </summary>
        public Vector3 Position { get; private set; }
        
        /// <summary>
        /// Gets or sets the data contained within the node.
        /// </summary>
        public T Data { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the node is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Converts the node to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the node.</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Creates an instance of the <see cref="OctreeNode{T}"/> class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string representing the node.</param>
        /// <returns>A new instance of the <see cref="OctreeNode{T}"/> class.</returns>
        public static OctreeNode<T> FromJson(string json)
        {
            return JsonUtility.FromJson<OctreeNode<T>>(json);
        }
    }

    /// <summary>
    /// Represents a balanced Octree data structure designed to efficiently manage spatial data.
    /// This implementation is a variant of the standard Octree, optimized for query performance
    /// and providing dynamic management through configurable parameters like `maxDepth`, `minPoints`,
    /// and `maxPoints`. The Octree ensures even distribution of points by performing subdivisions
    /// and merges, preventing unnecessary growth or complexity.
    /// </summary>
    /// <typeparam name="T">Type of data contained within the nodes of the Octree.</typeparam>
    [Serializable]
    public class Octree<T>
    {
        /// <summary>
        /// Gets the origin of the Octree.
        /// </summary>
        public Vector3 Origin => origin;
        
        /// <summary>
        /// Gets the half dimensions of the Octree.
        /// </summary>
        public Vector3 HalfDimension => halfDimension;
        
        /// <summary>
        /// Gets the child Octrees.
        /// </summary>
        public Octree<T>[] Children { get; }
        
        /// <summary>
        /// Gets the nodes within this Octree.
        /// </summary>
        public List<OctreeNode<T>> Nodes { get; }

        [SerializeField] private int depth;
        [SerializeField] private Vector3 halfDimension;
        [SerializeField] private int maxDepth;
        [SerializeField] private int maxPoints;
        [SerializeField] private int minPoints;
        [SerializeField] private Vector3 origin;

        public Octree(Vector3 origin, Vector3 halfDimension, int maxDepth, int minPoints, int maxPoints)
        {
            this.origin = origin;
            this.halfDimension = halfDimension;
            this.maxDepth = maxDepth;
            depth = 0;
            this.minPoints = minPoints;
            this.maxPoints = maxPoints;
            Nodes = new List<OctreeNode<T>>();
            Children = new Octree<T>[8];
        }

        public bool IsLeafNode()
        {
            return depth >= maxDepth || Children[0] == null;
        }

        public void Insert(Vector3 position, T data)
        {
            if (depth >= maxDepth || Nodes.Count < maxPoints)
            {
                Nodes.Add(new OctreeNode<T>(position, data));
                return;
            }

            Subdivide();
            GetOctantContainingPoint(position).Insert(position, data);
        }

        public void Remove(Vector3 position)
        {
            var index = Nodes.FindIndex(node => node.Position == position);
            if (index >= 0)
            {
                Nodes.RemoveAt(index);
                if (Nodes.Count < minPoints && depth > 0) Merge();
            }
            else
            {
                GetOctantContainingPoint(position)?.Remove(position);
            }
        }

        public OctreeNode<T> Query(Vector3 position)
        {
            if (ContainsPoint(position))
            {
                foreach (var node in Nodes)
                    if (node.Position == position)
                        return node;

                return GetOctantContainingPoint(position)?.Query(position);
            }

            return null;
        }

        public List<OctreeNode<T>> SphereCast(Vector3 center, float radius)
        {
            var result = new List<OctreeNode<T>>();
            if (IntersectsSphere(center, radius))
            {
                foreach (var node in Nodes)
                    if (Vector3.Distance(center, node.Position) <= radius)
                        result.Add(node);

                foreach (var child in Children)
                    if (child != null)
                        result.AddRange(child.SphereCast(center, radius));
            }

            return result;
        }

        public OctreeNode<T> FindNearestNode(Vector3 position)
        {
            if (ContainsPoint(position))
            {
                var nearestNode = Nodes[0];
                var nearestDistance = Vector3.Distance(nearestNode.Position, position);
                foreach (var node in Nodes)
                {
                    var distance = Vector3.Distance(node.Position, position);
                    if (distance < nearestDistance)
                    {
                        nearestNode = node;
                        nearestDistance = distance;
                    }
                }

                return nearestNode;
            }

            return null;
        }

        public OctreeNode<T> FindNearestEnabledNode(Vector3 position, bool enabledStatus = true)
        {
            if (ContainsPoint(position))
            {
                OctreeNode<T> nearestNode = null;
                var nearestDistance = float.MaxValue;
                foreach (var node in Nodes)
                    if (node.Enabled == enabledStatus)
                    {
                        var distance = Vector3.Distance(node.Position, position);
                        if (distance < nearestDistance)
                        {
                            nearestNode = node;
                            nearestDistance = distance;
                        }
                    }

                return nearestNode;
            }

            return null;
        }

        public bool NodeExistsAt(Vector3 position)
        {
            return Query(position) != null;
        }

        private void Subdivide()
        {
            for (var i = 0; i < 8; i++)
            {
                var newOrigin = origin;
                newOrigin.x += halfDimension.x * (i & 1);
                newOrigin.y += halfDimension.y * ((i >> 1) & 1);
                newOrigin.z += halfDimension.z * ((i >> 2) & 1);
                Children[i] = new Octree<T>(newOrigin, halfDimension * 0.5f, maxDepth, minPoints, maxPoints);
            }

            Nodes.ForEach(node => GetOctantContainingPoint(node.Position).Insert(node.Position, node.Data));
            Nodes.Clear();
        }

        private void Merge()
        {
            foreach (var child in Children) Nodes.AddRange(child.Nodes);
        }

        private bool ContainsPoint(Vector3 point)
        {
            return point.x >= origin.x - halfDimension.x && point.x <= origin.x + halfDimension.x &&
                   point.y >= origin.y - halfDimension.y && point.y <= origin.y + halfDimension.y &&
                   point.z >= origin.z - halfDimension.z && point.z <= origin.z + halfDimension.z;
        }

        private Octree<T> GetOctantContainingPoint(Vector3 point)
        {
            var index = 0;
            if (point.x > origin.x) index |= 1;
            if (point.y > origin.y) index |= 2;
            if (point.z > origin.z) index |= 4;
            return Children[index];
        }

        private bool IntersectsSphere(Vector3 center, float radius)
        {
            float minSquaredDistance = 0; // its full meaning would be minSquaredDistanceToBoundingBox
            if (center.x < origin.x - halfDimension.x)
                minSquaredDistance += (center.x - origin.x + halfDimension.x) * (center.x - origin.x + halfDimension.x);
            else if (center.x > origin.x + halfDimension.x)
                minSquaredDistance += (center.x - origin.x - halfDimension.x) * (center.x - origin.x - halfDimension.x);
            if (center.y < origin.y - halfDimension.y)
                minSquaredDistance += (center.y - origin.y + halfDimension.y) * (center.y - origin.y + halfDimension.y);
            else if (center.y > origin.y + halfDimension.y)
                minSquaredDistance += (center.y - origin.y - halfDimension.y) * (center.y - origin.y - halfDimension.y);
            if (center.z < origin.z - halfDimension.z)
                minSquaredDistance += (center.z - origin.z + halfDimension.z) * (center.z - origin.z + halfDimension.z);
            else if (center.z > origin.z + halfDimension.z)
                minSquaredDistance += (center.z - origin.z - halfDimension.z) * (center.z - origin.z - halfDimension.z);
            return minSquaredDistance <= radius * radius;
        }
        
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static Octree<T> FromJson(string json)
        {
            return JsonUtility.FromJson<Octree<T>>(json);
        }
    }
}