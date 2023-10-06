using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.VisualScripting;

[System.Serializable]
public class MapFile
{
    public float[,] heights;
    public float[,,] splatmaps;
}
public sealed class TerrainTool : MonoBehaviour
{
    public enum TerrainModificationAction
    {
        Smooth,
        Flatten,
        PaintOnly
    }
    public int currentPaintLayer;
    public int currentFloor;
    public int brushWidth;

    private int brushWidthSmooth=6;

    private bool overUI = false;

    [Range(0.001f, 0.1f)]
    public float strength;

    public TerrainModificationAction modificationAction;
    public Terrain _targetTerrain;
    private TerrainData terrainData;
    public RectTransform loadMapBtnParent;
    public GameObject prefabSelectableLoadMapBtn;
    public List<GameObject> loadMapBtnList;
    private int alphamapResolution = 0;
    public string currentMapName;
    private void Start()
    {
        // Get the TerrainData and heightmap resolution
        terrainData = _targetTerrain.terrainData;
        alphamapResolution = terrainData.alphamapResolution;
        modificationAction = TerrainModificationAction.Flatten;
        CreateNewMap();
    }
    private void Update()
    {
        if (overUI == true) return;
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
            {
                if (hit.transform.TryGetComponent(out Terrain terrain)) _targetTerrain = terrain;

                switch (modificationAction)
                {
                    case TerrainModificationAction.Smooth:

                        //LowerTerrain(hit.point, strength);
                        SmoothTerrain(hit.point, brushWidth, brushWidth, currentPaintLayer);
                        break;

                    case TerrainModificationAction.Flatten:

                        FlattenTerrain(hit.point, currentFloor, brushWidth, brushWidth, currentPaintLayer);

                        break;
                    case TerrainModificationAction.PaintOnly:

                        PaintTerrain(hit.point, currentFloor, brushWidth, brushWidth, currentPaintLayer);

                        break;
                }
            }
        }
    }

    public void OverUI(bool over)
    {
        overUI = over;
    }
    public void CreateNewMap()
    {
        overUI = false;
        PaintAllTerrainWithLayer(3);
        FlattenAllMap();
    }
    public void SaveMap()
    {
        var heights = _targetTerrain.terrainData.GetHeights(0,0, _targetTerrain.terrainData.heightmapResolution, _targetTerrain.terrainData.heightmapResolution);
        int splatmapResolution = _targetTerrain.terrainData.alphamapResolution;
        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, splatmapResolution, splatmapResolution);
       
        MapFile newMapFile = new MapFile()
        {
            heights = heights,
            splatmaps = splatmapData
        };

        string json = JsonConvert.SerializeObject(newMapFile);
        string saveFileName = "/" + currentMapName + ".rtsmapeditor";
        string folderName = "/Resources/Maps";
        string savePath = Application.persistentDataPath + folderName;
        string savePathName = savePath + saveFileName;
        var folder = Directory.CreateDirectory(savePath); // returns a DirectoryInfo object
        Debug.Log("saving terrain data to " + savePathName);
        File.WriteAllText(savePathName, json);

    }
    public void ChangeSavedMapName(TMP_InputField inputfield)
    {
        currentMapName = inputfield.text;
    }
    public void OpenMapListToLoad()
    {
        for(int i = 0; i < loadMapBtnList.Count;i++)
        {
            Destroy(loadMapBtnList[i]);
        }
        loadMapBtnList.Clear();
        string folderName = "/Resources/Maps";
        string savePath = Application.persistentDataPath + folderName;
        var dir = new DirectoryInfo(savePath);
        FileInfo[] FilesAS = dir.GetFiles("*.rtsmapeditor"); //Getting Text files
        foreach (FileInfo fileS in FilesAS)
        {
            string loadSignature = folderName + "/" + fileS.Name;
            GameObject o = Instantiate(prefabSelectableLoadMapBtn, loadMapBtnParent);
            o.SetActive(true);
            o.GetComponentInChildren<TMP_Text>().text = fileS.Name;
            loadMapBtnList.Add(o);
            Debug.Log(string.Format("Map file was found in directory with name {0}",fileS.Name));
            Debug.Log(loadSignature);
        }
    }
    public void SelectLoadMap(TMP_Text textName)
    {
        currentMapName = textName.text;
    }
    public void LoadMap()
    {
        string folderName = "/Resources/Maps";
        string loadPathFolder = Application.persistentDataPath + folderName;
        if (Directory.Exists(loadPathFolder))
        {
            Debug.Log("Folder exist, seems file are extracted");
        }
        else
        {
            Debug.Log("Folder do not exist");
        }
        string saveFileName = "/" + currentMapName;
        string loadPath = loadPathFolder + saveFileName;
        if (File.Exists(loadPath))
        {
            string json = File.ReadAllText(loadPath);
            //Debug.Log(json);
            MapFile loadedMap = JsonConvert.DeserializeObject<MapFile>(json);
            Debug.Log(json);
            Debug.Log("loaded map data from " + loadPath);

            _targetTerrain.terrainData.SetHeights(0, 0, loadedMap.heights);
            _targetTerrain.terrainData.SetAlphamaps(0, 0, loadedMap.splatmaps);
        }
    }
    public void OpenSavesMapFolder()
    {
        string folderName = "/Resources/Maps";
        string savePath = Application.persistentDataPath + folderName;
        string itemPath = savePath.Replace(@"/", @"\");   // explorer doesn't like front slashes
        System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
    }
    private void FlattenTerrain(Vector3 worldPosition, int floor, int brushWidth, int brushHeight, int layerPaint)
    {
        if (overUI == true) return;
        float height = 0;
        if (floor == 5)
        {
            height = 0.004f;
        }
        else if (floor == 4)
        {
            height = 0.003f;
        }
        else if (floor == 3)
        {
            height = 0.002f;
        }
        else if (floor == 2)//sand
        {
            height = 0.0015f;
        }
        else if (floor == 1)//water
        {
            height = 0.001f;
        }
        else if (floor == 0)//ocean
        {
            height = 0f;
        }
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);
        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] = height;
            }
        }
        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }
    private void PaintTerrain(Vector3 worldPosition, int floor, int brushWidth, int brushHeight, int layerPaint)
    {
        if (overUI == true) return;

        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        int splatmapResolution = alphamapResolution;
        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, splatmapResolution, splatmapResolution);
        int alphaMapLayers = terrainData.alphamapLayers;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                for (int i = 0; i < alphaMapLayers; i++)
                {
                    // Is Cliff layer
                    if (i == layerPaint)
                    {
                        splatmapData[brushPosition.y, brushPosition.x, i] = 1;
                        splatmapData[brushPosition.y+y, brushPosition.x+x, i] = 1;
                        splatmapData[brushPosition.y-y, brushPosition.x-x, i] = 1;
                    }
                    else
                    {
                        splatmapData[brushPosition.y, brushPosition.x, i] = 0f;
                        splatmapData[brushPosition.y + y, brushPosition.x + x, i] = 0;
                        splatmapData[brushPosition.y - y, brushPosition.x - x, i] = 0;
                    }
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    public void SmoothTerrain(Vector3 worldPosition, int brushWidth, int brushHeight, int layerPaint)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        float smoothingIntensity = 0.25f;
        TerrainData terrainData = _targetTerrain.terrainData;
        int heightmapWidth = brushSize.x;
        int heightmapHeight = brushSize.y;
        float[,] heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (int x = 0; x < heightmapWidth; x++)
        {
            for (int z = 0; z < heightmapHeight; z++)
            {
                float averageHeight = 0f;
                int sampleCount = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int neighborX = x + i;
                        int neighborZ = z + j;

                        if (neighborX >= 0 && neighborX < heightmapWidth && neighborZ >= 0 && neighborZ < heightmapHeight)
                        {
                            averageHeight += heights[neighborX, neighborZ];
                            sampleCount++;
                        }
                    }
                }

                averageHeight /= sampleCount;
                heights[x, z] = Mathf.Lerp(heights[x, z], averageHeight, smoothingIntensity);
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }
    public void SmoothAllTerrain()
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        for (int x = 0; x < heightmapWidth; x++)
        {
            for (int z = 0; z < heightmapHeight; z++)
            {
                float averageHeight = 0f;
                int sampleCount = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int neighborX = x + i;
                        int neighborZ = z + j;

                        if (neighborX >= 0 && neighborX < heightmapWidth && neighborZ >= 0 && neighborZ < heightmapHeight)
                        {
                            averageHeight += heights[neighborX, neighborZ];
                            sampleCount++;
                        }
                    }
                }

                averageHeight /= sampleCount;
                heights[x, z] = Mathf.Lerp(heights[x, z], averageHeight, 0.50f);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
    private void FlattenAllMap()
    {
        var terrainData = GetTerrainData();

        int splatmapResolution = terrainData.heightmapResolution;
        float[,] splatmapData = terrainData.GetHeights(0, 0, splatmapResolution, splatmapResolution);

        for (var y = 0; y < splatmapData.GetLength(0); y++)
        {
            for (var x = 0; x < splatmapData.GetLength(1); x++)
            {
                splatmapData[y, x] = 0;

            }
        }

        terrainData.SetHeights(0, 0, splatmapData);
    }
    private void PaintAllTerrainWithLayer(int layerPaint)
    {
        int splatmapResolution = alphamapResolution;
        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, splatmapResolution, splatmapResolution);
        int alphaMapLayers = terrainData.alphamapLayers;
        for (int y = 0; y < splatmapData.GetLength(0); y++)
        {
            for (int x = 0; x < splatmapData.GetLength(1); x++)
            {
                for (int i = 0; i < alphaMapLayers; i++)
                {
                    // Is Cliff layer
                    if (i == layerPaint)
                    {
                        splatmapData[y, x, i] = 1.0f;
                    }
                    else
                    {
                        splatmapData[y, x, i] = 0f;
                    }
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    public void ChangePaintLayer(int layernum)
    {
        currentPaintLayer = layernum;
    }
    public void ChangeFloor(TMP_Dropdown dropdown)
    {
        currentFloor = dropdown.value;
    }
    public void ChangeBetweenSmoothAndFlatten(TMP_Dropdown dropdown)
    {
        int value = dropdown.value;
        if (value == 0)
        {
            modificationAction = TerrainModificationAction.Flatten;
        }
        else if (value == 1)
        {
            modificationAction = TerrainModificationAction.Smooth;
        }
        else if (value == 2)
        {
            modificationAction = TerrainModificationAction.PaintOnly;
        }
    }
    public void ChangeBrushSize(TMP_Dropdown dropdown)
    {
        int value = dropdown.value;
        if (value == 0)
        {
            brushWidth = 2;
        }
        else if (value == 1)
        {
            brushWidth = 4;
        }
        else if (value == 2)
        {
            brushWidth = 6;
        }
        else if (value == 3)
        {
            brushWidth = 8;
        }
        else if (value == 4)
        {
            brushWidth = 10;
        }
        else if (value == 5)
        {
            brushWidth = 12;
        }
        else if (value == 6)
        {
            brushWidth = 14;
        }
    }
    private TerrainData GetTerrainData() => _targetTerrain.terrainData;

    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;

    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - _targetTerrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }

    public Vector2Int GetBrushPosition(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        var heightmapResolution = GetHeightmapResolution();

        return new Vector2Int((int)Mathf.Clamp(terrainPosition.x - brushWidth / 2.0f, 0.0f, heightmapResolution), (int)Mathf.Clamp(terrainPosition.z - brushHeight / 2.0f, 0.0f, heightmapResolution));
    }

    public Vector2Int GetSafeBrushSize(int brushX, int brushY, int brushWidth, int brushHeight)
    {
        var heightmapResolution = GetHeightmapResolution();

        while (heightmapResolution - (brushX + brushWidth) < 0) brushWidth--;

        while (heightmapResolution - (brushY + brushHeight) < 0) brushHeight--;

        return new Vector2Int(brushWidth, brushHeight);
    }
    public void ManageDetail()
    {
        /*
        // read all detail layers into a 3D int array:
        int numDetails = terrainData.detailPrototypes.Length;
        int[,,] detailMapData = new int[terrainData.detailWidth, terrainData.detailHeight, numDetails];
        for (int layerNum = 0; layerNum < numDetails; layerNum++)
        {
            int[,] detailLayer = terrainData.GetDetailLayer(int x, int y, int width, int height, int layerNum);
        }

        // write all detail data to terrain data:
        for (int n = 0; n < detailMapData.Length; n++)
        {
            terrainData.SetDetailLayer(0, 0, n, detailMapData[n]);
        }*/

    }
}