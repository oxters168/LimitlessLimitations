using UnityEngine;
using UnityHelpers;
using Rewired;

public class PlayerInputBridge : MonoBehaviour
{
    public int playerId = 0;
    private Player player;

    [RequireInterface(typeof(IValueManager))]
    public GameObject controlledObject;
    private IValueManager controlValues;

    void Start()
    {
        player = ReInput.players.GetPlayer(playerId);
        controlValues = controlledObject.GetComponent<IValueManager>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = 0;
        x += player.GetButton("Horizontal") ? 1 : 0;
        x -= player.GetNegativeButton("Horizontal") ? 1 : 0;
        float y = 0;
        y += player.GetButton("Vertical") ? 1 : 0;
        y -= player.GetNegativeButton("Vertical") ? 1 : 0;

        controlValues.SetAxis("dpadHor", x);
        controlValues.SetAxis("dpadVer", y);
        controlValues.SetToggle("crossBtn", player.GetButton("Jog"));
        controlValues.SetToggle("triangleBtn", player.GetButton("EnterExitVehicle"));
    }
}
