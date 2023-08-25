using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace NeatWolf.Spatial.Partitioning
{
    /// <summary>
    ///     Represents a single node within an Octree containing a position and associated data.
    /// </summary>
    /// <typeparam name="T">Type of data contained within the node.</typeparam>
    [Serializable]
    public class OctreeNode<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OctreeNode{T}" /> class with the specified position and data.
        /// </summary>
        /// <param name="position">The position of the node within the space.</param>
        /// <param name="data">The data contained within the node.</param>
        public OctreeNode(Vector3 position, T data)
        {
            Position = position;
            Data = data;
        }

        /// <summary>
        ///     Gets the position of the node within the space.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        ///     Gets or sets the data contained within the node.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the node is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Converts the node to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the node.</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        ///     Creates an instance of the <see cref="OctreeNode{T}" /> class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string representing the node.</param>
        /// <returns>A new instance of the <see cref="OctreeNode{T}" /> class.</returns>
        public static OctreeNode<T> FromJson(string json)
        {
            return JsonUtility.FromJson<OctreeNode<T>>(json);
        }
    }

    /// <summary>
    ///     Represents a balanced Octree data structure designed to efficiently manage spatial data.
    ///     This implementation is a variant of the standard Octree, optimized for query performance
    ///     and providing dynamic management through configurable parameters like `maxDepth`, `minPoints`,
    ///     and `maxPoints`. The Octree ensures even distribution of points by performing subdivisions
    ///     and merges, preventing unnecessary growth or complexity.
    /// </summary>
    /// <typeparam name="T">Type of data contained within the nodes of the Octree.</typeparam>
    [Serializable]
    public class Octree<T>
    {
        [SerializeField] private int depth;
        [SerializeField] private Vector3 halfDimension;
        [SerializeField] private int maxDepth;
        [SerializeField] private int maxPoints;
        [SerializeField] private int minPoints;
        [SerializeField] private Vector3 origin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Octree{T}" /> class with the specified parameters.
        /// </summary>
        /// <param name="origin">The origin of the Octree.</param>
        /// <param name="halfDimension">The half dimensions of the Octree.</param>
        /// <param name="maxDepth">The maximum depth of the Octree.</param>
        /// <param name="minPoints">The minimum number of points in a node before it is subdivided.</param>
        /// <param name="maxPoints">The maximum number of points in a node before it is subdivided.</param>
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

        /// <summary>
        ///     Gets the origin of the Octree.
        /// </summary>
        public Vector3 Origin => origin;

        /// <summary>
        ///     Gets the half dimensions of the Octree.
        /// </summary>
        public Vector3 HalfDimension => halfDimension;

        /// <summary>
        ///     Gets the child Octrees.
        /// </summary>
        public Octree<T>[] Children { get; }

        /// <summary>
        ///     Gets the nodes within this Octree.
        /// </summary>
        public List<OctreeNode<T>> Nodes { get; private set; }

        /// <summary>
        ///     Determines whether this Octree is a leaf node.
        /// </summary>
        /// <returns><c>true</c> if this Octree is a leaf node; otherwise, <c>false</c>.</returns>
        public bool IsLeafNode()
        {
            return depth >= maxDepth || Children[0] == null;
        }

        /// <summary>
        ///     Inserts a node with the specified position and data into the Octree.
        /// </summary>
        /// <param name="position">The position of the node.</param>
        /// <param name="data">The data of the node.</param>
        /// <returns>True if the node was inserted successfully, false otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the position is out of bounds.</exception>
        public bool Insert(Vector3 position, T data)
        {
            if (!ContainsPoint(position))
                throw new ArgumentOutOfRangeException(nameof(position), "Octree insert: point is out of bounds");

            if (IsLeafNode())
            {
                Nodes.Add(new OctreeNode<T>(position, data));
                if (Nodes.Count > maxPoints && depth < maxDepth)
                {
                    Subdivide();
                    Nodes.Clear();
                }

                return true;
            }

            var childOctree = GetOctantContainingPoint(position);
            return childOctree != null && childOctree.Insert(position, data);
        }

        /// <summary>
        ///     Removes the node at the specified position from the Octree.
        /// </summary>
        /// <param name="position">The position of the node.</param>
        /// <returns>True if the node was removed successfully, false otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the position is out of bounds.</exception>
        public bool Remove(Vector3 position)
        {
            if (!ContainsPoint(position))
                throw new ArgumentOutOfRangeException(nameof(position), "Octree remove: point is out of bounds");

            if (IsLeafNode())
            {
                var index = Nodes.FindIndex(node => node.Position == position);
                if (index >= 0)
                {
                    Nodes.RemoveAt(index);
                    return true;
                }

                return false;
            }

            var childOctree = GetOctantContainingPoint(position);
            if (childOctree != null)
                if (childOctree.NodeExistsAt(position))
                {
                    childOctree.Remove(position);

                    if (ShouldMerge()) Merge();
                    return true;
                }

            return false;
        }

        /// <summary>
        ///     Determines whether the Octree should merge its child Octrees.
        /// </summary>
        /// <returns><c>true</c> if the Octree should merge its child Octrees; otherwise, <c>false</c>.</returns>
        private bool ShouldMerge()
        {
            foreach (var child in Children)
                if (child != null && child.Nodes.Count > 0)
                    return false;

            return true;
        }

        /// <summary>
        ///     Queries the Octree for a node at the specified position.
        /// </summary>
        /// <param name="position">The position to query.</param>
        /// <returns>The node at the specified position, or <c>null</c> if no node exists at the position.</returns>
        public OctreeNode<T> Query(Vector3 position)
        {
            if (!ContainsPoint(position))
            {
                Debug.LogWarning("Octree query: point is out of bounds");
                return null;
            }

            foreach (var node in Nodes)
                if (node.Position == position)
                    return node;

            var octant = GetOctantContainingPoint(position);
            return octant?.Query(position);
        }

        /// <summary>
        ///     Performs a sphere cast on the Octree and returns all nodes within the specified radius of the center.
        ///     This method may return duplicate nodes if the same node is contained in multiple child Octrees.
        /// </summary>
        /// <param name="center">The center of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <returns>A set of nodes within the specified radius of the center.</returns>
        public HashSet<OctreeNode<T>> SphereCastWithDuplicates(Vector3 center, float radius)
        {
            var result = new HashSet<OctreeNode<T>>();
            foreach (var node in Nodes)
                if (Vector3.Distance(center, node.Position) <= radius)
                    result.Add(node);

            if (!IsLeafNode())
                foreach (var child in Children)
                    if (child != null)
                        result.UnionWith(child.SphereCastWithDuplicates(center, radius));

            return result;
        }

        /// <summary>
        ///     Performs a sphere cast on the Octree and returns all nodes within the specified radius of the center.
        /// </summary>
        /// <param name="center">The center of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <returns>A list of nodes within the specified radius of the center.</returns>
        public List<OctreeNode<T>> SphereCast(Vector3 center, float radius)
        {
            var result = new List<OctreeNode<T>>();
            foreach (var node in Nodes)
                if (Vector3.Distance(center, node.Position) <= radius)
                    result.Add(node);

            if (!IsLeafNode())
                foreach (var child in Children)
                    if (child != null)
                        result.AddRange(child.SphereCast(center, radius));

            return result;
        }

        /// <summary>
        ///     Finds the nearest node to the specified position.
        /// </summary>
        /// <param name="position">The position to find the nearest node to.</param>
        /// <returns>The nearest node to the specified position, or <c>null</c> if no nodes exist in the Octree.</returns>
        public OctreeNode<T> FindNearestNode(Vector3 position)
        {
            OctreeNode<T> nearestNode = null;
            var nearestDistance = float.MaxValue;
            foreach (var node in Nodes)
            {
                var distance = Vector3.Distance(node.Position, position);
                if (distance < nearestDistance)
                {
                    nearestNode = node;
                    nearestDistance = distance;
                }
            }

            if (!IsLeafNode())
                foreach (var child in Children)
                {
                    var childNearestNode = child.FindNearestNode(position);
                    if (childNearestNode != null)
                    {
                        var childNearestDistance = Vector3.Distance(childNearestNode.Position, position);
                        if (childNearestDistance < nearestDistance)
                        {
                            nearestNode = childNearestNode;
                            nearestDistance = childNearestDistance; // Update nearestDistance
                        }
                    }
                }

            return nearestNode;
        }

        /// <summary>
        ///     Finds the nearest enabled node to the specified position.
        /// </summary>
        /// <param name="position">The position to find the nearest enabled node to.</param>
        /// <param name="enabledStatus">The enabled status to match. Defaults to <c>true</c>.</param>
        /// <returns>The nearest enabled node to the specified position, or <c>null</c> if no enabled nodes exist in the Octree.</returns>
        public OctreeNode<T> FindNearestEnabledNode(Vector3 position, bool enabledStatus = true)
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

            if (!IsLeafNode())
                foreach (var child in Children)
                {
                    var childNearestNode = child.FindNearestEnabledNode(position, enabledStatus);
                    if (childNearestNode != null)
                    {
                        var childNearestDistance = Vector3.Distance(childNearestNode.Position, position);
                        if (childNearestDistance < nearestDistance)
                        {
                            nearestNode = childNearestNode;
                            nearestDistance = childNearestDistance; // Update nearestDistance
                        }
                    }
                }

            return nearestNode;
        }

        /// <summary>
        ///     Determines whether a node exists at the specified position.
        /// </summary>
        /// <param name="position">The position to check for a node.</param>
        /// <returns><c>true</c> if a node exists at the specified position; otherwise, <c>false</c>.</returns>
        public bool NodeExistsAt(Vector3 position)
        {
            if (Query(position) != null) return true;

            if (!IsLeafNode())
                foreach (var child in Children)
                    if (child.NodeExistsAt(position))
                        return true;

            return false;
        }

        /// <summary>
        ///     Subdivides the Octree into eight child Octrees.
        ///     This method is used when the number of nodes in the Octree exceeds the maximum limit.
        ///     Each child Octree represents an octant of the current Octree.
        /// </summary>
        /// <summary>
        ///     Subdivides the Octree into eight child Octrees.
        ///     This method is used when the number of nodes in the Octree exceeds the maximum limit.
        ///     Each child Octree represents an octant of the current Octree.
        /// </summary>
        private void Subdivide()
        {
            if (depth >= maxDepth) return;

            for (var i = 0; i < 8; i++)
            {
                Children[i]?.Nodes.Clear();
                var newOrigin = origin;
                newOrigin.x += halfDimension.x * ((i & 1) == 0 ? -0.5f : 0.5f);
                newOrigin.y += halfDimension.y * ((i & 2) == 0 ? -0.5f : 0.5f);
                newOrigin.z += halfDimension.z * ((i & 4) == 0 ? -0.5f : 0.5f);
                Children[i] = new Octree<T>(newOrigin, halfDimension * 0.5f, maxDepth, minPoints, maxPoints);
            }

            // Re-insert nodes into the correct child Octrees
            Nodes.ForEach(node => GetOctantContainingPoint(node.Position)?.Insert(node.Position, node.Data));
            Nodes.Clear();
        }

        /// <summary>
        ///     Merges the child Octrees into the current Octree.
        ///     This method is used when the number of nodes in the child Octrees is below the minimum limit.
        ///     All nodes from the child Octrees are moved to the current Octree.
        /// </summary>
        private void Merge()
        {
            if (IsLeafNode()) return;

            foreach (var child in Children)
                if (child != null)
                {
                    Nodes.AddRange(child.Nodes);
                    child.Nodes.Clear();
                }
        }

        /// <summary>
        ///     Checks if the Octree contains a point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if the Octree contains the point, false otherwise.</returns>
        private bool ContainsPoint(Vector3 point)
        {
            return point.x >= origin.x - halfDimension.x && point.x <= origin.x + halfDimension.x &&
                   point.y >= origin.y - halfDimension.y && point.y <= origin.y + halfDimension.y &&
                   point.z >= origin.z - halfDimension.z && point.z <= origin.z + halfDimension.z;
        }

        /*/// <summary>
        ///     Gets the child Octree (octant) that contains the point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The child Octree that contains the point, or null if no child Octree contains the point.</returns>
        */
        // private Octree<T> GetOctantContainingPoint(Vector3 point)
        // {
        //     var index = 0;
        //     if (point.x > origin.x) index |= 1;
        //     if (point.y > origin.y) index |= 2;
        //     if (point.z > origin.z) index |= 4;
        //     return Children[index] != null ? Children[index] : null;
        // }

        /// <summary>
        ///     Gets the child Octree (octant) that contains the point.
        ///     More readable version.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The child Octree that contains the point, or null if no child Octree contains the point.</returns>
        private Octree<T> GetOctantContainingPoint(Vector3 point)
        {
            // Determine which octant of the space should contain the point.
            var index = 0;
            if (point.x > origin.x) index += 1;
            if (point.y > origin.y) index += 2;
            if (point.z > origin.z) index += 4;

            // Return the child octree at the computed index if it exists, otherwise return null.
            return Children[index] != null ? Children[index] : null;
        }

        /// <summary>
        ///     Converts the Octree to a JSON string.
        ///     This method uses Unity's JsonUtility by default, but can be overridden in a subclass to use a different
        ///     serialization library.
        ///     If the NEWTONSOFT_JSON symbol is defined in the project settings, Newtonsoft.Json will be used instead.
        ///     Newtonsoft.Json can be installed via the NuGet package manager in Visual Studio, or downloaded directly from the
        ///     Newtonsoft website.
        /// </summary>
        /// <returns>A JSON string representing the Octree. If serialization fails, an empty string is returned.</returns>
        public virtual string ToJson()
        {
            try
            {
#if NEWTONSOFT_JSON
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
#else
                return JsonUtility.ToJson(this);
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to serialize Octree: " + e.Message);
                return string.Empty;
            }
        }

        /// <summary>
        ///     Creates an instance of the Octree class from a JSON string.
        ///     This method uses Unity's JsonUtility by default, but can be overridden in a subclass to use a different
        ///     deserialization library.
        ///     If the NEWTONSOFT_JSON symbol is defined in the project settings, Newtonsoft.Json will be used instead.
        ///     Newtonsoft.Json can be installed via the NuGet package manager in Visual Studio, or downloaded directly from the
        ///     Newtonsoft website.
        ///     Note: This method cannot be made virtual because it's a static method.
        ///     In C#, static methods cannot be virtual or abstract because they belong to the class itself,
        ///     not an instance of the class.
        ///     If you need to customize the deserialization behavior, you could consider using a factory pattern
        ///     or a deserialization strategy pattern.
        /// </summary>
        /// <param name="json">The JSON string representing the Octree.</param>
        /// <returns>A new instance of the Octree class. If deserialization fails, null is returned.</returns>
        public static Octree<T> FromJson(string json)
        {
            try
            {
#if NEWTONSOFT_JSON
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Octree<T>>(json);
#else
                return JsonUtility.FromJson<Octree<T>>(json);
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to deserialize Octree: " + e.Message);
                return null;
            }
        }
    }
}