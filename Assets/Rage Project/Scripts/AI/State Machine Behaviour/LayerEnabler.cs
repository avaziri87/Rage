using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerEnabler : AIStateMachineLink
{
    public bool     onEnter     = false;
    public bool     onExit      = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
            _stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), onEnter);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
            _stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), onExit);
    }
}
