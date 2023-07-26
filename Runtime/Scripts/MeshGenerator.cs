using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace NeatWolf.Spatial.Analysis
{
    public class MeshGenerator
    {
        private AnalyzerContext _context;
        private MonoBehaviour _monoBehaviour;

        private readonly Vector3Int[] adjacentOffsets =
        {
            new Vector3Int(1, 0, 0),  // right
            new Vector3Int(0, 1, 0),  // up
            new Vector3Int(0, 0, 1)   // forward
        };

        private readonly int[,] faceTriangles =
        {
            {5, 4, 7, 7, 4, 6}, // right
            {1, 5, 3, 3, 5, 7}, // up
            {2, 3, 6, 6, 3, 7}  // forward
        };

        private readonly Vector3[] faceVertices =
        {
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(0, 1, 0), // 2
            new Vector3(1, 1, 0), // 3
            new Vector3(0, 0, 1), // 4
            new Vector3(1, 0, 1), // 5
            new Vector3(0, 1, 1), // 6
            new Vector3(1, 1, 1)  // 7
        };

        public MeshGenerator(AnalyzerContext context, MonoBehaviour monoBehaviour)
        {
            this._context = context;
            this._monoBehaviour = monoBehaviour;
            //InitializeChunkMeshes();
        }

        private void InitializeChunkMeshes()
        {
            for (int x = 0; x < _context.ChunkCount.x; x++)
            {
                for (int y = 0; y < _context.ChunkCount.y; y++)
                {
                    for (int z = 0; z < _context.ChunkCount.z; z++)
                    {
                        GameObject chunkObject = new GameObject($"Chunk ({x}, {y}, {z})")
                        {
                            transform =
                            {
                                parent = _monoBehaviour.transform
                            }
                        };
                        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                        Mesh mesh = new Mesh();

                        meshFilter.sharedMesh = mesh;
                        _context.chunkMeshFilters[x, y, z] = meshFilter;
                        _context.chunkMeshes[x, y, z] = mesh;
                        _context.chunkMeshGO[x, y, z] = chunkObject;

                        meshRenderer.sharedMaterial = _context.ChunkMaterial;
                        chunkObject.hideFlags = HideFlags.NotEditable;
                    }
                }
            }
        }
        
        private void DisposeChunkMeshes()
        {
            if (_context.chunkMeshGO != null)
            {
                foreach (var chunkObject in _context.chunkMeshGO)
                {
                    if(chunkObject != null)
                    {
                        var meshFilter = chunkObject.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            if (Application.isPlaying)
                            {
                                UnityEngine.Object.Destroy(meshFilter.sharedMesh);
                            }
                            else
                            {
                                UnityEngine.Object.DestroyImmediate(meshFilter.sharedMesh, true); 
                            }
                        
                            meshFilter.sharedMesh = null;
                        }

                        if (Application.isPlaying)
                        {
                            UnityEngine.Object.Destroy(chunkObject);
                        }
                        else
                        {
                            UnityEngine.Object.DestroyImmediate(chunkObject);
                        }
                    }
                }

                Array.Clear(_context.chunkMeshGO, 0, _context.chunkMeshGO.Length);
            }

            if (_context.chunkMeshFilters != null)
            {
                Array.Clear(_context.chunkMeshFilters, 0, _context.chunkMeshFilters.Length);
            }

            if (_context.chunkMeshes != null)
            {
                Array.Clear(_context.chunkMeshes, 0, _context.chunkMeshes.Length);
            }
        }


        public IEnumerator GenerateMeshChunks()
        {
            for (int x = 0; x < _context.ChunkCount.x; x++)
            {
                for (int y = 0; y < _context.ChunkCount.y; y++)
                {
                    for (int z = 0; z < _context.ChunkCount.z; z++)
                    {
                        GenerateChunkMesh(x, y, z);
                        yield return null;
                    }
                }
            }
        }

        private void GenerateChunkMesh(int chunkX, int chunkY, int chunkZ)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            int startX = chunkX * _context.ChunkSize.x;
            int startY = chunkY * _context.ChunkSize.y;
            int startZ = chunkZ * _context.ChunkSize.z;

            for (int localZ = 0; localZ < _context.ChunkSize.z; localZ++)
            {
                int worldZ = startZ + localZ;
                if (worldZ >= _context.Cells.GetLength(2)) break;

                for (int localY = 0; localY < _context.ChunkSize.y; localY++)
                {
                    int worldY = startY + localY;
                    if (worldY >= _context.Cells.GetLength(1)) break;

                    for (int localX = 0; localX < _context.ChunkSize.x; localX++)
                    {
                        int worldX = startX + localX;
                        if (worldX >= _context.Cells.GetLength(0)) break;

                        if (_context.Cells[worldX, worldY, worldZ].solid)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int adjacentX = worldX + adjacentOffsets[i].x;
                                int adjacentY = worldY + adjacentOffsets[i].y;
                                int adjacentZ = worldZ + adjacentOffsets[i].z;

                                if (adjacentX >= _context.Cells.GetLength(0) || adjacentY >= _context.Cells.GetLength(1) || adjacentZ >= _context.Cells.GetLength(2))
                                    continue;

                                bool isAdjacentCellSolid = _context.Cells[adjacentX, adjacentY, adjacentZ].solid;

                                if (!isAdjacentCellSolid)
                                {
                                    AddFace(vertices, triangles, worldX, worldY, worldZ, i);
                                }
                            }
                        }
                    }
                }
            }

            Mesh mesh = _context.chunkMeshes[chunkX, chunkY, chunkZ];
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
        }

        private void AddFace(List<Vector3> vertices, List<int> triangles, int x, int y, int z, int faceIndex)
        {
            int vertCount = vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                int vertexIndex = faceTriangles[faceIndex, (i < 2) ? i : i + 1];
                Vector3 vertexPosition = new Vector3(x, y, z) + faceVertices[vertexIndex];
                vertices.Add(vertexPosition);
            }

            for (int i = 0; i < 6; i++)
            {
                triangles.Add(vertCount + faceTriangles[faceIndex, i] % 4);
            }
        }
    
        public void HandleInitialize(AnalyzerContext context)
        {
            // Implement logic to handle initialization here.
        }

        public void HandleContextUpdate(AnalyzerContext context)
        {
            // Implement logic to handle context updates here.
        }

        public void HandleGridAnalyzed(AnalyzerContext context)
        {
        //    this._context = context;
        //    InitializeChunkMeshes();
        }

        public void HandleChunksGenerated(AnalyzerContext context)
        {
            // Implement logic to handle chunks generated event here.
        }

        public void HandleAnalyzerDestroyed(AnalyzerContext context)
        {
            DisposeChunkMeshes();
        }

        public void VolumesAnalyzed(AnalyzerContext context)
        {
            //
        }
    }
}