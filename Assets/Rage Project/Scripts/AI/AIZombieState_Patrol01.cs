using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
/*
 ---------------------------------------------------------------
 |  Class       : AIZombieState_Patrol01                       |
 |  Description : An AIState that implements a zombies Patrol  |
 |                Behaviour                                    |
 ---------------------------------------------------------------
 */
public class AIZombieState_Patrol01 : AIZombieState
{
    //Inspector Assigned
    [SerializeField] float             _turnOnspotThreshold = 90.0f;
    [SerializeField] float             _slerpSpeed          = 5.0f;

    [SerializeField] [Range(0.0f, 3.0f)]float _speed    = 0.0f;
    /*
    ---------------------------------------------------------------
    |  Name        : GetStateType                                 |
    |  Description : Returns the type of the state                |
    ---------------------------------------------------------------
    */
    public override AIStateType GetStateType() { return AIStateType.Patrol; }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnEnterState                                            |
    |  Description : Called by the State Machine when first transitioned into|
    |                this state.                                             |
    --------------------------------------------------------------------------
    */
    public override void OnEnterState()
    {
        Debug.Log("enter Patrol state");
        base.OnEnterState();

        if (_zombieStateMachine == null) return;

        //configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        //set destination
        _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        //make sure nav agent is turned on
        _zombieStateMachine.navMeshAgent.isStopped = false;
    }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnUpdateState                                           |
    |  Description : Called by the state machine each frame to give this     |
	|                state a time-slice to update itself. It processes       |
	|                threats and handles transitions as well as keeping      |
	|                the zombie aligned with its proper direction in the     |
	|                case where root rotation isn't being used.              |
    --------------------------------------------------------------------------
    */
    public override AIStateType OnUpdateState()
    {
        if (_zombieStateMachine == null) return AIStateType.Patrol;
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
            if((1.0f-_zombieStateMachine.satisfaction) > (_zombieStateMachine.VisualThreat.distance/_zombieStateMachine.sensorRadius))
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        if(_zombieStateMachine.navMeshAgent.pathPending)
        {
            _zombieStateMachine.speed = 0;
            return AIStateType.Patrol;
        }
        else
        {
            _zombieStateMachine.speed = _speed;
        }

        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, 
                                    (_zombieStateMachine.navMeshAgent.steeringTarget - _zombieStateMachine.transform.position)
                                    );
        if (angle > _turnOnspotThreshold) return AIStateType.Alerted;

        if(!_zombieStateMachine.useRootRotation)
        {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navMeshAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        if(_zombieStateMachine.navMeshAgent.isPathStale ||
           !_zombieStateMachine.navMeshAgent.hasPath ||
           _zombieStateMachine.navMeshAgent.pathStatus!= NavMeshPathStatus.PathComplete)
        {
            _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }

        return AIStateType.Patrol;
    }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnDestinationReached                                    |
    |  Description : Called by the parent StateMachine when the zombie has   |
	|			     reached its target (entered its target trigger)         |
    --------------------------------------------------------------------------
    */
    public override void OnDestinationReached(bool isReached) 
    {
        if (_zombieStateMachine == null || !isReached) return;

        if(_zombieStateMachine.targetType == AITargetType.Waypoint)
        {
            _zombieStateMachine.GetWaypointPosition(true);
        }
    }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnAnimatorIKUpdate                                      |
    |  Description : Override IK Goals                                       |
    --------------------------------------------------------------------------
    */
    //public override void OnAnimatorIKUpdate()
    //{
    //    if (_zombieStateMachine == null) { return; }
    //    else
    //    {
    //        _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
    //        _zombieStateMachine.animator.SetLookAtWeight(0.55f);
    //    }
    //}
}
