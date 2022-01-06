using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner instance = null;

    public Block blockPrefab;

    public Animator filterPanelAnimator;
    private bool filterPanelShown = false;
    [Header("Filter options")]
    public Mesh[] cubeShapes;
    public Mesh[] sphereShapes;
    public Mesh[] otherShapes;
    private Mesh[] allShapes;
    public Gradient blueGradient;
    public Gradient greenGradient;
    public Gradient pinkGradient;
    public Gradient purpleGradient;
    public Gradient gradient;

    [Header("Filter button back images")]
    public Image blueButtonImage;
    public Image greenButtonImage;
    public Image pinkButtonImage;
    public Image purpleButtonImage;
    public Image cubeButtonImage;
    public Image sphereButtonImage;
    public Image otherButtonImage;

    public Transform blockSpawnPosition;
    public Transform blocksParent;
    public int firstRadius;
    public float radiusInterval;
    public int degreeInterval;
    public int blocksPerCircle;
    public int minBlocks;
    public int maxBlocks;
    public Transform grid;
    public GameObject gridPrefab;
    public GameObject currentGrid;
    public GameObject projectionPlane;
    List<GameObject> blocksInPalette;

    bool firstBlock = true;

    //Need to learn how to associate enums with objects
    public enum FilterType
    {
        BLUE, GREEN, PINK, PURPLE, CUBES, SPHERES, OTHERSHAPES
    }

    private List<FilterType> activeFilters;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        blocksInPalette = new List<GameObject>();
        activeFilters = new List<FilterType>();

        allShapes = new Mesh[cubeShapes.Length + sphereShapes.Length + otherShapes.Length];
        cubeShapes.CopyTo(allShapes, 0);
        sphereShapes.CopyTo(allShapes, cubeShapes.Length);
        otherShapes.CopyTo(allShapes, cubeShapes.Length + sphereShapes.Length);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) || blocksInPalette.Count == 0)
        {
            SpawnBlocks(1);
        }
        if (Input.GetKey(KeyCode.E))
        {
            SpawnBlocks(10);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            UploadCanvas();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            //Test json
            SaveCanvasToJSON();
        }
    }

    public async void UploadCanvas()
    {
        string canvasJson = SaveCanvasToJSON();
        await FirebaseDatabaseManager.instance.AddNewCanvas(canvasJson);
    }

    public void SpawnBlocks(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject block = Instantiate(blockPrefab.gameObject);
            block.transform.position = blockSpawnPosition.position;
            block.transform.parent = blocksParent;

            //Randomize shape and color
            if (!firstBlock)
            {
                //So ridiculously inefficient
                List<Mesh> filteredShapes = new List<Mesh>();
                if (activeFilters.Contains(FilterType.CUBES))
                {
                    foreach (Mesh m in cubeShapes)
                    {
                        filteredShapes.Add(m);
                    }
                }
                if (activeFilters.Contains(FilterType.SPHERES))
                {
                    foreach (Mesh m in sphereShapes)
                    {
                        filteredShapes.Add(m);
                    }
                }
                if (activeFilters.Contains(FilterType.OTHERSHAPES))
                {
                    foreach (Mesh m in otherShapes)
                    {
                        filteredShapes.Add(m);
                    }
                }

                List<Gradient> filteredGradients = new List<Gradient>();
                if (activeFilters.Contains(FilterType.BLUE))
                {
                    filteredGradients.Add(blueGradient);
                }
                if (activeFilters.Contains(FilterType.GREEN))
                {
                    filteredGradients.Add(greenGradient);
                }
                if (activeFilters.Contains(FilterType.PINK))
                {
                    filteredGradients.Add(pinkGradient);
                }
                if (activeFilters.Contains(FilterType.PURPLE))
                {
                    filteredGradients.Add(purpleGradient);
                }

                Mesh[] randomShapes;
                if (filteredShapes.Count == 0)
                {
                    randomShapes = allShapes;
                }
                else
                {
                    randomShapes = filteredShapes.ToArray();
                }
                int randomShapeIndex = UnityEngine.Random.Range(0, randomShapes.Length - 1);
                block.GetComponent<MeshFilter>().mesh = randomShapes[randomShapeIndex];
                block.GetComponent<Block>().meshName = randomShapes[randomShapeIndex].name;

                Material mat = block.GetComponent<MeshRenderer>().material = new Material(block.GetComponent<MeshRenderer>().material);
                Gradient randomGradient;
                if (filteredGradients.Count == 0)
                {
                    randomGradient = gradient;
                }
                else
                {
                    int randomIndex = UnityEngine.Random.Range(0, filteredGradients.Count);
                    randomGradient = filteredGradients[randomIndex];
                }
                Color c = randomGradient.Evaluate(UnityEngine.Random.Range(0f, 1f));
                mat.color = c;
                block.GetComponent<Block>().color = c;
            }
            else
            {
                firstBlock = false;
            }

            blocksInPalette.Add(block);
        }
        RepositionBlocks();
    }

    public void RepositionBlocks()
    {
        int cycles = Mathf.CeilToInt((float)blocksInPalette.Count / blocksPerCircle);
        for (int i = 0; i < cycles; i++)
        {
            int count = i == cycles - 1 ? (blocksInPalette.Count - i * blocksPerCircle) : blocksPerCircle;
            RepositionCycle(blocksInPalette.GetRange(i * blocksPerCircle, count), i);
        }
    }

    private void RepositionCycle(List<GameObject> blocks, int cycle)
    {
        int amount = blocks.Count;
        int degree = (amount - 1) * -(degreeInterval / 2);
        for (int i = 0; i < amount; i++, degree += degreeInterval)
        {
            float rad = degree * Mathf.Deg2Rad;
            blocks[i].GetComponent<Block>().SetLocalTarget(new Vector3(firstRadius * Mathf.Sin(rad), firstRadius * Mathf.Cos(rad) + radiusInterval * cycle));
        }
    }

    public void RemoveBlock(Block block)
    {
        blocksInPalette.Remove(block.gameObject);
        RepositionBlocks();
        SpawnBlocks(UnityEngine.Random.Range(minBlocks, maxBlocks));
        block.transform.parent = grid;
    }

    private FilterType StringToFilterEnum(string filterTypeString)
    {
        filterTypeString = filterTypeString.ToLower();
        FilterType filterType;
        switch (filterTypeString)
        {
            case "blue":
                filterType = FilterType.BLUE;
                break;
            case "green":
                filterType = FilterType.GREEN;
                break;
            case "pink":
                filterType = FilterType.PINK;
                break;
            case "purple":
                filterType = FilterType.PURPLE;
                break;
            case "cube":
                filterType = FilterType.CUBES;
                break;
            case "sphere":
                filterType = FilterType.SPHERES;
                break;
            case "other":
                filterType = FilterType.OTHERSHAPES;
                break;
            default:
                filterType = FilterType.OTHERSHAPES;
                break;
        }
        return filterType;
    }

    public void ShowFiltersButton()
    {
        filterPanelAnimator.SetTrigger(filterPanelShown ? "Exit" : "Enter");
        filterPanelShown = !filterPanelShown;
    }

    private void ChangeButtonColor(FilterType filterType, bool on)
    {
        Image buttonImage;
        switch (filterType)
        {
            case FilterType.BLUE:
                buttonImage = blueButtonImage;
                break;
            case FilterType.GREEN:
                buttonImage = greenButtonImage;
                break;
            case FilterType.PINK:
                buttonImage = pinkButtonImage;
                break;
            case FilterType.PURPLE:
                buttonImage = purpleButtonImage;
                break;
            case FilterType.CUBES:
                buttonImage = cubeButtonImage;
                break;
            case FilterType.SPHERES:
                buttonImage = sphereButtonImage;
                break;
            case FilterType.OTHERSHAPES:
                buttonImage = otherButtonImage;
                break;
            default:
                buttonImage = otherButtonImage;
                break;
        }
        float a = buttonImage.color.a;
        buttonImage.color = on ? Color.green : Color.white;
        buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, a);
    }

    public void AddFilter(string filterTypeString)
    {
        FilterType filterType = StringToFilterEnum(filterTypeString);
        if (activeFilters.Contains(filterType))
        {
            RemoveFilter(filterType);
        }
        else
        {
            activeFilters.Add(filterType);
            ChangeButtonColor(filterType, true);
            FilterOutBlocks();
        }

        //TODO Remove blocks that don't belong to this filter from the pallette
    }

    private void RemoveFilter(FilterType filterType)
    {
        activeFilters.Remove(filterType);
        ChangeButtonColor(filterType, false);
        if (activeFilters.Count > 0)
        {
            FilterOutBlocks(); 
        }
    }

    private void FilterOutBlocks()
    {
        List<GameObject> blocksToRemove = new List<GameObject>();
        foreach (GameObject g in blocksInPalette)
        {
            Block block = g.GetComponent<Block>();
            if (!BlockBelongs(block))
            {
                blocksToRemove.Add(block.gameObject);
                block.Delete();
            }
        }
        foreach (GameObject g in blocksToRemove)
        {
            blocksInPalette.Remove(g);
        }
        RepositionBlocks();
    }

    private bool BlockBelongs(Block block)
    {
        bool correctShape = false;
        bool filterContainsShape = false;
        bool correctColor = false;
        bool filterContainsColor = false;
        Mesh blockMesh = block.GetComponent<MeshFilter>().mesh;
        print(blockMesh);
        if (activeFilters.Contains(FilterType.CUBES))
        {
            filterContainsShape = true;
            foreach (Mesh m in cubeShapes)
            {
                if (blockMesh.name.Contains(m.name))
                {
                    correctShape = true;
                }
            }
        }
        if (activeFilters.Contains(FilterType.SPHERES))
        {
            filterContainsShape = true;
            foreach (Mesh m in sphereShapes)
            {
                if (blockMesh.name.Contains(m.name))
                {
                    correctShape = true;
                }
            }
        }
        if (activeFilters.Contains(FilterType.OTHERSHAPES))
        {
            filterContainsShape = true;
            foreach (Mesh m in otherShapes)
            {
                if (blockMesh.name.Contains(m.name))
                {
                    correctShape = true;
                }
            }
        }

        Color blockColor = block.GetComponent<MeshRenderer>().material.color;
        if (activeFilters.Contains(FilterType.BLUE))
        {
            filterContainsColor = true;
            if (ColorInGradient(blockColor, blueGradient))
            {
                correctColor = true;
            }
        }
        if (activeFilters.Contains(FilterType.GREEN))
        {
            filterContainsColor = true;
            if (ColorInGradient(blockColor, greenGradient))
            {
                correctColor = true;
            }
        }
        if (activeFilters.Contains(FilterType.PINK))
        {
            filterContainsColor = true;
            if (ColorInGradient(blockColor, pinkGradient))
            {
                correctColor = true;
            }
        }
        if (activeFilters.Contains(FilterType.PURPLE))
        {
            filterContainsColor = true;
            if (ColorInGradient(blockColor, purpleGradient))
            {
                correctColor = true;
            }
        }
        return (correctShape && correctColor) || (!filterContainsShape && correctColor) || (!filterContainsColor && correctShape);
    }

    private bool ColorInGradient(Color color, Gradient gradient)
    {
        float minr = Mathf.Min(gradient.colorKeys[0].color.r, gradient.colorKeys[1].color.r);
        float minb = Mathf.Min(gradient.colorKeys[0].color.b, gradient.colorKeys[1].color.b);
        float ming = Mathf.Min(gradient.colorKeys[0].color.g, gradient.colorKeys[1].color.g);
        float maxr = Mathf.Max(gradient.colorKeys[0].color.r, gradient.colorKeys[1].color.r);
        float maxb = Mathf.Max(gradient.colorKeys[0].color.b, gradient.colorKeys[1].color.b);
        float maxg = Mathf.Max(gradient.colorKeys[0].color.g, gradient.colorKeys[1].color.g);

        return color.r >= minr && color.r <= maxr
            && color.g >= ming && color.g <= maxg
            && color.b >= minb && color.b <= maxb;
    }

    public void TrashButton()
    {
        Block[] blocks = grid.GetComponentsInChildren<Block>();
        foreach (Block b in blocks)
        {
            b.Delete();
        }
        Destroy(currentGrid);
        currentGrid = Instantiate(gridPrefab, grid);
        currentGrid.transform.localPosition = Vector3.zero;
    }

    public string SaveCanvasToJSON()
    {
        CanvasData canvasData = new CanvasData();
        Block[] blocks = grid.GetComponentsInChildren<Block>();
        canvasData.blocks = new BlockData[blocks.Length];
        for (int i = 0; i < blocks.Length; i++)
        {
            Block b = blocks[i];
            BlockData block = new BlockData();
            block.meshName = b.meshName;
            block.r = b.color.r;
            block.g = b.color.g;
            block.b = b.color.b;
            block.x = b.transform.position.x;
            block.y = b.transform.position.y;
            block.z = b.transform.position.z;
            canvasData.blocks[i] = block;
        }
        string canvasJSON = JsonUtility.ToJson(canvasData);
        print("Json result: " + canvasJSON);
        return canvasJSON;
    }
    
}

[Serializable]
public class CanvasData
{
    public BlockData[] blocks;
}

[Serializable]
public class BlockData
{
    public string meshName;
    public float r;
    public float g;
    public float b;
    public float x;
    public float y;
    public float z;
}
