using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 --------------------------------------------------------------------------------------------------------------
 |  Class       : AISensor                                                                                    |
 |  Description : enter its trigger via the AIStateMachine's OnTriggerEvent method                            |
 --------------------------------------------------------------------------------------------------------------
 */
public class AISensor : MonoBehaviour
{
    //private
    AIStateMachine _parentStateMachine = null;
    public AIStateMachine ParentStateMachine { set { _parentStateMachine = value; } }

    private void OnTriggerEnter(Collider other)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, other);
    }
    private void OnTriggerStay(Collider other)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, other);
    }
    private void OnTriggerExit(Collider other)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, other);
    }
}
