using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink
{
    [SerializeField] int rootPosition = 0;
    [SerializeField] int rootRotation = 0;

    bool _rootMotionProcessed = false;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.AddRootMotionRequest(rootPosition, rootRotation);
            _rootMotionProcessed = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine && _rootMotionProcessed)
        {
            _stateMachine.AddRootMotionRequest(-rootPosition, -rootRotation);
            _rootMotionProcessed = false;
        }
    }
}
