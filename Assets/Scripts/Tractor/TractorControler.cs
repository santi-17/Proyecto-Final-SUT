using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorControler : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle;
    private float currentbreakForce;
    private bool isBreaking;

    // Settings
    [SerializeField] private AnimationCurve torqueCurve; // Maximum speed in km/h
    [SerializeField] private float maxRPM = 6000f; // Maximum RPM of the engine
    [SerializeField] private float motorForce = 3000f;
    [SerializeField] private Transmision transmision; // Assuming Transmision is a class that handles gear ratios and torque
    [SerializeField] private float breakForce = 3000f;
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] public float steeringSpeed = 5f;

    //Rigidbody
    [SerializeField] private Rigidbody rb; // Reference to the Rigidbody component of the tractor

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    private void GetInput()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");

        // Acceleration Input
        verticalInput = Input.GetAxis("Vertical");

        // Breaking Input
        isBreaking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor()
    {
        //float torque = verticalInput * motorForce * transmision.GetTorque();
        float rpm = rb.velocity.magnitude * 60f / (2f * Mathf.PI * frontLeftWheelCollider.radius); // Calculate RPM based on wheel speed
        float torque = torqueCurve.Evaluate(rpm / maxRPM) * transmision.GetTorque() * verticalInput * motorForce;
        //float appliedForce = verticalInput * motorForce;
        frontLeftWheelCollider.motorTorque = torque;
        frontRightWheelCollider.motorTorque = torque;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        float  targetAngle = horizontalInput * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * steeringSpeed);
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
    
}
