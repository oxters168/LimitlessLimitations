using UnityEngine;
using UnityHelpers;

public class UserCharacterManager : MonoBehaviour
{
    public int playerId;
    public OrbitCameraController followCamera;
    public MimicTransform waterPrefab;
    private MimicTransform waterInstance;
    public ValuedObject characterPrefab;
    private ValuedObject characterInstance;

    public ValuedObject currentVehicle;
    public ValuedObject vehicleInVicinity;
    private bool enteredExited;

    void Start()
    {
        characterInstance = GameObject.Instantiate(characterPrefab);
        var bridge = characterInstance.gameObject.AddComponent<PlayerInputBridge>();
        bridge.playerId = playerId;
        bridge.controlledObject = characterInstance.gameObject;

        waterInstance = GameObject.Instantiate(waterPrefab);

        FollowTransform(characterInstance.transform);
    }
    void FixedUpdate()
    {
        FindVehicleInVicinity();
        EnterExitVehicle();
    }

    private void FindVehicleInVicinity()
    {
        if (currentVehicle == null)
        {
            RaycastHit[] raycastHits = Physics.BoxCastAll(characterInstance.transform.position + Vector3.up, new Vector3(2.5f, 1f, 2.5f), Vector3.up, Quaternion.identity, 1f, LayerMask.GetMask("Vehicle"));
            if (raycastHits.Length > 0)
            {
                var valuedVehicle = raycastHits[0].transform.GetComponent<ValuedObject>();
                if (valuedVehicle != null)
                    vehicleInVicinity = valuedVehicle;
            }
            else
                vehicleInVicinity = null;
        }
    }
    private void EnterExitVehicle()
    {
        if ((currentVehicle == null && vehicleInVicinity != null && characterInstance.GetToggle("triangleBtn")) || (currentVehicle != null && currentVehicle.GetToggle("triangleBtn")))
        {
            if (!enteredExited)
            {
                enteredExited = true;
                SetVehicle(currentVehicle == null ? vehicleInVicinity : null);
            }
        }
        else
            enteredExited = false;
    }

    public void SetVehicle(ValuedObject vehicle)
    {
        if (vehicle != null)
        {
            var inputBridge = vehicle.gameObject.AddComponent<PlayerInputBridge>();
            inputBridge.playerId = playerId;
            inputBridge.controlledObject = vehicle.gameObject;

            followCamera.distance = 30;
        }
        else
        {
            var inputBridge = currentVehicle.gameObject.GetComponent<PlayerInputBridge>();
            if (inputBridge != null)
                GameObject.Destroy(inputBridge);

            followCamera.distance = 10;
        }

        HideCharacter(vehicle == null);
        FollowTransform(vehicle != null ? vehicle.transform : characterInstance.transform);

        currentVehicle = vehicle;
    }
    public void HideCharacter(bool onOff)
    {
        characterInstance.gameObject.SetActive(onOff);
    }

    private void FollowTransform(Transform target)
    {
        waterInstance.other = target;
        followCamera.target = target;
    }
}
