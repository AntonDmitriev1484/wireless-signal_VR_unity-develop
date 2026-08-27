using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField]
    private GameObject menuMain;

    [SerializeField]
    private GameObject menuQA;

    [SerializeField, Tooltip("Shared parent of the panels (the Menus canvas). Leave empty to use PanelMain's parent.")]
    private Transform menuRoot;

    [Header("Panel Positions")]
    [SerializeField, Tooltip("How far in front of the camera PanelMain is placed. Every other element " +
                             "keeps the position it has relative to PanelMain in the editor.")]
    private float mainMenuDistanceFromCamera = 2f;

    [SerializeField, Tooltip("Vertical nudge applied after placing the menu.")]
    private float menuVerticalOffset = 0f;

    [Header("Camera")]
    [SerializeField]
    private Camera playerCamera;

    [Header("UI Raycast")]
    [SerializeField]
    private GraphicRaycaster mainGraphicRaycaster;

    [SerializeField]
    private GraphicRaycaster qaGraphicRaycaster;

    [SerializeField]
    private EventSystem eventSystem;

    [Header("Input")]
    [SerializeField]
    private InputActionReference openMenuClick;

    [SerializeField]
    private InputActionReference selectItem;

    [Header("Raycast")]
    [SerializeField]
    private float raycastDistance = 100f;

    // Currently hovered button
    private Button hoveredButton;


    [Header("Backdrop")]
    [SerializeField, Tooltip("Optional. Leave empty to build a flat unlit white material at runtime. " +
                             "If you assign one, use an Unlit shader - a Lit material will be shaded by the scene lights.")]
    private Material backdropMaterial;

    // Clear gap between the panels and the front face of the slab.
    private const float BACKDROP_GAP = 0.01f;
    private const float BACKDROP_THICKNESS = 0.005f;
    private const float BACKDROP_PADDING = 0.02f;   // margin around the panels

    private GameObject backdrop;


    // ========================================================================
    // START
    // ========================================================================

    private void Start()
    {
        Remove_Menu_Ray();

        if (openMenuClick != null)
        {
            openMenuClick.action.Enable();
        }

        if (selectItem != null)
        {
            selectItem.action.Enable();
        }
    }


    // ========================================================================
    // UPDATE
    // ========================================================================

    private void Update()
    {
        MainMenuToggleByMouseClick();

        if (menuMain != null && menuMain.activeSelf)
        {
            CheckButtonUnderCenterOfView();
            SelectHoveredButton();
        }
    }


    // ========================================================================
    // CLEANUP
    // ========================================================================

    private void OnDestroy()
    {
        if (openMenuClick != null)
        {
            openMenuClick.action.Disable();
        }

        if (selectItem != null)
        {
            selectItem.action.Disable();
        }

        // The slab lives at the scene root, so it has to be cleaned up explicitly.
        if (backdrop != null)
        {
            Destroy(backdrop);
        }
    }


    // ========================================================================
    // MENU
    // ========================================================================

    public void Remove_Menu_Ray()
    {
        if (menuMain != null)
        {
            menuMain.SetActive(false);
        }

        if (menuQA != null)
        {
            menuQA.SetActive(false);
        }

        if (backdrop != null)
        {
            backdrop.SetActive(false);
        }

        hoveredButton = null;
    }


    public void Show_Menu_Ray()
    {
        if (menuMain == null)
            return;

        // ------------------------------------------------------------
        // Show both panels
        // ------------------------------------------------------------

        menuMain.SetActive(true);

        if (menuQA != null)
        {
            menuQA.SetActive(true);
        }


        // ------------------------------------------------------------
        // Move the whole group in front of the camera, as one rigid unit
        // ------------------------------------------------------------

        PositionMenuInFrontOfCamera();


        // ------------------------------------------------------------
        // Fit the backdrop slab around whatever is now on screen
        // ------------------------------------------------------------

        UpdateBackdrop();
    }


    // ========================================================================
    // BACKDROP SLAB
    // ========================================================================

    // A thin opaque slab sized to enclose the visible panels, sitting BACKDROP_GAP behind them.
    // Refitted on every open, because the panels are repositioned relative to the camera each time.
    private void UpdateBackdrop()
    {
        if (menuMain == null)
            return;

        // Both panels share PanelMain's rotation, so measure them in that frame. It is a
        // rotation-only transform, so distances are preserved and the result maps straight back.
        Quaternion rotation = menuMain.transform.rotation;
        Quaternion inverse = Quaternion.Inverse(rotation);

        bool anyCorner = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        Vector3[] corners = new Vector3[4];

        foreach (GameObject panel in new[] { menuMain, menuQA })
        {
            if (panel == null || !panel.activeInHierarchy)
                continue;

            // Include the panel's children (PanelHeatmap sits well below PanelMain's own rect), so
            // the slab really does bound everything on screen. Inactive elements are skipped.
            foreach (RectTransform rect in panel.GetComponentsInChildren<RectTransform>(false))
            {
                rect.GetWorldCorners(corners);

                foreach (Vector3 corner in corners)
                {
                    Vector3 local = inverse * corner;

                    if (!anyCorner)
                    {
                        min = local;
                        max = local;
                        anyCorner = true;
                    }
                    else
                    {
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }
        }

        if (!anyCorner)
            return;

        if (backdrop == null)
            CreateBackdrop();

        Vector3 center = (min + max) * 0.5f;

        // A world-space canvas is read looking along its forward axis, so +z here is behind it.
        // Offsetting by half the thickness as well keeps the visible gap exactly BACKDROP_GAP.
        center.z = max.z + BACKDROP_GAP + BACKDROP_THICKNESS * 0.5f;

        backdrop.SetActive(true);
        backdrop.transform.rotation = rotation;
        backdrop.transform.position = rotation * center;
        backdrop.transform.localScale = new Vector3(
            (max.x - min.x) + BACKDROP_PADDING * 2f,
            (max.y - min.y) + BACKDROP_PADDING * 2f,
            BACKDROP_THICKNESS
        );
    }


    private void CreateBackdrop()
    {
        // Kept at the scene root: parenting it to a panel would inherit that canvas's scale.
        backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backdrop.name = "MenuBackdrop";

        // Purely visual - it must never absorb a ray.
        Collider collider = backdrop.GetComponent<Collider>();

        if (collider != null)
            collider.enabled = false;

        MeshRenderer meshRenderer = backdrop.GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial =
            backdropMaterial != null
                ? backdropMaterial
                : CreateOpaqueWhiteMaterial();

        meshRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        meshRenderer.receiveShadows = false;
    }


    // Deliberately UNLIT: the backdrop must read as one flat white everywhere, unaffected by the
    // scene's lighting, so it never shades darker on one side or picks up the room's tint.
    private static Material CreateOpaqueWhiteMaterial()
    {
        // Fallbacks in order of preference - all unlit. Sprites/Default is a built-in shader and is
        // already relied on elsewhere in this project (the ray LineRenderers), so it always resolves.
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("MenuManager: no unlit shader found for the menu backdrop.");
            return null;
        }

        Material material = new Material(shader);

        // URP Unlit drives _BaseColor; the built-in unlit shaders drive _Color. Set whichever exists.
        material.color = Color.white;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        return material;
    }


    // ========================================================================
    // POSITION MENU IN FRONT OF CAMERA
    // ========================================================================

    // The shared parent of the panels. Everything is moved by this one transform, so the layout
    // authored in the editor - panel to panel, and every child within them - is carried across
    // untouched. Nothing writes to a panel's own transform any more.
    private Transform MenuRoot =>
        menuRoot != null
            ? menuRoot
            : (menuMain != null ? menuMain.transform.parent : null);

    public void PositionMenuInFrontOfCamera()
    {
        Transform root = MenuRoot;

        if (playerCamera == null || root == null || menuMain == null)
            return;

        // ------------------------------------------------------------
        // Face the camera, kept upright
        // ------------------------------------------------------------

        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;     // upright: ignore the camera's pitch

        // Degenerate only when looking straight up or down.
        if (forward.sqrMagnitude < 0.000001f)
        {
            forward = playerCamera.transform.right;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude > 0.000001f)
            root.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);


        // ------------------------------------------------------------
        // Slide the whole group so PanelMain lands in front of the camera
        // ------------------------------------------------------------
        //
        // The root is offset from the panels in the editor, so placing the root itself would put
        // the menu metres away. Translating by the delta keeps every relative offset intact while
        // anchoring the group on PanelMain, which is where the menu used to be placed directly.

        Vector3 target =
            playerCamera.transform.position +
            playerCamera.transform.forward * mainMenuDistanceFromCamera;

        target.y += menuVerticalOffset;

        // menuMain's world position already reflects the rotation set above.
        root.position += target - menuMain.transform.position;
    }


    // ========================================================================
    // OPEN / CLOSE MENU
    // ========================================================================

    private void MainMenuToggleByMouseClick()
    {
        if (openMenuClick == null)
            return;

        if (!openMenuClick.action.WasPressedThisFrame())
            return;


        // ------------------------------------------------------------
        // Toggle the main menu
        // ------------------------------------------------------------

        bool shouldShow =
            menuMain != null &&
            !menuMain.activeSelf;


        if (shouldShow)
        {
            Show_Menu_Ray();
        }
        else
        {
            Remove_Menu_Ray();
        }
    }


    // ========================================================================
    // FIND UI ELEMENT AT CENTER OF CAMERA
    // ========================================================================

    private void CheckButtonUnderCenterOfView()
    {
        if (playerCamera == null)
            return;

        if (eventSystem == null)
            return;


        // ------------------------------------------------------------
        // Create a ray from the center of the camera's FOV
        // ------------------------------------------------------------

        Vector2 viewportCenter =
            new Vector2(0.5f, 0.5f);

        Ray cameraRay =
            playerCamera.ViewportPointToRay(
                viewportCenter
            );


        // ------------------------------------------------------------
        // Check PanelMain
        // ------------------------------------------------------------

        Button newHoveredButton =
            RaycastPanel(
                mainGraphicRaycaster,
                cameraRay
            );


        // ------------------------------------------------------------
        // If PanelMain did not contain a button,
        // check PanelQA
        // ------------------------------------------------------------

        if (newHoveredButton == null)
        {
            newHoveredButton =
                RaycastPanel(
                    qaGraphicRaycaster,
                    cameraRay
                );
        }


        // ------------------------------------------------------------
        // The hovered button changed
        // ------------------------------------------------------------

        if (newHoveredButton != hoveredButton)
        {
            // Remove highlight from previous button
            if (hoveredButton != null)
            {
                hoveredButton.OnPointerExit(
                    new PointerEventData(eventSystem)
                );
            }


            // Store new button
            hoveredButton =
                newHoveredButton;


            // Highlight new button
            if (hoveredButton != null)
            {
                hoveredButton.OnPointerEnter(
                    new PointerEventData(eventSystem)
                );
            }
        }
    }


    // ========================================================================
    // RAYCAST A SINGLE PANEL
    // ========================================================================

    private Button RaycastPanel(
        GraphicRaycaster graphicRaycaster,
        Ray cameraRay)
    {
        if (graphicRaycaster == null)
            return null;


        // ------------------------------------------------------------
        // Find the Canvas belonging to this panel
        // ------------------------------------------------------------

        Canvas canvas =
            graphicRaycaster.GetComponent<Canvas>();

        if (canvas == null)
            return null;


        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        if (canvasRect == null)
            return null;


        // ------------------------------------------------------------
        // Find where the camera ray intersects the Canvas
        // ------------------------------------------------------------

        Plane canvasPlane =
            new Plane(
                canvasRect.forward,
                canvasRect.position
            );


        if (!canvasPlane.Raycast(
            cameraRay,
            out float distance))
        {
            return null;
        }


        // ------------------------------------------------------------
        // Don't interact with a Canvas behind the user
        // ------------------------------------------------------------

        if (distance < 0 ||
            distance > raycastDistance)
        {
            return null;
        }


        // ------------------------------------------------------------
        // Get the actual world position on the Canvas
        // ------------------------------------------------------------

        Vector3 worldHit =
            cameraRay.GetPoint(distance);


        // ------------------------------------------------------------
        // Convert world position into screen coordinates
        // ------------------------------------------------------------

        Vector3 screenPoint =
            playerCamera.WorldToScreenPoint(
                worldHit
            );

        Vector2 screenPosition =
            new Vector2(
                screenPoint.x,
                screenPoint.y
            );


        // ------------------------------------------------------------
        // Create PointerEventData
        // ------------------------------------------------------------

        PointerEventData pointerData =
            new PointerEventData(eventSystem);

        pointerData.position =
            screenPosition;


        // ------------------------------------------------------------
        // Graphic Raycast
        // ------------------------------------------------------------

        List<RaycastResult> results =
            new List<RaycastResult>();

        graphicRaycaster.Raycast(
            pointerData,
            results
        );


        // ------------------------------------------------------------
        // Find a Button
        // ------------------------------------------------------------

        foreach (RaycastResult result in results)
        {
            Button button =
                result.gameObject.GetComponent<Button>();


            if (button == null)
            {
                button =
                    result.gameObject.GetComponentInParent<Button>();
            }


            if (button != null &&
                button.interactable)
            {
                return button;
            }
        }


        return null;
    }


    // ========================================================================
    // SELECT BUTTON
    // ========================================================================

    private void SelectHoveredButton()
    {
        if (selectItem == null)
            return;

        if (!selectItem.action.WasPressedThisFrame())
            return;

        if (hoveredButton == null)
            return;

        if (!hoveredButton.interactable)
            return;


        // ------------------------------------------------------------
        // Trigger the button's normal Unity onClick events
        // ------------------------------------------------------------

        hoveredButton.onClick.Invoke();
    }
}