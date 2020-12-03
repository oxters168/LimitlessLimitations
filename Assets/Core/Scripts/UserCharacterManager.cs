using UnityEngine;
using UnityHelpers;

public class UserCharacterManager : MonoBehaviour
{
    public int playerId;
    public OrbitCameraController cameraPrefab;
    private OrbitCameraController followCamera;
    public MimicTransform waterPrefab;
    private MimicTransform waterInstance;
    public ValuedObject characterPrefab;
    private ValuedObject characterInstance;

    public bool InVehicle { get { return currentVehicle != null; } }
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

        followCamera = GameObject.Instantiate(cameraPrefab);

        FollowTransform(characterInstance.transform);
    }
    void Update()
    {
        SetCameraValues();
    }
    void FixedUpdate()
    {
        FindVehicleInVicinity();
        EnterExitVehicle();
    }

    private void SetCameraValues()
    {
        float height;
        float minDistance;
        float maxDistance;

        if (InVehicle)
        {
            height = currentVehicle.transform.position.y;
            minDistance = 20;
            maxDistance = 40;
        }
        else
        {
            height = characterInstance.transform.position.y;
            minDistance = 5;
            maxDistance = 40;
        }

        float minAngle = 45;
        float maxAngle = 90;
        float minHeight = 15;
        float maxHeight = 100;
        float percentHeight = Mathf.Clamp01((height - minHeight) / maxHeight);
        float currentDistance = (maxDistance - minDistance) * percentHeight + minDistance;
        float currentAngle = (maxAngle - minAngle) * percentHeight + minAngle;
        followCamera.distance = Mathf.Lerp(followCamera.distance, currentDistance, Time.deltaTime * 5);
        followCamera.rightAngle = Mathf.Lerp(followCamera.rightAngle, currentAngle, Time.deltaTime * 5);
    }
    private void FindVehicleInVicinity()
    {
        if (!InVehicle)
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
        if ((!InVehicle && vehicleInVicinity != null && characterInstance.GetToggle("triangleBtn")) || (InVehicle && currentVehicle.GetToggle("triangleBtn")))
        {
            if (!enteredExited)
            {
                enteredExited = true;
                SetVehicle(!InVehicle ? vehicleInVicinity : null);
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
        }
        else
        {
            var inputBridge = currentVehicle.gameObject.GetComponent<PlayerInputBridge>();
            if (inputBridge != null)
                GameObject.Destroy(inputBridge);
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
