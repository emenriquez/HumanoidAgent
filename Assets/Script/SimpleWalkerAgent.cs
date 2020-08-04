using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

[RequireComponent(typeof(JointDriveController))] // Required to set joint forces
public class SimpleWalkerAgent : Agent
{
    float maximumWalkingSpeed = 999; //The max walk velocity magnitude an agent will be rewarded for
    Vector3 m_WalkDir; //Direction to the target
    public float currentReward = 0f;

    [Header("Target To Walk Towards")] [Space(10)]
    public TargetController target; //Target the agent will walk towards.

    [Header("Body Parts")] [Space(10)] 
    public Transform Head;
    public Transform Hips;
    public Transform Core;
    public Transform Chest;
    public Transform ForeArmL, UpperArmL, ForeArmR, UpperArmR;
    public Transform ThighR, ShinR, FootR, ThighL, ShinL, FootL;

    [Header("Orientation")] [Space(10)]
    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController orientationCube;

    [Header("Heuristic Joint Controls")] [Space(10)]
    public float fx;
    public float ux, uy;
    public float tx, ty, tz, sx, fox, foy;

    public float cx, cy, cz;
    public float cox, coy, coz;
    public float strength;

    JointDriveController m_JdController;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_JdController = GetComponent<JointDriveController>();

        orientationCube.UpdateOrientation(Hips, target.transform);

        //Setup each body part
        m_JdController.SetupBodyPart(Hips);
        m_JdController.SetupBodyPart(Core);
        m_JdController.SetupBodyPart(Chest);
        m_JdController.SetupBodyPart(Head);

        m_JdController.SetupBodyPart(ThighL);
        m_JdController.SetupBodyPart(ShinL);
        m_JdController.SetupBodyPart(FootL);
        m_JdController.SetupBodyPart(ThighR);
        m_JdController.SetupBodyPart(ShinR);
        m_JdController.SetupBodyPart(FootR);

        m_JdController.SetupBodyPart(ForeArmL);
        m_JdController.SetupBodyPart(UpperArmL);
        m_JdController.SetupBodyPart(ForeArmR);
        m_JdController.SetupBodyPart(UpperArmR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

    }

