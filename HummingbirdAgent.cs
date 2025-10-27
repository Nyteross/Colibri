using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;

    new private Rigidbody rigidbody;

    private FlowerArea flowerArea;

    private Flower nearestFlower;

    private float smoothPitchChange = 0f;

    private float smoothYawChange = 0f;

    private const float MaxPitchAngle = 80f;

    private const float BeakTipRadius = 0.008f;

    private bool frozen = false;

    public float NectarObtained { get; private set; }

    public override void Initialize()
    {
        rigidbody = GetComponent<RigidBody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        if (!trainingMode) MaxStep = 0;
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            flowerArea.ResetFlowers();
        }

        NectarObtained = 0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);

        UpdateNearestFlower();
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (frozen) return;

        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        rigidbody.AddForce(move * moveForce);

        Vector3 rotationVector = transform.rotation.eulerAngles;

        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);



    }
    
    public override void CollectObservations(VectorSensor sensor)
    {

        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        sensor.AddObservation(transform.localRotation.normalized);

        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        sensor.AddObservation(toFlower.normalized);

        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);
    }

    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                float radius = UnityEngine.Random.Range(2f, 7f);

                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);

                Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

                safePositionFound = colliders.Length == 0;
            }

            Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

            transform.position = potentialPosition;
            transform.rotation = potentialRotation;
        }
    }


    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }
}
