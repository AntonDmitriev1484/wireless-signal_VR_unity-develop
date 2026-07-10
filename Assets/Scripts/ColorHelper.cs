using System.IO;
using UnityEngine;

public class ColorHelper : MonoBehaviour
{
    // define const value for the palette number of colors
    public const int PALETTE_COLOR_COUNT = 255;

    [Header("Game Obj Reference")]
    public GameObject colorTestGameObj; // just use this for local test

    [Header("Palette Settings")]
    [SerializeField] private string paletteFileName = ""; //set this in inspector
    [SerializeField] private int currentColorIndex = 0;
    [Range(0, PALETTE_COLOR_COUNT-1)]
    [SerializeField] private int previewColorIndex = 0;

    private Texture2D paletteTexture;
    private bool isPaletteInitialized = false;


    //== TEST ONLY ================= BEGIN
    // This is for selftesting as this class is made to be used as a helper class, not a MonoBehaviour
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializePalette();
        if (isPaletteInitialized && colorTestGameObj != null)
        {
            int colorIdx = currentColorIndex;
            SetObjColorByIndex(colorTestGameObj, colorIdx);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // increase the color index by 1 every frame, and wrap around to 0 when it reaches PALETTE_COLOR_COUNT ant set color to colorTestGameObj with SetObjColorByIndex
        currentColorIndex = (currentColorIndex + 1) % PALETTE_COLOR_COUNT;
        // display the current color index in the console
        //Debug.Log($"Current color index: {currentColorIndex}");

        // Set the color of the colorTestGameObj to the current color index from the palette
        if (paletteTexture != null)
        {
            SetObjColorByIndex(colorTestGameObj, currentColorIndex);
        }
        else
        {
            Debug.LogWarning("Palette texture is not initialized, cannot set color.");
        }


    }
    //== TEST ONLY ================= END

    public void InitializePalette()
    {
        if (isPaletteInitialized)
            return;

        Debug.Log($"Palette file name: {paletteFileName}");

        try
        {
            // Load palette file from Resources folder
            TextAsset paletteAsset = Resources.Load<TextAsset>(paletteFileName);

            if (paletteAsset == null)
            {
                Debug.LogError($"Palette file not found in Resources folder: {paletteFileName}");
                Debug.LogError("Make sure to place your palette file in Assets/Resources/ folder");
                return;
            }

            string paletteFile = paletteAsset.text;

            if (string.IsNullOrEmpty(paletteFile))
            {
                Debug.LogError($"Palette file is empty: {paletteFileName}");
                return;
            }

            Debug.Log($"Palette file content length: {paletteFile.Length}");

            // Create a texture for the palette
            paletteTexture = new Texture2D(PALETTE_COLOR_COUNT, 1, TextureFormat.RGBA32, false);
            paletteTexture.filterMode = FilterMode.Point;
            paletteTexture.wrapMode = TextureWrapMode.Clamp;

            LoadPaletteData(paletteFile);
            isPaletteInitialized = true;

            Debug.Log("Palette loaded successfully from Resources!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing palette: {e.Message}");
        }
    }

    void LoadPaletteData(string fileContent)
    {
        string[] lines = fileContent.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        Color[] colors = new Color[PALETTE_COLOR_COUNT];

        for (int i = 0; i < PALETTE_COLOR_COUNT; i++)
        {
            if (i < lines.Length)
            {
                string[] values = lines[i].Trim().Split(new char[] { ' ', '\t' },
                    System.StringSplitOptions.RemoveEmptyEntries);

                if (values.Length >= 4)
                {
                    if (float.TryParse(values[0], out float r) &&
                        float.TryParse(values[1], out float g) &&
                        float.TryParse(values[2], out float b) &&
                        float.TryParse(values[3], out float a))
                    {
                        // JK_TODO - should get A values in the color file
                        // remove this line after correcting A values in the color file
                        colors[i] = new Color(r, g, b, 1.0f);

                        //colors[i] = new Color(r, g, b, a);
                        //Debug.Log($"Col index {i} : R({r}) G({g}) B({b}) A({a}) loaded");

                    }
                    else
                    {
                        colors[i] = Color.white; // Parse error indicator
                        Debug.LogError($"Err: Color index {i} is not correct!");

                    }
                }
                else
                {
                    colors[i] = Color.white; // Format error indicator
                    Debug.LogError($"Err: Color index {i} is not correct!");

                }
            }
            else
            {
                colors[i] = Color.black; // Default for missing entries
                Debug.LogError($"Err: Color index {i} is missing!");

            }
        }

        paletteTexture.SetPixels(colors);
        paletteTexture.Apply();
    }

    public int GetPaletteColorCount()
    {
        return PALETTE_COLOR_COUNT;
    }

    // Public method to get a color from the palette by index
    public Color GetPaletteColor(int index)
    {
        if (!isPaletteInitialized)
        {
            InitializePalette();
        }

        if (paletteTexture == null)
        {
            Debug.LogWarning("Palette texture is not loaded!");
            return Color.white;
        }

        // Clamp index to valid range
        index = Mathf.Clamp(index, 0, PALETTE_COLOR_COUNT-1);

        // Get color from the palette texture
        return paletteTexture.GetPixel(index, 0);
    }

    public void SetSpecificColor(int index)
    {
        currentColorIndex = Mathf.Clamp(index, 0, PALETTE_COLOR_COUNT - 1);
        previewColorIndex = currentColorIndex;

        if (colorTestGameObj != null)
        {
            SetObjColorByIndex(colorTestGameObj, currentColorIndex);
        }
    }

    public Color GetCurrentColor()
    {
        return GetPaletteColor(currentColorIndex);
    }

    public int GetCurrentColorIndex()
    {
        return currentColorIndex;
    }

    // Example using color setting in a material
    private void SetObjColorByIndex(GameObject obj, int colorIndex)
    {
        if (obj == null)
        {
            Debug.LogWarning("GameObject reference is null!");
            return;
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("Obj renderer is not available!");
            return;
        }

        Color paletteColor = GetPaletteColor(colorIndex);
        renderer.material.color = paletteColor;


        //Debug.Log($"Set Object color to palette index {colorIndex}: {paletteColor}");
    }

}
