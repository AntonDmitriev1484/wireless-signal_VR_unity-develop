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

    [Header("Camera")]
    [SerializeField]
    private Camera playerCamera;

    [Header("UI Raycast")]
    [SerializeField]
    private GraphicRaycaster graphicRaycaster;

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

    private float mainMenuDistanceFromCamera = 2f;

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

        if (menuMain.activeSelf)
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

        hoveredButton = null;
    }


    public void Show_Menu_Ray()
    {
        if (menuMain == null)
            return;

        menuMain.SetActive(true);

        SetMenuInFrontOfCamera(
            menuMain,
            mainMenuDistanceFromCamera
        );
    }


    // ========================================================================
    // POSITION MENU IN FRONT OF CAMERA
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


        // Make the menu face the camera
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
    // OPEN / CLOSE MENU
    // ========================================================================

    private void MainMenuToggleByMouseClick()
    {
        if (openMenuClick == null)
            return;

        if (openMenuClick.action.WasPressedThisFrame())
        {
            menuMain.SetActive(!menuMain.activeSelf);

            if (menuMain.activeSelf)
            {
                SetMenuInFrontOfCamera(
                    menuMain,
                    mainMenuDistanceFromCamera
                );
            }
            else
            {
                hoveredButton = null;
            }
        }
    }


    // ========================================================================
    // FIND UI ELEMENT AT CENTER OF CAMERA
    // ========================================================================

    private void CheckButtonUnderCenterOfView()
    {
        if (playerCamera == null)
            return;

        if (graphicRaycaster == null)
            return;

        if (eventSystem == null)
            return;

        // ------------------------------------------------------------
        // Create a ray from the center of the camera's FOV
        // ------------------------------------------------------------

        Vector2 viewportCenter = new Vector2(0.5f, 0.5f);

        Ray cameraRay =
            playerCamera.ViewportPointToRay(viewportCenter);


        // ------------------------------------------------------------
        // Find the Canvas
        // ------------------------------------------------------------

        Canvas canvas =
            graphicRaycaster.GetComponent<Canvas>();

        if (canvas == null)
            return;


        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();


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
            hoveredButton = null;
            return;
        }


        // Don't interact with the Canvas if it is behind us
        if (distance < 0 || distance > raycastDistance)
        {
            hoveredButton = null;
            return;
        }


        // ------------------------------------------------------------
        // Get the actual world position on the Canvas
        // ------------------------------------------------------------

        Vector3 worldHit =
            cameraRay.GetPoint(distance);


        // ------------------------------------------------------------
        // Convert that point into screen coordinates
        // ------------------------------------------------------------

        Vector3 screenPoint =
            playerCamera.WorldToScreenPoint(worldHit);

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

        pointerData.position = screenPosition;


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

        Button newHoveredButton = null;

        foreach (RaycastResult result in results)
        {
            Debug.Log(result);

            Button button =
                result.gameObject.GetComponent<Button>();

            if (button == null)
            {
                button =
                    result.gameObject.GetComponentInParent<Button>();
            }

            if (button != null && button.interactable)
            {
                newHoveredButton = button;
                break;
            }
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
            hoveredButton = newHoveredButton;

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


        Debug.Log(
            "Selected button: " +
            hoveredButton.name
        );


        // Trigger the button's normal Unity onClick events
        hoveredButton.onClick.Invoke();
    }
}