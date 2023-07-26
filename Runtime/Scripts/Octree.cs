using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeatWolf.Spatial.Partitioning
{

    public interface IOctreeVisitor<T>
    {
        void Visit(OctreeNode<T> node);
    }

    [Serializable]
    public class OctreeNode<T>
    {
        private bool enabled;

        public T Data { get; set; }
        public Vector3 Position { get; set; }

        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if(enabled != value)
                {
                    enabled = value;
                    InvalidateCache();
                }
            }
        }

        // Parent will not be serialized
        [NonSerialized]
        public Octree<T> Parent;

        public OctreeNode(T data, Vector3 position)
        {
            Data = data;
            Position = position;
        }

        internal void InvalidateCache()
        {
            Parent?.InvalidateCache();
        }
    }

    [Serializable]
    public class Octree<T>
    {
        public OctreeNode<T> Node { get; private set; }
        [field: NonSerialized]
        public List<Octree<T>> Children { get; set; }

        public Vector3 Origin
        {
            get => origin;
            set => origin = value;
        }

        public Vector3 HalfDimension
        {
            get => halfDimension;
            set => halfDimension = value;
        }

        private Vector3 origin;
        private Vector3 halfDimension;
        private int depth;
        private int maxDepth;
        private int minPoints;
        private int maxPoints;
        private int pointCount;
        [NonSerialized]
        private Dictionary<Vector3, OctreeNode<T>> cache;
        [NonSerialized]
        private Dictionary<Vector3, List<OctreeNode<T>>> sphereCastCache;

        // Default constructor for deserialization
        public Octree()
        {
            this.Children = new List<Octree<T>>(8);
            for (int i = 0; i < 8; i++)
            {
                Children.Add(null);
            }
            this.cache = new Dictionary<Vector3, OctreeNode<T>>();
            this.sphereCastCache = new Dictionary<Vector3, List<OctreeNode<T>>>();
        }

        public Octree(Vector3 origin, Vector3 halfDimension, int maxDepth, int minPoints, int maxPoints)
        {
            this.origin = origin;
            this.halfDimension = halfDimension;
            this.maxDepth = maxDepth;
            this.depth = 0;
            this.minPoints = minPoints;
            this.maxPoints = maxPoints;
            this.cache = new Dictionary<Vector3, OctreeNode<T>>();
            this.sphereCastCache = new Dictionary<Vector3, List<OctreeNode<T>>>();

            Children = new List<Octree<T>>(8);
            for (int i = 0; i < 8; i++)
            {
                Children.Add(null);
            }
        }

        public bool IsLeafNode()
        {
            return depth == maxDepth || Children.TrueForAll(child => child == null);
        }

        private int GetChildIndexForPoint(Vector3 point)
        {
            int index = 0;
            index |= (point.x > origin.x) ? 4 : 0;
            index |= (point.y > origin.y) ? 2 : 0;
            index |= (point.z > origin.z) ? 1 : 0;
            return index;
        }

        public bool Insert(OctreeNode<T> node)
        {
            if (!IsPointInside(node.Position))
                return false;

            node.Parent = this;
            InvalidateCache();

            if (IsLeafNode())
            {
                if (Node == null)
                {
                    Node = node;
                    pointCount = 1;
                    return true;
                }
                else
                {
                    if (Node.Position == node.Position)
                    {
                        Node = node;
                        return true;
                    }
                    else if (depth < maxDepth && pointCount >= maxPoints)
                    {
                        Subdivide();
                        return Insert(node);
                    }
                    else
                    {
                        pointCount++;
                    }
                }
            }
            else
            {
                int index = GetChildIndexForPoint(node.Position);
                if (Children[index] == null)
                {
                    Vector3 newOrigin = origin + new Vector3((index & 4) > 0 ? halfDimension.x : -halfDimension.x,
                        (index & 2) > 0 ? halfDimension.y : -halfDimension.y,
                        (index & 1) > 0 ? halfDimension.z : -halfDimension.z) / 2;
                    Children[index] = new Octree<T>(newOrigin, halfDimension / 2, maxDepth, minPoints, maxPoints);
                    Children[index].depth = this.depth + 1;
                }
                return Children[index].Insert(node);
            }

            return false;
        }

        private void Subdivide()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 newOrigin = origin + new Vector3((i & 4) > 0 ? halfDimension.x : -halfDimension.x,
                    (i & 2) > 0 ? halfDimension.y : -halfDimension.y,
                    (i & 1) > 0 ? halfDimension.z : -halfDimension.z) / 2;
                Children[i] = new Octree<T>(newOrigin, halfDimension / 2, maxDepth, minPoints, maxPoints);
                Children[i].depth = this.depth + 1;
            }
            Insert(Node);
            Node = null;
        }

        private bool IsPointInside(Vector3 point)
        {
            return (point.x >= (origin.x - halfDimension.x)) && (point.x <= (origin.x + halfDimension.x)) &&
                   (point.y >= (origin.y - halfDimension.y)) && (point.y <= (origin.y + halfDimension.y)) &&
                   (point.z >= (origin.z - halfDimension.z)) && (point.z <= (origin.z + halfDimension.z));
        }

        public bool Remove(Vector3 point)
        {
            if (!IsPointInside(point))
                return false;

            InvalidateCache();

            if (IsLeafNode())
            {
                if (Node != null && Node.Position == point)
                {
                    Node = null;
                    pointCount--;
                    return true;
                }
            }
            else
            {
                int index = GetChildIndexForPoint(point);
                if (Children[index]?.Remove(point) == true)
                {
                    pointCount--;
                    if (pointCount <= minPoints)
                    {
                        Merge();
                    }
                    return true;
                }
            }

            return false;
        }

        private void Merge()
        {
            Node = GetFirstNonEmptyNode();
            for (int i = 0; i < 8; i++)
            {
                Children[i] = null;
            }
        }

        private OctreeNode<T> GetFirstNonEmptyNode()
        {
            foreach (var child in Children)
            {
                if (child?.Node != null)
                    return child.Node;
            }
            return null;
        }

        public OctreeNode<T> Query(Vector3 point)
        {
            if (!IsPointInside(point))
                return null;

            if (cache.ContainsKey(point))
                return cache[point];

            if (IsLeafNode())
            {
                if (Node != null && Node.Position == point)
                {
                    cache[point] = Node;
                    return Node;
                }
            }
            else
            {
                int index = GetChildIndexForPoint(point);
                if (Children[index] != null)
                {
                    OctreeNode<T> result = Children[index].Query(point);
                    if (result != null)
                    {
                        cache[point] = result;
                        return result;
                    }
                }
            }

            return null;
        }

        public List<OctreeNode<T>> SphereCast(Vector3 center, float radius)
        {
            if (sphereCastCache.ContainsKey(center) && sphereCastCache[center].TrueForAll(node => node.Enabled))
                return sphereCastCache[center];

            List<OctreeNode<T>> nodesInSphere = new List<OctreeNode<T>>();

            if (IntersectsSphere(center, radius))
            {
                if (Node != null && Node.Enabled && (Node.Position - center).magnitude <= radius)
                {
                    nodesInSphere.Add(Node);
                }

                foreach (var child in Children)
                {
                    if (child != null && child.IntersectsSphere(center, radius))
                    {
                        nodesInSphere.AddRange(child.SphereCast(center, radius));
                    }
                }
            }

            sphereCastCache[center] = nodesInSphere;
            return nodesInSphere;
        }

        private bool IntersectsSphere(Vector3 center, float radius)
        {
            float x = Mathf.Max(origin.x - halfDimension.x, Mathf.Min(origin.x + halfDimension.x, center.x));
            float y = Mathf.Max(origin.y - halfDimension.y, Mathf.Min(origin.y + halfDimension.y, center.y));
            float z = Mathf.Max(origin.z - halfDimension.z, Mathf.Min(origin.z + halfDimension.z, center.z));

            float distance = Mathf.Sqrt((x - center.x) * (x - center.x) +
                                        (y - center.y) * (y - center.y) +
                                        (z - center.z) * (z - center.z));

            return distance <= radius;
        }

        internal void InvalidateCache()
        {
            cache.Clear();
            sphereCastCache.Clear();
            Node?.InvalidateCache();
            foreach (var child in Children)
            {
                child?.InvalidateCache();
            }
        }

        public void Accept(IOctreeVisitor<T> visitor)
        {
            visitor.Visit(Node);
            foreach (var child in Children)
            {
                child?.Accept(visitor);
            }
        }

        public OctreeNode<T> FindNearestNode(Vector3 point)
        {
            if (!IsPointInside(point))
                return null;

            if (IsLeafNode())
                return Node;

            int index = GetChildIndexForPoint(point);
            return Children[index]?.FindNearestNode(point);
        }

        public bool NodeExistsAt(Vector3 point)
        {
            if (!IsPointInside(point))
                return false;

            if (IsLeafNode())
                return Node?.Position == point;

            int index = GetChildIndexForPoint(point);
            return Children[index]?.NodeExistsAt(point) ?? false;
        }
    
