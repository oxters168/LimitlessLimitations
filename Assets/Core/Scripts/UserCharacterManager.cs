using UnityEngine;
using UnityHelpers;

public class UserCharacterManager : MonoBehaviour
{
    public int playerId;
    public OrbitCameraController cameraPrefab;
    private OrbitCameraController followCamera;
    public MimicTransform waterPrefab;
    private MimicTransform waterInstance;
    public AnimateAndMoveCharacter characterPrefab;
    private AnimateAndMoveCharacter characterInstance;
    // private AnimateAndMoveCharacter dummyCharacter;

    public bool InVehicle { get { return currentVehicle != null; } }
    public ValuedObject currentVehicle;
    public ValuedObject vehicleInVicinity;
    private bool enteredExited;

    private bool astralled;

    void Start()
    {
        characterInstance = GameObject.Instantiate(characterPrefab);
        var bridge = characterInstance.gameObject.AddComponent<PlayerInputBridge>();

        // dummyCharacter = GameObject.Instantiate(characterPrefab);
        // Destroy(dummyCharacter.GetComponent<Collider>());
        // dummyCharacter.gameObject.SetActive(false);

        bridge.playerId = playerId;
        bridge.controlledObject = characterInstance.gameObject;

        waterInstance = GameObject.Instantiate(waterPrefab);

        followCamera = GameObject.Instantiate(cameraPrefab);

        FollowTransform(characterInstance.mainShells.transform);
    }
    void Update()
    {
        SetCameraValues();
        // dummyCharacter.gameObject.SetActive(characterInstance.astral && characterInstance.InAstral);
    }
    void FixedUpdate()
    {
        EnterExitAstral();

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
            height = characterInstance.mainShells.transform.position.y;
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

    private void EnterExitAstral()
    {
        if (characterInstance.GetToggle("down"))
        {
            if (!characterInstance.astral && !characterInstance.InAstral && !InVehicle && !characterInstance.IsUnderwater)
            {
                if (!astralled)
                {
                    characterInstance.astral = true;
                    astralled = true;
                }
            }

            if (characterInstance.astral && characterInstance.InAstral)
            {
                if (!astralled)
                {
                    characterInstance.astral = false;
                    astralled = true;
                }
            }
        }
        else
            astralled = false;
    }
    private void FindVehicleInVicinity()
    {
        if (!InVehicle)
        {
            RaycastHit[] raycastHits = Physics.BoxCastAll(characterInstance.mainShells.transform.position + Vector3.up, new Vector3(2.5f, 1f, 2.5f), Vector3.up, Quaternion.identity, 1f, LayerMask.GetMask("Vehicle"));
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
        if (!characterInstance.astral && ((!InVehicle && vehicleInVicinity != null && characterInstance.GetToggle("triangleBtn")) || (InVehicle && currentVehicle.GetToggle("triangleBtn"))))
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
        FollowTransform(vehicle != null ? vehicle.transform : characterInstance.mainShells.transform);

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
