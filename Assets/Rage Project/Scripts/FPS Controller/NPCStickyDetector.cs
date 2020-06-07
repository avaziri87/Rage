using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCStickyDetector : MonoBehaviour
{
    FPSController _fPSController = null;

    private void Start()
    {
        _fPSController = GetComponentInParent<FPSController>();
    }

    private void OnTriggerStay(Collider other)
    {
        AIStateMachine machine = GameSceneManager.instance.GetAIStateMachine(other.GetInstanceID());
        if(machine != null && _fPSController != null)
        {
            _fPSController.DoStickiness();
            machine.VisualThreat.Set(AITargetType.Visual_Player, 
                                        _fPSController.characterController, 
                                        _fPSController.transform.position, 
                                        Vector3.Distance(machine.transform.position, 
                                        _fPSController.transform.position)
                                    );
            machine.SetStateOverride(AIStateType.Attack);
        }
    }
}
