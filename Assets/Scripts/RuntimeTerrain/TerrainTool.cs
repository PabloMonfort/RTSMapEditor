using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
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
        Raise,
        Lower,
        Flatten
    }
    public int currentPaintLayer;
    public int currentFloor;
    public int brushWidth;
    public int brushHeight;
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
        OpenMapListToLoad();
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
                    case TerrainModificationAction.Raise:

                        RaiseTerrain(hit.point, strength, brushWidth, brushHeight);

                        break;

                    case TerrainModificationAction.Lower:

                        LowerTerrain(hit.point, strength);

                        break;

                    case TerrainModificationAction.Flatten:

                        FlattenTerrain(hit.point, currentFloor, brushWidth, brushHeight, currentPaintLayer);

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
        PaintAllTerrainWithLayer(0);
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
    public void RaiseTerrain(Vector3 worldPosition, float strength, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] += strength * Time.deltaTime;
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }

    private void LowerTerrain(Vector3 worldPosition, float strength)
    {
        if (overUI == true) return;
        var brushPosition = GetBrushPosition(worldPosition, brushWidthSmooth, brushWidthSmooth);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidthSmooth, brushWidthSmooth);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] -= strength * Time.deltaTime;
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }

    private void FlattenTerrain(Vector3 worldPosition, int floor, int brushWidth, int brushHeight, int layerPaint)
    {
        if (overUI == true) return;
        float height = 0;
        if (floor == 0)
        {
            height = 0f;
        }
        else if (floor == 1)
        {
            height = 0.001f;
        }
        else if (floor == 2)
        {
            height = 0.002f;
        }
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);
        int splatmapResolution = alphamapResolution;
        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, splatmapResolution, splatmapResolution);
        int alphaMapLayers = terrainData.alphamapLayers;

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] = height;
                for (int i = 0; i < alphaMapLayers; i++)
                {
                    // Is Cliff layer
                    if (i == layerPaint)
                    {
                        splatmapData[brushPosition.y, brushPosition.x, i] = 1.0f; // Set to 1 to fully paint this layer
                        splatmapData[brushPosition.y + y, brushPosition.x + x, i] = 1.0f; // Set to 1 to fully paint this layer
                        splatmapData[brushPosition.y - y, brushPosition.x - x, i] = 1.0f; // Set to 1 to fully paint this layer
                    }
                    else
                    {
                        splatmapData[brushPosition.y, brushPosition.x, i] = 0f; // Set to 1 to fully paint this layer
                        splatmapData[brushPosition.y + y, brushPosition.x + x, i] = 0f; // Set to 1 to fully paint this layer
                        splatmapData[brushPosition.y - y, brushPosition.x - x, i] = 0f; // Set to 1 to fully paint this layer
                    }
                }
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
        terrainData.SetAlphamaps(0, 0, splatmapData);
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
            modificationAction = TerrainModificationAction.Lower;
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
}