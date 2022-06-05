using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace TerrainPaintRestore
{
    [ExecuteInEditMode]
    public class TerrainPaintRestore : MonoBehaviour
    {
        [Tooltip("All terrains to backup/restore")]
        public Terrain[] terrains;

        [NonSerialized]
        public GuiAllTerrainData activeTerrain;

        [NonSerialized]
        public List<string> backups = new List<string>();

        [HideInInspector]
        public int selectedBackup = -1;

        [HideInInspector]
        public float brushSize = 20f;

        [HideInInspector]
        public bool restoreHeight = true;

        [HideInInspector]
        public bool restoreTexture = true;

        [HideInInspector]
        public bool restoreDetails = true;

        [HideInInspector]
        public bool restoreTrees = true;

        private const float _updateRate = 1f / 30f;
        private const int _brushIndicatorSteps = 10;
        private Vector3? _mouseWorld;
        private bool _mouseIsDown = false;
        private double _lastRestoreTime;

        public bool CanRestorePaint()
        {
            return activeTerrain != null && Selection.activeGameObject == gameObject;
        }

        public void DeleteBackup(string id)
        {
            string baseScenePath = EditorSceneManager.GetActiveScene().path;
            string baseSceneDirectory = Path.GetDirectoryName(baseScenePath);
            string sceneName = Path.GetFileNameWithoutExtension(baseScenePath);
            string fileName = $"{baseSceneDirectory}/terrain-backup-{sceneName}-{id}.dat";

            if (AssetDatabase.DeleteAsset(fileName))
            {
                backups.Remove(id);

                if (backups.Count > 0)
                {
                    selectedBackup = 0;
                }
            }
        }

        public void BackupTerrain()
        {
            if (terrains != null && terrains.Length > 0)
            {
                string id = DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss");

                var backup = new GuiAllTerrainData();

                backup.terrains = new GuiTerrainData[terrains.Length];

                for (int i = 0; i < terrains.Length; i++)
                {
                    backup.terrains[i] = new GuiTerrainData();
                    backup.terrains[i].heights = terrains[i].terrainData.GetHeights(0, 0, terrains[i].terrainData.heightmapResolution, terrains[i].terrainData.heightmapResolution);
                    backup.terrains[i].alphaMaps = terrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].terrainData.alphamapWidth, terrains[i].terrainData.alphamapHeight);
                    backup.terrains[i].detailLayers = new GuiDetailLayer[terrains[i].terrainData.detailPrototypes.Length];
                    backup.terrains[i].detailWidth = terrains[i].terrainData.detailWidth;
                    backup.terrains[i].detailHeight = terrains[i].terrainData.detailHeight;

                    for (int n = 0; n < terrains[i].terrainData.detailPrototypes.Length; n++)
                    {
                        backup.terrains[i].detailLayers[n] = new GuiDetailLayer
                        {
                            data = terrains[i].terrainData.GetDetailLayer(0, 0, terrains[i].terrainData.detailWidth, terrains[i].terrainData.detailHeight, n)
                        };

                        if (terrains[i].terrainData.detailPrototypes[n].prototypeTexture != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(terrains[i].terrainData.detailPrototypes[n].prototypeTexture, out string prototypeTextureGuid, out long _))
                        {
                            backup.terrains[i].detailLayers[n].prototypeTextureGuid = prototypeTextureGuid;
                        }

                        if (terrains[i].terrainData.detailPrototypes[n].prototype != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(terrains[i].terrainData.detailPrototypes[n].prototype, out string prototypeGuid, out long _))
                        {
                            backup.terrains[i].detailLayers[n].prototypeGuid = prototypeGuid;
                        }
                    }

                    backup.terrains[i].treePrototypes = new string[terrains[i].terrainData.treePrototypes.Length];
                    for (int n = 0; n < terrains[i].terrainData.treePrototypes.Length; n++)
                    {
                        if (terrains[i].terrainData.treePrototypes[n].prefab != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(terrains[i].terrainData.treePrototypes[n].prefab, out string prefabGuid, out long _))
                        {
                            backup.terrains[i].treePrototypes[n] = prefabGuid;
                        }
                    }

                    backup.terrains[i].splatPrototypes = new string[terrains[i].terrainData.splatPrototypes.Length];
                    for (int n = 0; n < terrains[i].terrainData.splatPrototypes.Length; n++)
                    {
                        if (terrains[i].terrainData.splatPrototypes[n].texture != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(terrains[i].terrainData.splatPrototypes[n].texture, out string textureGuid, out long _))
                        {
                            backup.terrains[i].splatPrototypes[n] = textureGuid;
                        }
                    }

                    backup.terrains[i].alphamapWidth = terrains[i].terrainData.alphamapWidth;
                    backup.terrains[i].alphamapHeight = terrains[i].terrainData.alphamapHeight;
                    backup.terrains[i].heightmapResolution = terrains[i].terrainData.heightmapResolution;
                    backup.terrains[i].treeInstances = new GuiTreeInstance[terrains[i].terrainData.treeInstances.Length];

                    for (int j = 0; j < terrains[i].terrainData.treeInstances.Length; j++)
                    {
                        var tree = terrains[i].terrainData.GetTreeInstance(j);

                        backup.terrains[i].treeInstances[j] = new GuiTreeInstance
                        {
                            position = tree.position,
                            widthScale = tree.widthScale,
                            heightScale = tree.heightScale,
                            rotation = tree.rotation,
                            color = tree.color,
                            lightmapColor = tree.lightmapColor,
                            prototypeIndex = tree.prototypeIndex
                        };
                    }
                }

                string baseScenePath = EditorSceneManager.GetActiveScene().path;
                string baseSceneDirectory = Path.GetDirectoryName(baseScenePath);
                string sceneName = Path.GetFileNameWithoutExtension(baseScenePath);

                using (var fs = new FileStream($"{baseSceneDirectory}/terrain-backup-{sceneName}-{id}.dat", FileMode.Create))
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    backup.Save(bw);
                    ms.Position = 0;
                    ms.CopyTo(fs);
                }

                backups.Insert(0, id);

                selectedBackup = 0;
            }
        }

        public void RestoreTerrain(Rect worldSpace)
        {
            if (activeTerrain != null)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    terrains[i].GetComponent<TerrainCollider>().enabled = false;
                }

                for (int i = 0; i < terrains.Length; i++)
                {
                    float relativeHitTerX = (worldSpace.x - terrains[i].transform.position.x) / terrains[i].terrainData.size.x;
                    float relativeHitTerZ = (worldSpace.y - terrains[i].transform.position.z) / terrains[i].terrainData.size.z;
                    float relativeHitTerXX = relativeHitTerX + (worldSpace.width / terrains[i].terrainData.size.x);
                    float relativeHitTerZZ = relativeHitTerZ + (worldSpace.height / terrains[i].terrainData.size.z);

                    relativeHitTerX = Math.Min(1f, Math.Max(0, relativeHitTerX));
                    relativeHitTerZ = Math.Min(1f, Math.Max(0, relativeHitTerZ));
                    relativeHitTerXX = Math.Min(1f, Math.Max(0, relativeHitTerXX));
                    relativeHitTerZZ = Math.Min(1f, Math.Max(0, relativeHitTerZZ));

                    var rect = new Rect(relativeHitTerX, relativeHitTerZ, relativeHitTerXX - relativeHitTerX, relativeHitTerZZ - relativeHitTerZ);

                    var backup = activeTerrain.terrains[i];

                    if (restoreTexture)
                    {
                        int x = (int)(rect.x * backup.alphamapWidth);
                        int y = (int)(rect.y * backup.alphamapHeight);
                        int xx = (int)((rect.x + rect.width) * backup.alphamapWidth);
                        int yy = (int)((rect.y + rect.height) * backup.alphamapHeight);

                        x = Math.Min(backup.alphamapWidth, Math.Max(0, x));
                        y = Math.Min(backup.alphamapHeight, Math.Max(0, y));
                        xx = Math.Min(backup.alphamapWidth, Math.Max(0, xx));
                        yy = Math.Min(backup.alphamapHeight, Math.Max(0, yy));

                        if (xx - x > 0 && yy - y > 0)
                        {
                            terrains[i].terrainData.SetAlphamaps(x, y, backup.GetAlphamaps(x, y, xx - x, yy - y, terrains[i].terrainData));
                        }
                    }

                    if (restoreHeight)
                    {
                        int x = (int)(rect.x * backup.heightmapResolution);
                        int y = (int)(rect.y * backup.heightmapResolution);
                        int xx = (int)((rect.x + rect.width) * backup.heightmapResolution);
                        int yy = (int)((rect.y + rect.height) * backup.heightmapResolution);

                        x = Math.Min(backup.heightmapResolution, Math.Max(0, x));
                        y = Math.Min(backup.heightmapResolution, Math.Max(0, y));
                        xx = Math.Min(backup.heightmapResolution, Math.Max(0, xx));
                        yy = Math.Min(backup.heightmapResolution, Math.Max(0, yy));

                        if (xx - x > 0 && yy - y > 0)
                        {
                            terrains[i].terrainData.SetHeights(x, y, backup.GetHeights(x, y, xx - x, yy - y));
                        }
                    }

                    if (restoreDetails)
                    {
                        for (int n = 0; n < terrains[i].terrainData.detailPrototypes.Length; n++)
                        {
                            int x = (int)(rect.x * backup.detailWidth);
                            int y = (int)(rect.y * backup.detailHeight);
                            int xx = (int)((rect.x + rect.width) * backup.detailWidth);
                            int yy = (int)((rect.y + rect.height) * backup.detailHeight);

                            x = Math.Min(backup.detailWidth, Math.Max(0, x));
                            y = Math.Min(backup.detailHeight, Math.Max(0, y));
                            xx = Math.Min(backup.detailWidth, Math.Max(0, xx));
                            yy = Math.Min(backup.detailHeight, Math.Max(0, yy));

                            if (xx - x > 0 && yy - y > 0)
                            {
                                terrains[i].terrainData.SetDetailLayer(x, y, n, backup.GetDetailLayer(x, y, n, xx - x, yy - y, terrains[i].terrainData));
                            }
                        }
                    }

                    if (restoreTrees)
                    {
                        var treeInstances = new List<TreeInstance>();

                        foreach (var tree in terrains[i].terrainData.treeInstances)
                        {
                            if (tree.position.x < rect.x || tree.position.x > rect.xMax || tree.position.z < rect.y || tree.position.z > rect.yMax)
                            {
                                treeInstances.Add(tree);
                            }
                        }

                        int[] treeRemap = new int[backup.treePrototypes.Length];
                        string[] treePrototypes = new string[terrains[i].terrainData.treePrototypes.Length];

                        for (int n = 0; n < terrains[i].terrainData.treePrototypes.Length; n++)
                        {
                            if (terrains[i].terrainData.treePrototypes[n].prefab != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(terrains[i].terrainData.treePrototypes[n].prefab, out string prefabGuid, out long _))
                            {
                                treePrototypes[n] = prefabGuid;
                            }
                        }

                        for (int n = 0; n < backup.treePrototypes.Length; n++)
                        {
                            treeRemap[n] = treePrototypes.IndexOf(x => x == backup.treePrototypes[n]);
                        }

                        foreach (var tree in backup.treeInstances)
                        {
                            if (tree.position.x >= rect.x && tree.position.x <= rect.xMax && tree.position.z >= rect.y && tree.position.z <= rect.yMax &&
                                treeRemap[tree.prototypeIndex] != -1)
                            {
                                treeInstances.Add(new TreeInstance()
                                {
                                    color = tree.color,
                                    heightScale = tree.heightScale,
                                    lightmapColor = tree.lightmapColor,
                                    position = tree.position,
                                    prototypeIndex = treeRemap[tree.prototypeIndex],
                                    rotation = tree.rotation,
                                    widthScale = tree.widthScale
                                });
                            }
                        }

                        terrains[i].terrainData.treeInstances = treeInstances.ToArray();
                    }
                }

                for (int i = 0; i < terrains.Length; i++)
                {
                    terrains[i].GetComponent<TerrainCollider>().enabled = true;
                }
            }
        }

        public void LoadBackup(string id)
        {
            if (activeTerrain == null)
            {
                string baseScenePath = EditorSceneManager.GetActiveScene().path;
                string baseSceneDirectory = Path.GetDirectoryName(baseScenePath);
                string sceneName = Path.GetFileNameWithoutExtension(baseScenePath);

                var restored = new GuiAllTerrainData();

                using (var fs = new FileStream($"{baseSceneDirectory}/terrain-backup-{sceneName}-{id}.dat", FileMode.Open))
                using (var ms = new MemoryStream())
                using (var br = new BinaryReader(ms))
                {
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    restored.Load(br);
                }

                activeTerrain = restored;
            }
        }

        public void LoadBackupIds()
        {
            string baseScenePath = EditorSceneManager.GetActiveScene().path;
            string baseSceneDirectory = Path.GetDirectoryName(baseScenePath);
            string sceneName = Path.GetFileNameWithoutExtension(baseScenePath);

            var backupFiles = Directory.GetFiles(baseSceneDirectory, $"terrain-backup-{sceneName}-*.dat").OrderByDescending(x => x);

            var prefixLength = $"terrain-backup-{sceneName}-".Length;

            backups.Clear();

            foreach (var backupFile in backupFiles)
            {
                backups.Add(Path.GetFileNameWithoutExtension(backupFile).Substring(prefixLength));
            }
        }

        public void SetPainting(bool painting)
        {
            if (painting && selectedBackup >= 0 && selectedBackup < backups.Count)
            {
                LoadBackup(backups[selectedBackup]);
            }
            else if (!painting)
            {
                activeTerrain = null;
                GC.Collect();
            }
        }

        private void OnDrawGizmos()
        {
            if (_mouseWorld != null && Selection.activeGameObject == gameObject)
            {
                float step = brushSize / 10f;

                Gizmos.color = Color.red;

                for (int x = -(_brushIndicatorSteps / 2); x < (_brushIndicatorSteps / 2); x++)
                {
                    for (int y = -(_brushIndicatorSteps / 2); y < (_brushIndicatorSteps / 2); y++)
                    {
                        var target = _mouseWorld.Value + new Vector3(x * step, 0f, y * step);

                        foreach (var terrain in terrains)
                        {
                            if (target.x >= terrain.transform.position.x && target.x <= terrain.transform.position.x + terrain.terrainData.size.x &&
                                target.z >= terrain.transform.position.z && target.z <= terrain.transform.position.z + terrain.terrainData.size.z)
                            {
                                float terrainHeight = terrain.SampleHeight(target);

                                Vector3 terrainPoint = new Vector3(target.x, terrain.transform.position.y + terrainHeight, target.z);

                                Gizmos.DrawSphere(terrainPoint, step / 3f);
                            }
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (!Application.isEditor)
            {
                Destroy(this);
            }

            LoadBackupIds();

            SceneView.duringSceneGui += OnScene;
        }

        private void OnScene(SceneView scene)
        {
            Event e = Event.current;

            _mouseWorld = null;

            if (CanRestorePaint())
            {
                Vector3 mousePos = e.mousePosition;
                float ppp = EditorGUIUtility.pixelsPerPoint;
                mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
                mousePos.x *= ppp;

                Ray ray = scene.camera.ScreenPointToRay(mousePos);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    _mouseWorld = hit.point;

                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        _mouseIsDown = true;
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        _mouseIsDown = false;
                        e.Use();
                    }

                    if (_mouseIsDown && EditorApplication.timeSinceStartup - _lastRestoreTime > _updateRate)
                    {
                        RestoreTerrain(new Rect(hit.point.x - brushSize / 2, hit.point.z - brushSize / 2, brushSize, brushSize));

                        _lastRestoreTime = EditorApplication.timeSinceStartup;
                    }
                }

                SceneView.RepaintAll();
            }
        }

        public class GuiDetailLayer
        {
            public string prototypeTextureGuid;
            public string prototypeGuid;
            public int[,] data;

            public void Save(BinaryWriter bw)
            {
                bw.Write(prototypeTextureGuid ?? "");
                bw.Write(prototypeGuid ?? "");

                for (int y = 0; y < data.GetLength(0); y++)
                {
                    for (int x = 0; x < data.GetLength(1); x++)
                    {
                        bw.Write(data[y, x]);
                    }
                }
            }

            public void Load(BinaryReader br)
            {
                prototypeTextureGuid = br.ReadString();
                prototypeGuid = br.ReadString();

                for (int y = 0; y < data.GetLength(0); y++)
                {
                    for (int x = 0; x < data.GetLength(1); x++)
                    {
                        data[y, x] = br.ReadInt32();
                    }
                }
            }
        }


        public class GuiTreeInstance
        {
            public Vector3 position;
            public float widthScale;
            public float heightScale;
            public float rotation;
            public Color32 color;
            public Color32 lightmapColor;
            public int prototypeIndex;

            public void Save(BinaryWriter bw)
            {
                bw.Write(position.x);
                bw.Write(position.y);
                bw.Write(position.z);
                bw.Write(widthScale);
                bw.Write(heightScale);
                bw.Write(rotation);
                bw.Write(color.a);
                bw.Write(color.r);
                bw.Write(color.g);
                bw.Write(color.b);
                bw.Write(lightmapColor.a);
                bw.Write(lightmapColor.r);
                bw.Write(lightmapColor.g);
                bw.Write(lightmapColor.b);
                bw.Write(prototypeIndex);
            }

            public void Load(BinaryReader br)
            {
                position.x = br.ReadSingle();
                position.y = br.ReadSingle();
                position.z = br.ReadSingle();
                widthScale = br.ReadSingle();
                heightScale = br.ReadSingle();
                rotation = br.ReadSingle();
                color.a = br.ReadByte();
                color.r = br.ReadByte();
                color.g = br.ReadByte();
                color.b = br.ReadByte();
                lightmapColor.a = br.ReadByte();
                lightmapColor.r = br.ReadByte();
                lightmapColor.g = br.ReadByte();
                lightmapColor.b = br.ReadByte();
                prototypeIndex = br.ReadInt32();
            }
        }

        public class GuiTerrainData
        {
            public int detailHeight;
            public int detailWidth;
            public int alphamapHeight;
            public int alphamapWidth;
            public int heightmapResolution;
            public float[,,] alphaMaps;
            public float[,] heights;
            public GuiDetailLayer[] detailLayers;
            public GuiTreeInstance[] treeInstances;
            public string[] treePrototypes;
            public string[] splatPrototypes;

            public int[,] GetDetailLayer(int x, int y, int n, int width, int height, TerrainData remapTo)
            {
                int remapIndex = -1;

                if (remapTo.detailPrototypes[n].prototype != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(remapTo.detailPrototypes[n].prototype, out string prototypeGuid, out long _))
                {
                    remapIndex = detailLayers.IndexOf(x => x.prototypeGuid == prototypeGuid);
                }

                if (remapIndex == -1 && remapTo.detailPrototypes[n].prototypeTexture != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(remapTo.detailPrototypes[n].prototypeTexture, out string prototypeTextureGuid, out long _))
                {
                    remapIndex = detailLayers.IndexOf(x => x.prototypeTextureGuid == prototypeTextureGuid);
                }

                var result = new int[height, width];

                if (remapIndex != -1)
                {
                    for (int yy = 0; yy < height; yy++)
                    {
                        for (int xx = 0; xx < width; xx++)
                        {
                            if (xx + x >= 0 && xx + x < detailLayers[remapIndex].data.GetLength(1) &&
                                yy + y >= 0 && yy + y < detailLayers[remapIndex].data.GetLength(0))
                            {
                                result[yy, xx] = detailLayers[remapIndex].data[yy + y, xx + x];
                            }
                        }
                    }
                }

                return result;
            }

            public float[,,] GetAlphamaps(int x, int y, int width, int height, TerrainData remapTo)
            {
                var result = new float[height, width, remapTo.splatPrototypes.Length];

                for (int zz = 0; zz < remapTo.splatPrototypes.Length; zz++)
                {
                    if (remapTo.splatPrototypes[zz].texture != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(remapTo.splatPrototypes[zz].texture, out string textureGuid, out long _))
                    {
                        int backupIndex = splatPrototypes.IndexOf(x => x == textureGuid);

                        if (backupIndex != -1)
                        {
                            for (int yy = 0; yy < height; yy++)
                            {
                                for (int xx = 0; xx < width; xx++)
                                {
                                    if (yy + y >= 0 && yy + y < alphaMaps.GetLength(0) &&
                                        xx + x >= 0 && xx + x < alphaMaps.GetLength(1))
                                    {
                                        result[yy, xx, zz] = alphaMaps[yy + y, xx + x, backupIndex];
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }

            public float[,] GetHeights(int x, int y, int width, int height)
            {
                var result = new float[height, width];

                for (int yy = 0; yy < height; yy++)
                {
                    for (int xx = 0; xx < width; xx++)
                    {
                        if (yy + y >= 0 && yy + y < heights.GetLength(1) &&
                            xx + x >= 0 && xx + x < heights.GetLength(0))
                        {
                            result[yy, xx] = heights[yy + y, xx + x];
                        }
                    }
                }

                return result;
            }

            public void Save(BinaryWriter bw)
            {
                bw.Write(treePrototypes.Length);
                foreach (var treePrototype in treePrototypes)
                {
                    bw.Write(treePrototype ?? "");
                }

                bw.Write(splatPrototypes.Length);
                foreach (var splatPrototype in splatPrototypes)
                {
                    bw.Write(splatPrototype ?? "");
                }

                bw.Write(alphamapHeight);
                bw.Write(alphamapWidth);
                bw.Write(alphaMaps.GetLength(2));
                for (int y = 0; y < alphamapHeight; y++)
                {
                    for (int x = 0; x < alphamapWidth; x++)
                    {
                        for (int z = 0; z < alphaMaps.GetLength(2); z++)
                        {
                            bw.Write(alphaMaps[y, x, z]);
                        }
                    }
                }

                bw.Write(heightmapResolution);
                for (int y = 0; y < heightmapResolution; y++)
                {
                    for (int x = 0; x < heightmapResolution; x++)
                    {
                        bw.Write(heights[y, x]);
                    }
                }

                bw.Write(detailHeight);
                bw.Write(detailWidth);
                bw.Write(detailLayers.Length);
                foreach (var detailLayer in detailLayers)
                {
                    detailLayer.Save(bw);
                }

                bw.Write(treeInstances.Length);
                foreach (var treeInstance in treeInstances)
                {
                    treeInstance.Save(bw);
                }
            }

            public void Load(BinaryReader br)
            {
                int treePrototypesCount = br.ReadInt32();
                treePrototypes = new string[treePrototypesCount];
                for (int i = 0; i < treePrototypesCount; i++)
                {
                    treePrototypes[i] = br.ReadString();
                }

                int splatPrototypesCount = br.ReadInt32();
                splatPrototypes = new string[splatPrototypesCount];
                for (int i = 0; i < splatPrototypesCount; i++)
                {
                    splatPrototypes[i] = br.ReadString();
                }

                alphamapHeight = br.ReadInt32();
                alphamapWidth = br.ReadInt32();
                int alphamapZ = br.ReadInt32();
                alphaMaps = new float[alphamapHeight, alphamapWidth, alphamapZ];
                for (int y = 0; y < alphamapHeight; y++)
                {
                    for (int x = 0; x < alphamapWidth; x++)
                    {
                        for (int z = 0; z < alphaMaps.GetLength(2); z++)
                        {
                            alphaMaps[y, x, z] = br.ReadSingle();
                        }
                    }
                }

                heightmapResolution = br.ReadInt32();
                heights = new float[heightmapResolution, heightmapResolution];
                for (int y = 0; y < heightmapResolution; y++)
                {
                    for (int x = 0; x < heightmapResolution; x++)
                    {
                        heights[y, x] = br.ReadSingle();
                    }
                }

                detailHeight = br.ReadInt32();
                detailWidth = br.ReadInt32();
                detailLayers = new GuiDetailLayer[br.ReadInt32()];
                for (int n = 0; n < detailLayers.Length; n++)
                {
                    detailLayers[n] = new GuiDetailLayer();
                    detailLayers[n].data = new int[detailHeight, detailWidth];
                    detailLayers[n].Load(br);
                }

                treeInstances = new GuiTreeInstance[br.ReadInt32()];
                for (int n = 0; n < treeInstances.Length; n++)
                {
                    treeInstances[n] = new GuiTreeInstance();
                    treeInstances[n].Load(br);
                }
            }
        }

        public class GuiAllTerrainData
        {
            public GuiTerrainData[] terrains;

            public void Save(BinaryWriter bw)
            {
                bw.Write(terrains.Length);
                foreach (var terrain in terrains)
                {
                    terrain.Save(bw);
                }
            }

            public void Load(BinaryReader br)
            {
                terrains = new GuiTerrainData[br.ReadInt32()];
                for (int i = 0; i < terrains.Length; i++)
                {
                    terrains[i] = new GuiTerrainData();
                    terrains[i].Load(br);
                }
            }
        }
    }
}