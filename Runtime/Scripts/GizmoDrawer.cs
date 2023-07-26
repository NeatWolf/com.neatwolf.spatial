using UnityEngine;
using System.Collections.Generic;

namespace NeatWolf.Spatial.Analysis
{
    public class GizmoDrawer
    {
        private AnalyzerContext _context;
        private Dictionary<CellType, Color> _volumeTypeColors;

        public GizmoDrawer(AnalyzerContext context)
        {
            this._context = context;
            this._volumeTypeColors = new Dictionary<CellType, Color>();
            SetupVolumeTypeColors();
        }

        private void SetupVolumeTypeColors()
        {
            _volumeTypeColors[CellType.Unknown] = _context.VolumeColors.UnknownColor;
            _volumeTypeColors[CellType.Wall] = _context.VolumeColors.WallColor;
            _volumeTypeColors[CellType.Room] = _context.VolumeColors.RoomColor;
            _volumeTypeColors[CellType.Corridor] = _context.VolumeColors.CorridorColor;
            _volumeTypeColors[CellType.Air] = _context.VolumeColors.AirColor;
            _volumeTypeColors[CellType.Prop] = _context.VolumeColors.PropColor;
        }

        public void DrawGizmos()
        {
            if (_context == null)
                return;
            if (_context.Cells == null)
                return;
            if (_context.Cells.GetLength(0) < 3)
                return;

            Vector3 startPoint = _context.AnalysisBounds.center - _context.AnalysisBounds.size / 2f;

            //DrawCells(startPoint);
            DrawVolumeGizmos(startPoint);
        }

        private void DrawCells(Vector3 startPoint)
        {
            Vector3 offset = Vector3.zero;
            bool solid;
            for (int z = 0; z < _context.GridSize.z; z++)
            {
                offset.z = z * _context.CellSize;
                for (int y = 0; y < _context.GridSize.y; y++)
                {
                    offset.y = y * _context.CellSize;
                    for (int x = 0; x < _context.GridSize.x; x++)
                    {
                        solid = _context.Cells[x, y, z].solid;
                        if (!solid)
                            continue;

                        offset.x = x * _context.CellSize;
                        Vector3 worldPoint = startPoint + offset;
                        Gizmos.color = solid? _context.GizmoOccupiedColor: _context.GizmoFreeColor;
                        Gizmos.DrawWireCube(worldPoint, Vector3.one * _context.CellSize * 0.5f);
                    }
                }
            }
        }

        private void DrawVolumeGizmos(Vector3 startPoint)
        {
            if (!_context)
                return;
            if (_context.VolumesByType == null)
                return;
            if (_context.VolumesByType.Count <= 0)
                return;

            foreach (CellType cellType in _context.VolumesByType.Keys)
            {
                if (_volumeTypeColors[cellType].a == 0) // Skip if alpha is 0
                    continue;

                Gizmos.color = _volumeTypeColors[cellType];
                foreach (Volume volume in _context.VolumesByType[cellType])
                {
                    Vector3 position = startPoint + new Vector3(volume.Min.x + (volume.Max.x - volume.Min.x) / 2f,
                                                   volume.Min.y + (volume.Max.y - volume.Min.y) / 2f,
                                                   volume.Min.z + (volume.Max.z - volume.Min.z) / 2f) * _context.CellSize;
                    Vector3 size = new Vector3(volume.Max.x - volume.Min.x + 1,
                                               volume.Max.y - volume.Min.y + 1,
                                               volume.Max.z - volume.Min.z + 1) * _context.CellSize;
                    Gizmos.DrawWireCube(position, size);
                    Gizmos.DrawLine(startPoint + (Vector3)volume.Min * _context.CellSize, startPoint + (Vector3)volume.Max * _context.CellSize);
                }
            }
        }

        public void HandleInitialize(AnalyzerContext context)
        {
            // Implement logic to handle initialization here.
        }

        public void HandleContextUpdate(AnalyzerContext context)
        {
            SetupVolumeTypeColors();
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
