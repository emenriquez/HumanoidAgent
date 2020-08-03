using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;

[RequireComponent(typeof(JointDriveController))] // Required to set joint forces
public class TestMotionScript : MonoBehaviour
{
    [Header("Body Parts")] [Space(10)] 
    public Transform Hips;
    public Transform Core;
    public Transform Chest;
    public Transform ForeArmL, UpperArmL, ForeArmR, UpperArmR;
    public Transform ThighR, ShinR, FootR, ThighL, ShinL, FootL;

    [Header("Joint Controls")] [Space(10)]
    public float fx;
    public float ux, uy;
    public float tx, ty, sx, fox, foy;

    public float cx, cy, cz;
    public float cox, coy, coz;
    public float strength;

    JointDriveController m_JdController;

    // Start is called before the first frame update
    void Start()
    {
        m_JdController = GetComponent<JointDriveController>();

        //Setup each body part
        m_JdController.SetupBodyPart(ForeArmL);
        m_JdController.SetupBodyPart(UpperArmL);
        m_JdController.SetupBodyPart(ForeArmR);
        m_JdController.SetupBodyPart(UpperArmR);

        m_JdController.SetupBodyPart(ThighL);
        m_JdController.SetupBodyPart(ShinL);
        m_JdController.SetupBodyPart(FootL);
        m_JdController.SetupBodyPart(ThighR);
        m_JdController.SetupBodyPart(ShinR);
        m_JdController.SetupBodyPart(FootR);

        m_JdController.SetupBodyPart(Chest);
        m_JdController.SetupBodyPart(Core);
        m_JdController.SetupBodyPart(Hips);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var bpDict = m_JdController.bodyPartsDict;

        bpDict[UpperArmL].SetJointTargetRotation(ux, uy, 0);
        bpDict[ForeArmL].SetJointTargetRotation(fx, 0, 0);
        bpDict[UpperArmR].SetJointTargetRotation(ux, -uy, 0);
        bpDict[ForeArmR].SetJointTargetRotation(fx, 0, 0);

        bpDict[ThighL].SetJointTargetRotation(tx, ty, 0);
        bpDict[ShinL].SetJointTargetRotation(sx, 0, 0);
        bpDict[FootL].SetJointTargetRotation(fox, foy, 0);
        bpDict[ThighR].SetJointTargetRotation(tx, ty, 0);
        bpDict[ShinR].SetJointTargetRotation(sx, 0, 0);
        bpDict[FootR].SetJointTargetRotation(fox, foy, 0);

        bpDict[Chest].SetJointTargetRotation(cx, cy, cz);
        bpDict[Core].SetJointTargetRotation(cox, coy, coz);

        bpDict[UpperArmL].SetJointStrength(strength);
        bpDict[ForeArmL].SetJointStrength(strength);
        bpDict[UpperArmR].SetJointStrength(strength);
        bpDict[ForeArmR].SetJointStrength(strength);

        bpDict[ThighL].SetJointStrength(strength);
        bpDict[ShinL].SetJointStrength(strength);
        bpDict[FootL].SetJointStrength(strength);
        bpDict[ThighR].SetJointStrength(strength);
        bpDict[ShinR].SetJointStrength(strength);
        bpDict[FootR].SetJointStrength(strength);


        bpDict[Chest].SetJointStrength(strength);
        bpDict[Core].SetJointStrength(strength);
    }
}
