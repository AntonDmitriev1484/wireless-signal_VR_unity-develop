using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Desktop HUD menu. The Menus canvas is a screen-space overlay laid out in the scene:
//   - left stack  (PanelMain): Play/Pause, Restart, Rays, Heatmap, then the heatmap colour bar
//   - bottom strip (PanelQA):  tutorial text, Back on the left, Next on the right,
//                              answer buttons A-D in a row just above the strip
//
// The tutorial text is always visible. Pressing E (Open Menu) enters menu mode: the left stack and
// the QA buttons appear, the cursor is freed and the player stops moving/looking. E again leaves it.
//
// Constraints from MoveAsParticleTest1_v2 to keep in mind when editing the layout:
//   - buttons are found by the method their onClick calls, so keep their On Click entries
//   - the QA gate locks every button under the answer buttons' parent, so AnsA-D, Back and Next
//     must stay direct children of PanelQA
//   - QA buttons are hidden with a CanvasGroup, never SetActive, because the task managers own
//     their active state and the gate owns Button.interactable
public class MenuManager : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField]
    private GameObject menuMain;

    [SerializeField]
    private GameObject menuQA;

    [SerializeField, Tooltip("Optional. Hidden while the menu is open. Leave empty to look up 'CrosshairCanvas'.")]
    private GameObject crosshair;

    [Header("Input")]
    [SerializeField]
    private InputActionReference openMenuClick;

    private bool menuOpen;

    // Every button in the bottom strip; hidden (not deactivated) while the menu is closed.
    private CanvasGroup[] qaButtonGroups = new CanvasGroup[0];


    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        if (crosshair == null)
            crosshair = GameObject.Find("CrosshairCanvas");

        CollectQAButtonGroups();
    }

    private void Start()
    {
        if (openMenuClick != null)
            openMenuClick.action.Enable();

        SetMenuOpen(false);
    }

    private void Update()
    {
        if (openMenuClick != null && openMenuClick.action.WasPressedThisFrame())
            SetMenuOpen(!menuOpen);
    }

    private void OnDestroy()
    {
        if (openMenuClick != null)
            openMenuClick.action.Disable();

        PlayerController.InputLocked = false;
    }


    // ========================================================================
    // MENU MODE
    // ========================================================================

    public bool IsMenuOpen => menuOpen;

    public void SetMenuOpen(bool open)
    {
        menuOpen = open;

        if (menuMain != null)
            menuMain.SetActive(open);

        // PanelQA stays active so the tutorial text is always readable; only its buttons hide.
        if (menuQA != null)
            menuQA.SetActive(true);

        foreach (CanvasGroup group in qaButtonGroups)
        {
            if (group == null) continue;
            group.alpha = open ? 1f : 0f;
            group.blocksRaycasts = open;
        }

        if (crosshair != null)
            crosshair.SetActive(!open);

        PlayerController.InputLocked = open;

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    // Kept for anything still wired to the old names.
    public void Show_Menu_Ray() => SetMenuOpen(true);
    public void Remove_Menu_Ray() => SetMenuOpen(false);

    // Every button directly under PanelQA (Back, Next, AnsA-D) gets a CanvasGroup so it can be
    // hidden while the menu is closed.
    private void CollectQAButtonGroups()
    {
        if (menuQA == null)
            return;

        var groups = new System.Collections.Generic.List<CanvasGroup>();

        foreach (Transform child in menuQA.transform)
        {
            if (child.GetComponent<Button>() == null)
                continue;

            CanvasGroup group = child.GetComponent<CanvasGroup>();
            groups.Add(group != null ? group : child.gameObject.AddComponent<CanvasGroup>());
        }

        qaButtonGroups = groups.ToArray();
    }
}
