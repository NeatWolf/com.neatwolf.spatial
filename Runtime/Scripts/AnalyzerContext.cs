using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeatWolf.Spatial.Analysis
{
    /*[Serializable]
    public struct Cell
    {
        public bool solid;
    }*/
    [Serializable]
    public struct Cell
    {
        [FormerlySerializedAs("isSolid")] public bool solid;
        public bool Visited;
        public CellType Type;
        public Volume Volume;
    }
    
    [Serializable]
    public struct VolumeColors
    {
        public Color UnknownColor;
        public Color WallColor;
        public Color RoomColor;
        public Color CorridorColor;
        public Color AirColor;
        public Color PropColor;

        public VolumeColors(Color unknownColor, Color wallColor, Color roomColor, Color corridorColor, Color airColor, Color propColor)
        {
            UnknownColor = unknownColor;
            WallColor = wallColor;
            RoomColor = roomColor;
            CorridorColor = corridorColor;
            AirColor = airColor;
            PropColor = propColor;
        }
    }
    
    [CreateAssetMenu(fileName = "AnalyzerData", menuName = "AnalyzerData", order = 0)]
    public class AnalyzerContext : ScriptableObject
    {
        //public BoxCollider ColliderBounds; // Can't reference a scene object...
        public Bounds AnalysisBounds;
        public float  CellSize;
        public Vector3Int GridSize;
        public Vector3Int ChunkSize;
        public Color GizmoFreeColor;
        public Color GizmoOccupiedColor;
        public Material ChunkMaterial;
        public int ChecksPerCycle;
        public Vector3Int ChunkCount;
        //public Matrix<Cell> Cells; 
        [SerializeField]
        public Cell[,,] Cells = null;
        public MeshFilter[,,] chunkMeshFilters;
        public Mesh[,,] chunkMeshes;
        public GameObject[,,] chunkMeshGO;
        [SerializeField] public int CorridorToRoomThreshold;
        [SerializeField] public int RoomToAirThreshold;
        [SerializeField] public Dictionary<CellType,List<Volume>> VolumesByType;
        [SerializeField] public VolumeColors VolumeColors;
        public int VolumeChecksPerCycle;


        public void UpdateBounds(BoxCollider bounds)
        {
            AnalysisBounds.center = bounds.center;
            AnalysisBounds.size = bounds.size;
        }

        public void UpdateGridSize()
        {
            var boundsSize = AnalysisBounds.size;
            GridSize.x = Mathf.CeilToInt(boundsSize.x / CellSize);
            GridSize.y = Mathf.CeilToInt(boundsSize.y / CellSize);
            GridSize.z = Mathf.CeilToInt(boundsSize.z / CellSize);
        }
        
        public void UpdateChunkCount()//(int cellSize, Vector3Int chunkSize)
        {
            Vector3Int gridSize = GridSize;
            
            ChunkCount.x = Mathf.CeilToInt((float)gridSize.x / ChunkSize.x);
            ChunkCount.y = Mathf.CeilToInt((float)gridSize.y / ChunkSize.y);
            ChunkCount.z = Mathf.CeilToInt((float)gridSize.z / ChunkSize.z);
        }

    }
}