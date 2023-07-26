using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeatWolf.Spatial.Analysis
{
    public enum CellType : uint
    {
        Unknown = 0,
        Wall = 1,
        Room = 2,
        Corridor = 3,
        Air = 4,
        Prop = 5
    }

    public class Volume
    {
        public bool solid; 
        public CellType Type;
        public Vector3Int Min;
        public Vector3Int Max;

        public float GetVolumeSize(AnalyzerContext context)
        {
            return ((Max.x - Min.x + 1) * context.CellSize) *
                   ((Max.y - Min.y + 1) * context.CellSize) *
                   ((Max.z - Min.z + 1) * context.CellSize);
        }

        public bool IsBorderingGrid(AnalyzerContext context)
        {
            return Min.x <= 0 || Min.y <= 0 || Min.z <= 0 ||
                   Max.x >= context.GridSize.x - 1 || Max.y >= context.GridSize.y - 1 ||
                   Max.z >= context.GridSize.z - 1;
        }
    }

    public class VolumeAnalyzer
    {
        private AnalyzerContext _context;
        private int checksThisCycle;

        public VolumeAnalyzer(AnalyzerContext context)
        {
            this._context = context;
        }

        public IEnumerator AnalyzeVolumes()
        {
            _context.VolumesByType = new Dictionary<CellType, List<Volume>>();
            foreach (CellType cellType in Enum.GetValues(typeof(CellType)))
            {
                _context.VolumesByType[cellType] = new List<Volume>();
            }

            ClearVolumeData();

            for (int x = 0; x < _context.GridSize.x; x++)
            {
                for (int y = 0; y < _context.GridSize.y; y++)
                {
                    for (int z = 0; z < _context.GridSize.z; z++)
                    {
                        // int x, y, z;
                        // x = y = z = 0;
                        if (!_context.Cells[x, y, z].Visited)
                        {
                            _context.Cells[x, y, z].Visited = true;
                            
                            bool solidCell = _context.Cells[x, y, z].solid;// ? CellType.Wall : CellType.Unknown;
                            Volume volume = new Volume
                                { solid = solidCell, Min = new Vector3Int(x, y, z), Max = new Vector3Int(x, y, z) };
                            //Debug.Log("Post-inflate: Min:"+volume.Min+ " Max: "+volume.Max);
                            bool inflated;
                            do
                            {
                                inflated = InflateVolume(volume);
                                MarkCellsInVolume(volume, volume.Min, volume.Max);
                                //Debug.Log("Post-inflate: Min:"+volume.Min+ " Max: "+volume.Max);
                            } while (inflated);

                            DetermineVolumeType(volume);
                            _context.VolumesByType[volume.Type].Add(volume);
                            
                            checksThisCycle++;

                            if (checksThisCycle >= _context.VolumeChecksPerCycle)
                            {
                                checksThisCycle = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        private void ClearVolumeData()
        {
            for (int x = 0; x < _context.GridSize.x; x++)
            {
                for (int y = 0; y < _context.GridSize.y; y++)
                {
                    for (int z = 0; z < _context.GridSize.z; z++)
                    {
                        var cell = _context.Cells[x, y, z];

                        cell.Type = CellType.Unknown;
                        cell.Visited = false;
                        cell.Volume = null;

                        _context.Cells[x, y, z] = cell;
                    }
                }
            }
        }

        private bool InflateVolume(Volume volume)
        {
            bool didInflate = false;
            Vector3Int[] directions =
            {
                Vector3Int.forward, Vector3Int.up, Vector3Int.right, Vector3Int.back, Vector3Int.down, Vector3Int.left 
            };

            for (var directionIndex = 0; directionIndex < directions.Length; directionIndex++)
            {
                var direction = directions[directionIndex];
                Vector3Int newMin = volume.Min; // + Vector3Int.Min(direction, Vector3Int.zero);
                Vector3Int newMax = volume.Max; // + Vector3Int.Max(direction, Vector3Int.zero);

                if (directionIndex <= 2) // positive
                    newMax += direction;
                else
                    newMin += direction; // negative

                newMin.Clamp(Vector3Int.zero, _context.GridSize - Vector3Int.one);
                newMax.Clamp(Vector3Int.zero, _context.GridSize - Vector3Int.one);
                
                if (newMin == volume.Min && newMax == volume.Max) // no expansion is possible
                    continue;

                Vector3Int checkMin, checkMax;
                GetInflationBounds(volume, directionIndex, out checkMin, out checkMax);

                if (CanInflate(volume, checkMin, checkMax))
                {
                    volume.Min = newMin;
                    volume.Max = newMax;
                    MarkCellsInVolume(volume, checkMin, checkMax);
                    didInflate = true;
                }
            }

            return didInflate;
        }

        private void GetInflationBounds(Volume volume, int directionIndex,
            out Vector3Int checkMin, out Vector3Int checkMax)
        {
            switch (directionIndex)
            {
                case 0: // Forward
                    checkMin = new Vector3Int(volume.Min.x, volume.Min.y, volume.Max.z + 1);
                    checkMax = new Vector3Int(volume.Max.x, volume.Max.y, volume.Max.z + 1);
                    break;
                case 1: // Up
                    checkMin = new Vector3Int(volume.Min.x, volume.Max.y + 1, volume.Min.z);
                    checkMax = new Vector3Int(volume.Max.x, volume.Max.y + 1, volume.Max.z);
                    break;
                case 2: // Right
                    checkMin = new Vector3Int(volume.Max.x + 1, volume.Min.y, volume.Min.z);
                    checkMax = new Vector3Int(volume.Max.x + 1, volume.Max.y, volume.Max.z);
                    break;
                case 3: // Back
                    checkMin = new Vector3Int(volume.Min.x, volume.Min.y, volume.Min.z - 1);
                    checkMax = new Vector3Int(volume.Max.x, volume.Max.y, volume.Min.z - 1);
                    break;
                case 4: // Down
                    checkMin = new Vector3Int(volume.Min.x, volume.Min.y - 1, volume.Min.z);
                    checkMax = new Vector3Int(volume.Max.x, volume.Min.y - 1, volume.Max.z);
                    break;
                default: // Left
                    checkMin = new Vector3Int(volume.Min.x - 1, volume.Min.y, volume.Min.z);
                    checkMax = new Vector3Int(volume.Min.x - 1, volume.Max.y, volume.Max.z);
                    break;
            }
        }


        private bool CanInflate(Volume volume, Vector3Int min, Vector3Int max)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        if (x < 0 || x >= _context.GridSize.x || y < 0 || y >= _context.GridSize.y || z < 0 ||
                            z >= _context.GridSize.z)
                        {
                            return false;
                        }

                        if (_context.Cells[x, y, z].Visited)
                        {
                            return false;
                        }

                        if (_context.Cells[x, y, z].solid != volume.solid)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void MarkCellsInVolume(Volume volume, Vector3Int min, Vector3Int max)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        _context.Cells[x, y, z].Visited = true;
                    }
                }
            }
        }

        private void DetermineVolumeType(Volume volume)
        {
            if (volume.solid)
            {
                volume.Type = CellType.Wall;
                return;
            }
            
            if (volume.IsBorderingGrid(_context))
            {
                volume.Type = CellType.Air;
            }
            else
            {
                float volumeSize = volume.GetVolumeSize(_context);
                if (volumeSize > _context.RoomToAirThreshold)
                {
                    volume.Type = CellType.Air;
                }
                else if (volumeSize > _context.CorridorToRoomThreshold)
                {
                    volume.Type = CellType.Room;
                }
                else
                {
                    volume.Type = CellType.Corridor;
                }
            }
        }
        
        public void HandleInitialize(AnalyzerContext context)
        {
            // Implement logic to handle initialization here.
        }

        public void HandleContextUpdate(AnalyzerContext context)
        {
            //SetupVolumeTypeColors();
        }

        public void HandleGridAnalyzed(AnalyzerContext context)
        {
            // Implement logic to handle grid analyzed event here.
        }

        public void HandleChunksGenerated(AnalyzerContext context)
        {
            // Implement logic to handle chunks generated event here.
        }

        public void HandleAnalyzerDestroyed(AnalyzerContext context)
        {
            // Implement logic to handle analyzer destroyed event here.
        }

        public void VolumesAnalyzed(AnalyzerContext context)
        {
            //
        }
    }
}