using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace NeatWolf.Spatial.Analysis
{
    [ExecuteAlways]
    public class SceneAnalyzer : MonoBehaviour
    {
        public delegate void SceneAnalyzerDelegate(AnalyzerContext context);

        [SerializeField] private AnalyzerContext context;

        [FormerlySerializedAs("analysisBounds")] [SerializeField]
        internal BoxCollider boxColliderBounds;

        [Range(0.25f, 4f)] [SerializeField] private float cellSize;
        [SerializeField] private int checksPerCycle = 100;
        [SerializeField] private Vector3Int chunkSize = new(16, 16, 16);
        [SerializeField] private Color gizmoFreeColor = Color.green;
        [SerializeField] private Color gizmoOccupiedColor = Color.red;
        [SerializeField] private Material chunkMaterial;

        [HideInInspector,SerializeField] private float _previousCellSize;
        
#if UNITY_EDITOR
        private EditorCoroutine _analyzeCoroutine;
        private EditorCoroutine _generateChunksCoroutine;
#endif
        
        private GizmoDrawer _gizmoDrawer;
        private GridBuilder _gridBuilder;
        private VolumeAnalyzer _volumeAnalyzer;

        private MeshGenerator _meshGenerator;

        public BoxCollider BoxColliderBounds
        {
            get => boxColliderBounds;
            set => boxColliderBounds = value;
        }
        //private static BoxCollider ColliderBounds;

        /*private void Update()
        {
            UpdateContext();
        }*/

        private void OnEnable()
        {
            StopCoroutines();
            _previousCellSize = cellSize;
            InitializeComponents();
        }

        private void OnDisable()
        {
            StopCoroutines();
            DeregisterEvents();
            OnAnalyzerDestroyed?.Invoke(context);
        }

        private void OnDrawGizmos()
        {
            if (_gizmoDrawer != null)
                _gizmoDrawer.DrawGizmos();
        }

        private void OnValidate()
        {
            UpdateContext();
        }

        private void UpdateContext()
        {
            if (context == null)
                return;

            /*cellSize = Mathf.Clamp(cellSize, 0.25f, 4);
            if (Math.Abs(_previousCellSize - cellSize) < 0.01) return;*/

            //SceneAnalyzer.ColliderBounds = boxColliderBounds;
            //context.ColliderBounds = SceneAnalyzer.ColliderBounds;//boxColliderBounds;
            context.UpdateBounds(boxColliderBounds);
            //context.AnalysisBounds = new Bounds(boxColliderBounds.bounds.center, boxColliderBounds.bounds.size);
            context.CellSize = cellSize;
            context.UpdateGridSize();
            context.ChecksPerCycle = checksPerCycle;
            context.ChunkSize = chunkSize;
            context.UpdateChunkCount();
            context.ChecksPerCycle = checksPerCycle;
            context.GizmoFreeColor = gizmoFreeColor;
            context.GizmoOccupiedColor = gizmoOccupiedColor;
            context.ChunkMaterial = chunkMaterial;
            context.Cells = new Cell[context.GridSize.x, context.GridSize.y, context.GridSize.z];

            _previousCellSize = cellSize;


            EditorUtility.SetDirty(context);
            AssetDatabase.SaveAssets();

            OnContextUpdate?.Invoke(context);
            Debug.Log("Updated  context");
        }

        // public void UpdateContext()
        // {
        //     OnContextUpdate?.Invoke(context);
        // }

        public event SceneAnalyzerDelegate OnInitialize;
        public event SceneAnalyzerDelegate OnContextUpdate;
        public event SceneAnalyzerDelegate OnGridAnalyzed;
        public event SceneAnalyzerDelegate OnVolumesAnalyzed;
        public event SceneAnalyzerDelegate OnChunksGenerated;
        public event SceneAnalyzerDelegate OnAnalyzerDestroyed;

        public void InitializeComponents()
        {
            if (_gridBuilder == null)
            {
                _gridBuilder = new GridBuilder(context);
                OnInitialize += _gridBuilder.HandleInitialize;
                OnContextUpdate += _gridBuilder.HandleContextUpdate;
                OnGridAnalyzed += _gridBuilder.HandleGridAnalyzed;
                OnVolumesAnalyzed += _gridBuilder.VolumesAnalyzed;
                OnChunksGenerated += _gridBuilder.HandleChunksGenerated;
                OnAnalyzerDestroyed += _gridBuilder.HandleAnalyzerDestroyed;
            }

            if (_volumeAnalyzer == null)
            {
                _volumeAnalyzer = new VolumeAnalyzer(context);
                OnInitialize += _volumeAnalyzer.HandleInitialize;
                OnContextUpdate += _volumeAnalyzer.HandleContextUpdate;
                OnGridAnalyzed += _volumeAnalyzer.HandleGridAnalyzed;
                OnVolumesAnalyzed += _volumeAnalyzer.VolumesAnalyzed;
                OnChunksGenerated += _volumeAnalyzer.HandleChunksGenerated;
                OnAnalyzerDestroyed += _volumeAnalyzer.HandleAnalyzerDestroyed;
            }
            
            if (_gizmoDrawer == null)
            {
                _gizmoDrawer = new GizmoDrawer(context);
                OnInitialize += _gizmoDrawer.HandleInitialize;
                OnContextUpdate += _gizmoDrawer.HandleContextUpdate;
                OnGridAnalyzed += _gizmoDrawer.HandleGridAnalyzed;
                OnVolumesAnalyzed += _gizmoDrawer.VolumesAnalyzed;
                OnChunksGenerated += _gizmoDrawer.HandleChunksGenerated;
                OnAnalyzerDestroyed += _gizmoDrawer.HandleAnalyzerDestroyed;
            }
            
            if (_meshGenerator == null)
            {
                _meshGenerator = new MeshGenerator(context, this);
                OnInitialize += _meshGenerator.HandleInitialize;
                OnContextUpdate += _meshGenerator.HandleContextUpdate;
                OnGridAnalyzed += _meshGenerator.HandleGridAnalyzed;
                OnVolumesAnalyzed += _meshGenerator.VolumesAnalyzed;
                OnChunksGenerated += _meshGenerator.HandleChunksGenerated;
                OnAnalyzerDestroyed += _meshGenerator.HandleAnalyzerDestroyed;
            }

            OnInitialize?.Invoke(context);
        }


        private void DeregisterEvents()
        {
            OnInitialize -= _gridBuilder.HandleInitialize;
            OnContextUpdate -= _gridBuilder.HandleContextUpdate;
            OnGridAnalyzed -= _gridBuilder.HandleGridAnalyzed;
            OnVolumesAnalyzed -= _gridBuilder.VolumesAnalyzed;
            OnChunksGenerated -= _gridBuilder.HandleChunksGenerated;
            OnAnalyzerDestroyed -= _gridBuilder.HandleAnalyzerDestroyed;
            
            OnInitialize -= _volumeAnalyzer.HandleInitialize;
            OnContextUpdate -= _volumeAnalyzer.HandleContextUpdate;
            OnGridAnalyzed -= _volumeAnalyzer.HandleGridAnalyzed;
            OnVolumesAnalyzed -= _volumeAnalyzer.VolumesAnalyzed;
            OnChunksGenerated -= _volumeAnalyzer.HandleChunksGenerated;
            OnAnalyzerDestroyed -= _volumeAnalyzer.HandleAnalyzerDestroyed;
            
            OnInitialize -= _gizmoDrawer.HandleInitialize;
            OnContextUpdate -= _gizmoDrawer.HandleContextUpdate;
            OnGridAnalyzed -= _gizmoDrawer.HandleGridAnalyzed;
            OnVolumesAnalyzed -= _gizmoDrawer.VolumesAnalyzed;
            OnChunksGenerated -= _gizmoDrawer.HandleChunksGenerated;
            OnAnalyzerDestroyed -= _gizmoDrawer.HandleAnalyzerDestroyed;
            
            OnInitialize -= _meshGenerator.HandleInitialize;
            OnContextUpdate -= _meshGenerator.HandleContextUpdate;
            OnGridAnalyzed -= _meshGenerator.HandleGridAnalyzed;
            OnVolumesAnalyzed -= _meshGenerator.VolumesAnalyzed;
            OnChunksGenerated -= _meshGenerator.HandleChunksGenerated;
            OnAnalyzerDestroyed -= _meshGenerator.HandleAnalyzerDestroyed;
        }


        public void StartAnalyzeScene()
        {
            if (!IsAnalysisRunning())
                EditorCoroutineUtility.StartCoroutineOwnerless(AnalyzeAndGenerate());
        }

        private bool _colliderWasEnabled;
        private EditorCoroutine _volumesCoroutine;

        private IEnumerator AnalyzeAndGenerate()
        {
            _colliderWasEnabled= boxColliderBounds.enabled;
            try
            {
                UpdateContext();
                boxColliderBounds.enabled = false;
                context.VolumesByType = new Dictionary<CellType, List<Volume>>();
                //_gridBuilder.HandleContextUpdate(context);
                _analyzeCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(_gridBuilder.AnalyzeScene());
                yield return _analyzeCoroutine;
                OnGridAnalyzed?.Invoke(context);

                _volumesCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(_volumeAnalyzer.AnalyzeVolumes());
                yield return _volumesCoroutine;
                OnVolumesAnalyzed?.Invoke(context);
                
                //yield return EditorCoroutineUtility.StartCoroutineOwnerless(_meshGenerator.GenerateMeshChunks());
                //OnChunksGenerated?.Invoke(context);
            }
            finally
            {
                StopAnalysis();
            }
        }


        private void StopCoroutines()
        {
            if (_analyzeCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_analyzeCoroutine);
                _analyzeCoroutine = null;
            }
            
            if (_volumesCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_volumesCoroutine);
                _volumesCoroutine = null;
            }

            if (_generateChunksCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_generateChunksCoroutine);
                _generateChunksCoroutine = null;
            }
        }

        public bool IsAnalysisRunning()
        {
            return _analyzeCoroutine != null
                   || _volumesCoroutine != null
                   || _generateChunksCoroutine != null;
        }

        public void StopAnalysis()
        {
            StopCoroutines();
            boxColliderBounds.enabled = _colliderWasEnabled;
        }
    }
}