using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [SerializeField, Tooltip("VR player object")] 
    private Transform VRplayer;
    [SerializeField, Tooltip("1st level floor object")] 
    private Transform FloorLevel1Obj;
    [SerializeField, Tooltip("1st level floor object")] 
    private Transform FloorLevel2Obj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Moves the player to the 1st floor level , only y position is changed
    public void MovePlayerFloorLevel1()
    {
        if (VRplayer != null && FloorLevel1Obj != null)
        {
            // Get the current player position
            Vector3 playerPosition = VRplayer.position;
            
            // Set the player's y position to match the floor level object's y position
            playerPosition.y = FloorLevel1Obj.position.y;
            
            // Update the player's position
            VRplayer.position = playerPosition;
            
            //Debug.Log($"Moving player to floor level: Current Y = {playerPosition.y}, Floor Level Y = {FloorLevel1Obj.position.y}");

        }
        else
        {
            Debug.LogWarning("VRplayer or FloorLevel1Obj is not assigned");
        }
    }


    // Moves the player to the 2nd floor level, only y position is changed
    public void MovePlayerFloorLevel2()
    {
        if (VRplayer != null && FloorLevel2Obj != null)
        {
            // Get the current player position
            Vector3 playerPosition = VRplayer.position;
            
            // Set the player's y position to match the upper level object's y position
            playerPosition.y = FloorLevel2Obj.position.y;



            // Update the player's position
            VRplayer.position = playerPosition;
            
            //Debug.Log($"Moving player to upper level: Current Y = {playerPosition.y}, Upper Level Y = {FloorLevel2Obj.position.y}");

        }
        else
        {
            Debug.LogWarning("VRplayer or FloorLevel2Obj is not assigned");
        }
    }
}
