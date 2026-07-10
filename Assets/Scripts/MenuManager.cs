using UnityEngine;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    // Reference to the menuMain object
    private GameObject menuMain;

    [SerializeField]
    private GameObject menuCredit;


    //[Header("VR Control")]
    [SerializeField]
    // Reference to the Input Action Asset for the XR controller
    private InputActionProperty rController_A;

    [SerializeField]
    // Reference to Ray Interactor
    private GameObject rayInteractor;

    // make menuMain appear in front of VR camera
    [SerializeField]
    private Transform vrCamera;

    private float mainMenuDistanceFromCamera = 2;
    private float creditMenuDistanceFromCamera = 3;

    // make a menu appear in front of the VR camera
    public void SetMenuInFrontOfCamera(GameObject menu, float distanceFromCamera)
    {
        // Set the position of the menu in front of the VR camera
        // Add a y-offset to position the menu lower than the camera's eye level
        float yOffset = -0.3f; // Negative value moves menu down
        Vector3 menuPosition = vrCamera.position + vrCamera.forward * distanceFromCamera;
        menuPosition.y += yOffset; // Apply the vertical offset
        menu.transform.position = menuPosition;

        // Create a rotation that faces the camera but keeps the menu upright
        Vector3 directionToCamera = vrCamera.position - menu.transform.position;
        directionToCamera.y = 0; // Zero out the y component to prevent tilting

        // If directly above/below the camera, set the rotation
        if (directionToCamera.magnitude > 0.001f)
        {
            menu.transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        }
        else
        {
            // If directly above/below, just use camera's forward direction but keep upright
            Vector3 forward = vrCamera.forward;
            forward.y = 0;
            if (forward.magnitude < 0.001f)
                forward = vrCamera.right; // Fallback if looking straight up/down

            menu.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ensure the menuMain and rayInteractor are initially deactivated - R-Controller A button will activate them
        Remove_Menu_Ray();

        // disable menuCredit at the start
        menuCredit.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        MainMenuToggleByControllerButton();

    }

    public void Remove_Menu_Ray()
    {
        // off menuMain and oof rayInteractor
        menuMain.SetActive(false);
        rayInteractor.SetActive(false);
    }

    public void Show_Menu_Ray()
    {
        // off menuMain and oof rayInteractor
        menuMain.SetActive(true);
        rayInteractor.SetActive(true);
    }



    private void MainMenuToggleByControllerButton()
    {   //---------------------------------------------------------------------------------
        // toggle the menuMain on and off when A button is pressed of oculus R-controller with XR toolkit
        if (rController_A.action.WasPressedThisFrame())
        {
            // toggle the menuMain on and off
            menuMain.SetActive(!menuMain.activeSelf);

            // display menuMain.activeSelf in the console
            //Debug.Log("Menu Main Active: " + menuMain.activeSelf);

            // activate the ray interactor only when the menuMain is active
            if (menuMain.activeSelf)
            {
                // Set the ray interactor to be active
                rayInteractor.SetActive(true);

                // set the menuMain postion in front of the camera
                SetMenuInFrontOfCamera(menuMain, mainMenuDistanceFromCamera);
            }
            else
            {
                // Set the ray interactor to be inactive
                rayInteractor.SetActive(false);
            }
        }
    }

    //  show the menuCredit in front of camera 
    public void ButtonPressed_ShowCredit()
    {
        // currently menuMain is on, so off the menuMain after selecting a button
        menuMain.SetActive(false);

        // Set the menuCredit to be active
        menuCredit.SetActive(true);
        // Set the menuCredit position in front of the camera
        SetMenuInFrontOfCamera(menuCredit, creditMenuDistanceFromCamera);

        rayInteractor.SetActive(true);

    }

    public void ButtonPressed_HideCredit()
    {
        menuCredit.SetActive(false);
    }
}
