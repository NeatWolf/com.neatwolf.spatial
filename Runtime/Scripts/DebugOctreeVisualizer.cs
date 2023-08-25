using System.Collections.Generic;
using NeatWolf.Spatial.Partitioning;
using UnityEngine;

namespace NeatWolf.Spatial
{
    public class DebugOctreeVisualizer : MonoBehaviour
    {
        public Octree<GameObject> Octree;

        public Color NodeColor = Color.green;
        public Color LeafNodeColor = Color.red;
        public Color DepthColor = Color.blue; // New color for depth visualization
        public float NodeSize = 0.1f;

        void OnDrawGizmosSelected()
        {
            if (Octree != null)
            {
                DrawOctree(Octree, 0); // Initialize depth to 0
            }
        }

        private void DrawOctree(Octree<GameObject> octree, int depth) // Add depth parameter
        {
            if (octree.IsLeafNode())
            {
                DrawLeafNodes(octree.Nodes); // Draw all leaf nodes
            }
            else
            {
                DrawNode(octree, depth); // Pass depth to DrawNode
                foreach (var child in octree.Children)
                {
                    if (child != null)
                    {
                        DrawOctree(child, depth + 1); // Increment depth for child nodes
                    }
                }
            }
        }

        private void DrawLeafNodes(List<OctreeNode<GameObject>> nodes)
        {
            Gizmos.color = LeafNodeColor;
            foreach (var node in nodes)
            {
                DrawCube(node.Position, Vector3.one * NodeSize);
                if (node.Data != null)
                {
                    Gizmos.DrawLine(node.Position, node.Data.transform.position);
                }
            }
        }


        private void DrawNode(Octree<GameObject> octree, int depth) // Add depth parameter
        {
            // Lerp between NodeColor and DepthColor based on depth
            Gizmos.color = Color.Lerp(NodeColor, DepthColor, depth / 10.0f);
            DrawCube(octree.Origin, octree.HalfDimension * 2);
        }

        private void DrawCube(Vector3 center, Vector3 size)
        {
            Vector3 halfSize = size * 0.5f;

            Gizmos.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
            Gizmos.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
            Gizmos.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));

            Gizmos.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
            Gizmos.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
            Gizmos.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));

            Gizmos.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
            Gizmos.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
            Gizmos.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));

            Gizmos.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
            Gizmos.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
            Gizmos.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
        }
    }
}