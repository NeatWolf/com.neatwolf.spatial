using System.Collections;
using UnityEngine;

namespace NeatWolf.Spatial.Analysis
{
    public class GridBuilder
    {
        private readonly AnalyzerContext _context;


        public GridBuilder(AnalyzerContext context)
        {
            _context = context;
        }

        public void HandleInitialize(AnalyzerContext context)
        {
            //_context = context;

            //context.Cells = new Matrix<Cell>(context.CellSize, context.CellSize, context.CellSize, Allocator.Persistent);
            //context.Cells = new Cell[context.GridSize.x, context.GridSize.y, context.GridSize.z];
        }

        public void HandleContextUpdate(AnalyzerContext context)
        {
            //_context = context;

            //HandleInitialize(context);
        }

        public IEnumerator AnalyzeScene()
        {
            var startPoint = _context.AnalysisBounds.center - _context.AnalysisBounds.size / 2f;
            var checksThisCycle = 0;

            for (var z = 0; z < _context.GridSize.z; z++)
            for (var y = 0; y < _context.GridSize.y; y++)
            for (var x = 0; x < _context.GridSize.x; x++)
            {
                var worldPoint = startPoint + new Vector3(x, y, z) * _context.CellSize;
                //Debug.DrawRay(worldPoint, Vector3.up*_context.CellSize *0.5f, Color.cyan, 0.01f);
                var isOccupied = Physics.CheckBox(worldPoint, Vector3.one * (_context.CellSize * 0.5f));
                _context.Cells[x, y, z] = new Cell { solid = isOccupied };

                checksThisCycle++;

                if (checksThisCycle >= _context.ChecksPerCycle)
                {
                    checksThisCycle = 0;
                    yield return null;
                }
            }

            Debug.Log("Scene analysis completed.");
        }

        public void HandleGridAnalyzed(AnalyzerContext context)
        {
            // blank handler for now
        }

        public void HandleChunksGenerated(AnalyzerContext context)
        {
            // blank handler for now
        }

        public void HandleAnalyzerDestroyed(AnalyzerContext context)
        {
            //_context = context;
            // deallocate memory

            context.Cells = null;
            context.GridSize = Vector3Int.zero;
            context.ChunkCount = Vector3Int.zero;
        }

        public void VolumesAnalyzed(AnalyzerContext context)
        {
            // blank handler for now
        }
    }
}