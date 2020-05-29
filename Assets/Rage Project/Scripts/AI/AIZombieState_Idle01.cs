using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 ---------------------------------------------------------------
 |  Class       : AIZombieState_Idle01                         |
 |  Description : An AIState that implements a zombies Idle    |
 |                Behaviour                                    |
 ---------------------------------------------------------------
 */
public class AIZombieState_Idle01 : AIZombieState
{
    //Inspector Assigned
    [SerializeField] Vector2 _idleTimeRange = new Vector2(10.0f, 60.0f);

    //Private
    float _idleTime = 0.0f;
    float _timer    = 0.0f;

    /*
    ---------------------------------------------------------------
    |  Name        : GetStateType                                 |
    |  Description : Returns the type of the state                |
    ---------------------------------------------------------------
    */
    public override AIStateType GetStateType() { return AIStateType.Idle; }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnEnterState                                            |
    |  Description : Called by the State Machine when first transitioned into|
    |                this state. It initializes a timer and configures the   |
    |                the state machine                                       |
    --------------------------------------------------------------------------
    */
    public override void OnEnterState() 
    {
        Debug.Log("enter Idle state");
        base.OnEnterState();
        
        if (_zombieStateMachine == null) return;

        _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _timer    = 0.0f;

        //configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed   = 0.0f;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _zombieStateMachine.ClearTarget();
    }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnUpdateState                                           |
    |  Description : Called by the state machine each frame                  |
    --------------------------------------------------------------------------
    */
    public override AIStateType OnUpdateState()
    {
        if (_zombieStateMachine == null) return AIStateType.Idle;
        // Is the player visible
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }
        // Is the threat a flashlight
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Lights)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }
        // Is the threat an audio emitter
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }
        // Is the threat food
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }
        // Update the idle timer
        _timer += Time.deltaTime;
        // Patrol if idle time has been exceeded
        if (_timer > _idleTime)
        {
            _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navMeshAgent.isStopped = false;
            return AIStateType.Alerted;
        }

        return AIStateType.Idle;
    }
}
