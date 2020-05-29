using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
/*
---------------------------------------------------------------
|  Class       : AIZombieState_Pursuit01                      |
|  Description : An AIState that implements a zombies Alert   |
|                Behaviour                                    |
---------------------------------------------------------------
*/
public class AIZombieState_Pursuit01 : AIZombieState
{
    //inspector assigned
    [SerializeField] [Range(0, 10)] float _speed                    = 1.0f;
    [SerializeField]                float _slerpSpeed               = 5.0f;
    [SerializeField]                float _repathDistanceMultiplier = 0.035f;
    [SerializeField]                float _repathVisualMinDuration  = 0.05f;
    [SerializeField]                float _repathVisualMaxDuration  = 5.0f;
    [SerializeField]                float _repathAudioMinDuration   = 0.05f;
    [SerializeField]                float _repathAudioMaxDuration   = 5.0f;
    [SerializeField]                float _maxDuration              = 40.0f;

    //private variables
    float _timer         = 0.0f;
    float _repathTimer   = 0.0f;
    /*
    ---------------------------------------------------------------
    |  Name        : GetStateType                                 |
    |  Description : Returns the type of the state                |
    ---------------------------------------------------------------
    */
    public override AIStateType GetStateType() { return AIStateType.Pursuit; }

    /*
    -------------------------------------------------------------------
    |  Name        : OnEnterState                                     |
    |  Description : set up the base values of the class when entered |
    -------------------------------------------------------------------
    */
    public override void OnEnterState()
    {
        Debug.Log("enter Pursuit state");
        base.OnEnterState();

        if (_zombieStateMachine == null) return;

        //configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking     = 0;
        _zombieStateMachine.feeding     = false;
        _zombieStateMachine.attackType  = 0;

        //zombie pursuit for a set timer before breaking off
        _timer          = 0.0f;
        _repathTimer    = 0.0f;

        //set path
        _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.targetPosition);
        _zombieStateMachine.navMeshAgent.isStopped = false;
    }
    /*
    --------------------------------------------------------------------
    |  Name        : OnUpdateState                                     |
    |  Description : keep track if we are in our target trigger or not |
    --------------------------------------------------------------------
    */
    public override AIStateType OnUpdateState()
    {
        _timer       += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        if (_timer > _maxDuration) return AIStateType.Patrol;

        //if we are chasing the player and have entered the melee trigger then attack
        if(_stateMachine.targetType== AITargetType.Visual_Player && _zombieStateMachine.inMeleeRange)
        {
            return AIStateType.Attack;
        }

        //otherwise this is nagivation to areas of interest so use the standard target threshold
        if(_zombieStateMachine.isTargetReached)
        {
            switch(_stateMachine.targetType)
            {
                //if we have reached the source
                case AITargetType.Audio:
                case AITargetType.Visual_Lights:
                    _stateMachine.ClearTarget();    //clear the target
                    return AIStateType.Alerted;     //become alert and scna for targets
                case AITargetType.Visual_Food:
                    return AIStateType.Feeding;
            }
        }

        //if for any reason the nav agent has los its path then call then drop into alerted state
        //so it will try to re-aquired the target or eventually give up and resume patrolling
        if(_zombieStateMachine.navMeshAgent.isPathStale ||
          (!_zombieStateMachine.navMeshAgent.hasPath && !_zombieStateMachine.navMeshAgent.pathPending) ||
           _zombieStateMachine.navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete )
        {
            return AIStateType.Alerted;
        }
        if(_zombieStateMachine.navMeshAgent.pathPending)
        {
            _zombieStateMachine.speed = 0;
        }
        else
        {
            _zombieStateMachine.speed = _speed;
            //if we are close o the target that is a player and we still have the player in our vision then keep facing the player
            if (!_zombieStateMachine.useRootRotation &&
                _zombieStateMachine.targetType == AITargetType.Visual_Player
                && _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player
                && _zombieStateMachine.isTargetReached)
            {
                Vector3 targetposition = _zombieStateMachine.targetPosition;
                targetposition.y = _zombieStateMachine.transform.position.y;
                Quaternion newRot = Quaternion.LookRotation(targetposition - _zombieStateMachine.transform.position);
                _zombieStateMachine.transform.rotation = newRot;
            }
            else //slowly update our rotation to match the nav agents desired rotation BUT only if we are not persuing the player
            if (!_zombieStateMachine.useRootRotation && !_zombieStateMachine.isTargetReached)
            {
                //generte new quaternion representing the rotation we should have
                Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navMeshAgent.desiredVelocity);
                //smoothly rotate that new rotation over time
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime*_slerpSpeed);
            }
            else if(_zombieStateMachine.isTargetReached)
            {
                return AIStateType.Alerted;
            }
        }

        //do we have a visual threat that is the player
        if(_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            //the position is different - maybe same threat but it has moved so repath periodically
            if(_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position)
            {
                //repath more frequently as we get closer to the target(try and save CPU cycles)
                if(Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier,_repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                {
                    //Repath the target
                    _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                    _repathTimer = 0.0f;
                }
            }
            //make sure this is the current target
            _stateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        //if our target is the last sighting of a player then remain in pursuit
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player) return AIStateType.Pursuit;

        //if we have a vsual threat that is the player light
        if(_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Lights)
        {
            //and we have a lower priority, drop into alerted state and find the source of light
            if(_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }
            else if(_zombieStateMachine.targetType ==AITargetType.Visual_Lights)
            {
                //get unique Id of the collider of our target
                int currentID = _zombieStateMachine.targetColliderID;
                //if the target is the same light
                if(currentID == _zombieStateMachine.VisualThreat.collider.GetInstanceID())
                {
                    //the position is different - maybe same threat but it has moved so repath periodically
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position)
                    {
                        //repath more frequently as we get closer to the target(try and save CPU cycles)
                        if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                        {
                            //Repath the target
                            _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }
                    _stateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Pursuit;
                }
                else
                {
                    _stateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Pursuit;
                }
            }
        }
        else if(_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            if(_zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }
            else if(_zombieStateMachine.targetType ==AITargetType.Audio)
            {
                //get unique Id of the collider of our target
                int currentID = _zombieStateMachine.targetColliderID;
                //if the target is the same sound
                if(currentID == _zombieStateMachine.AudioThreat.collider.GetInstanceID())
                {
                    //the position is different - maybe same threat but it has moved so repath periodically
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.AudioThreat.position)
                    {
                        //repath more frequently as we get closer to the target(try and save CPU cycles)
                        if (Mathf.Clamp(_zombieStateMachine.AudioThreat.distance * _repathDistanceMultiplier, _repathAudioMinDuration, _repathAudioMaxDuration) < _repathTimer)
                        {
                            //Repath the target
                            _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }
                    _stateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Pursuit;
                }
                else
                {
                    _stateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Pursuit;
                }
            }
        }

        //default
        return AIStateType.Pursuit;
    }
}