    public override void OnEpisodeBegin() {       
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        orientationCube.UpdateOrientation(Hips, target.transform);
    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(Quaternion.FromToRotation(Hips.forward, orientationCube.transform.forward));
        sensor.AddObservation(Quaternion.FromToRotation(Head.forward, orientationCube.transform.forward));

        sensor.AddObservation(orientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public override void OnActionReceived(float[] vectorAction) {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        bpDict[UpperArmL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[ForeArmL].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[UpperArmR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[ForeArmR].SetJointTargetRotation(vectorAction[++i], 0, 0);

        bpDict[ThighL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
        bpDict[ShinL].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[FootL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[ThighR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
        bpDict[ShinR].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[FootR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);

        bpDict[Chest].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
        bpDict[Core].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);

        bpDict[UpperArmL].SetJointStrength(vectorAction[++i]);
        bpDict[ForeArmL].SetJointStrength(vectorAction[++i]);
        bpDict[UpperArmR].SetJointStrength(vectorAction[++i]);
        bpDict[ForeArmR].SetJointStrength(vectorAction[++i]);
        bpDict[ThighL].SetJointStrength(vectorAction[++i]);
        bpDict[ShinL].SetJointStrength(vectorAction[++i]);
        bpDict[FootL].SetJointStrength(vectorAction[++i]);
        bpDict[ThighR].SetJointStrength(vectorAction[++i]);
        bpDict[ShinR].SetJointStrength(vectorAction[++i]);
        bpDict[FootR].SetJointStrength(vectorAction[++i]);
        bpDict[Chest].SetJointStrength(vectorAction[++i]);
        bpDict[Core].SetJointStrength(vectorAction[++i]);

        // This is used to discourage action inputs outside of the useful range (-1, 1)
        foreach (var action in vectorAction) {
            if (Mathf.Abs(action) > 1.0f ) {
                AddReward(-.002f*action);
            }
        }
    }

    // FixedUpdate is used here to administer rewards based on actions
    void FixedUpdate()
    {
        var cubeForward = orientationCube.transform.forward;
        orientationCube.UpdateOrientation(Hips, target.transform);
        // Set reward for this step according to mixture of the following elements.
        
        // a. Velocity alignment with goal direction.
        var moveTowardsTargetReward = Vector3.Dot(cubeForward,
            Vector3.ClampMagnitude(m_JdController.bodyPartsDict[Hips].rb.velocity, maximumWalkingSpeed));
        if (float.IsNaN(moveTowardsTargetReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n"+
                $" hips.velocity: {m_JdController.bodyPartsDict[Hips].rb.velocity}\n"+
                $" maximumWalkingSpeed: {maximumWalkingSpeed}"
            );
        }

        // b. Rotation alignment with goal direction.
        var lookAtTargetReward = Vector3.Dot(cubeForward, Hips.forward);
        if (float.IsNaN(lookAtTargetReward))
        {
            throw new ArgumentException(
                "NaN in lookAtTargetReward.\n" +
                $" cubeForward: {cubeForward}\n"+
                $" hips.forward: {Hips.forward}"
            );
        }

        // c. Encourage head height. //Should normalize to ~1
        var headHeightOverFeetReward =
            ((Head.position.y - FootL.position.y) + (Head.position.y - FootR.position.y) / 9);
        if (float.IsNaN(headHeightOverFeetReward))
        {
            throw new ArgumentException(
                "NaN in headHeightOverFeetReward.\n" +
                $" head.position: {Head.position}\n"+
                $" footL.position: {FootL.position}\n"+
                $" footR.position: {FootR.position}"
            );
        }

        AddReward(
            + 0.02f * moveTowardsTargetReward
            + 0.02f * lookAtTargetReward
            + 0.005f * headHeightOverFeetReward
        );

        // End if Agent falls off
        if (Hips.position.y < -3) {
            AddReward(-1f);
            EndEpisode();
        }

        currentReward = GetCumulativeReward();
    }

    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor) {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - Hips.position));

        if (bp.rb.transform != Hips)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    // Heuristic Controls for debugging. Has not been tested, but "TestMotionScript" contains similar code that will work for testing.
    public override void Heuristic(float[] inputAction) {
        var i = -1;
        inputAction = new float[] { ux, uy, fx, ux, -uy, fx, tx, ty, tz, sx, fox, foy, tx, ty, tz, sx, fox, fox, foy, cx, cy, cz, cox, coy, coz, strength, strength, strength, strength, strength, strength, strength, strength, strength, strength, strength};
        
        var bpDict = m_JdController.bodyPartsDict;

        bpDict[UpperArmL].SetJointTargetRotation(inputAction[++i], inputAction[++i], 0);
        bpDict[ForeArmL].SetJointTargetRotation(inputAction[++i], 0, 0);
        bpDict[UpperArmR].SetJointTargetRotation(inputAction[++i], inputAction[++i], 0);
        bpDict[ForeArmR].SetJointTargetRotation(inputAction[++i], 0, 0);

        bpDict[ThighL].SetJointTargetRotation(inputAction[++i], inputAction[++i], inputAction[++i]);
        bpDict[ShinL].SetJointTargetRotation(inputAction[++i], 0, 0);
        bpDict[FootL].SetJointTargetRotation(inputAction[++i], inputAction[++i], 0);
        bpDict[ThighR].SetJointTargetRotation(inputAction[++i], inputAction[++i], inputAction[++i]);
        bpDict[ShinR].SetJointTargetRotation(inputAction[++i], 0, 0);
        bpDict[FootR].SetJointTargetRotation(inputAction[++i], inputAction[++i], 0);

        bpDict[Chest].SetJointTargetRotation(inputAction[++i], inputAction[++i], inputAction[++i]);
        bpDict[Core].SetJointTargetRotation(inputAction[++i], inputAction[++i], inputAction[++i]);

        foreach (var bodyPart in bpDict.Values)
        {
            if (bodyPart.rb.transform != Hips) {
                bodyPart.SetJointStrength(strength);
            }
        }
    }

        public void TouchedTarget()
    {
        AddReward(1f);
        EndEpisode();
    }
}
