using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink
{
    [SerializeField] int rootPosition = 0;
    [SerializeField] int rootRotation = 0;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine)
        {
            stateMachine.AddRootMotionRequest(rootPosition, rootRotation);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateMachine)
            stateMachine.AddRootMotionRequest(-rootPosition, -rootRotation);
    }
}
