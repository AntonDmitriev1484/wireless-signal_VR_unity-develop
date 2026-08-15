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

    [Header("Panel Positions")]
    [SerializeField]
    private float mainMenuDistanceFromCamera = 2f;

    [SerializeField]
    private float qaPanelDistanceFromMain = 0.6f;

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
        // Position PanelMain in front of camera
        // ------------------------------------------------------------

        SetMenuInFrontOfCamera(
            menuMain,
            mainMenuDistanceFromCamera
        );


        // ------------------------------------------------------------
        // Position PanelQA to the left of PanelMain
        // ------------------------------------------------------------

        PositionQAPanel();
    }


    // ========================================================================
    // POSITION MAIN MENU IN FRONT OF CAMERA
    // ========================================================================

    public void SetMenuInFrontOfCamera(
        GameObject menu,
        float distanceFromCamera)
    {
        if (playerCamera == null || menu == null)
            return;

        float yOffset = -0.3f;

        Vector3 menuPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            distanceFromCamera;

        menuPosition.y += yOffset;

        menu.transform.position = menuPosition;


        // ------------------------------------------------------------
        // Make menu face the camera
        // ------------------------------------------------------------

        Vector3 directionToCamera =
            playerCamera.transform.position -
            menu.transform.position;

        // Keep menu upright
        directionToCamera.y = 0f;

        if (directionToCamera.magnitude > 0.001f)
        {
            menu.transform.rotation =
                Quaternion.LookRotation(
                    -directionToCamera,
                    Vector3.up
                );
        }
        else
        {
            Vector3 forward =
                playerCamera.transform.forward;

            forward.y = 0f;

            if (forward.magnitude < 0.001f)
            {
                forward = playerCamera.transform.right;
            }

            menu.transform.rotation =
                Quaternion.LookRotation(
                    forward,
                    Vector3.up
                );
        }
    }


    // ========================================================================
    // POSITION QA PANEL
    // ========================================================================

    private void PositionQAPanel()
    {
        if (menuMain == null || menuQA == null)
            return;

        // ------------------------------------------------------------
        // Put QA to the LEFT of PanelMain
        //
        // PanelMain's local right direction is used so that the
        // relationship remains correct regardless of rotation.
        // ------------------------------------------------------------

        Vector3 qaPosition =
            menuMain.transform.position -
            menuMain.transform.right *
            qaPanelDistanceFromMain;

        menuQA.transform.position = qaPosition;


        // ------------------------------------------------------------
        // Give QA the same rotation as PanelMain
        // ------------------------------------------------------------

        menuQA.transform.rotation =
            menuMain.transform.rotation;
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