/*#if NET_STANDARD_2_0
    using System.Runtime.Serialization.Formatters.Binary;
    
    // Note: The System.Runtime.Serialization.Formatters.Binary namespace is not available in .NET Standard 2.0 and higher.
    // If you are using .NET Standard 2.0 or higher, you may need to use alternative methods
    // for binary serialization and deserialization.

    public void SerializeToBinary(string filePath)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            bf.Serialize(fs, this);
        }
    }

    public static Octree<T> DeserializeFromBinary(string filePath)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            return bf.Deserialize(fs) as Octree<T>;
        }
    }
#endif*/
    
        public void SerializeToJson(string filePath)
        {
            string json = JsonUtility.ToJson(this);
            File.WriteAllText(filePath, json);
        }

        public static Octree<T> DeserializeFromJson(string filePath)
        {
            string json = File.ReadAllText(filePath);
            Octree<T> octree = JsonUtility.FromJson<Octree<T>>(json);

            // After deserialization, parent-child relationships need to be restored
            RestoreParentReferences(octree);

            return octree;
        }

        private static void RestoreParentReferences(Octree<T> octree)
        {
            if (octree.Node != null)
            {
                octree.Node.Parent = octree;
            }

            foreach (Octree<T> child in octree.Children)
            {
                if (child != null)
                {
                    RestoreParentReferences(child);
                }
            }
        }
    }
}