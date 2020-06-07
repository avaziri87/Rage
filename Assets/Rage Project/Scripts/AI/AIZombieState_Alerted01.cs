using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 ---------------------------------------------------------------
 |  Class       : AIZombieState_Alerted01                      |
 |  Description : An AIState that implements a zombies Alert   |
 |                Behaviour                                    |
 ---------------------------------------------------------------
 */
public class AIZombieState_Alerted01 : AIZombieState
{
    //Inspector Assigned
    [SerializeField] [Range(1.0f,60.0f)] float _maxDuration             = 10.0f;
    [SerializeField]                     float _waypointAngleThreshold  = 90.0f;
    [SerializeField]                     float _threatAngleThreshold    = 10.0f;
    [SerializeField]                     float _directionChangeTime     = 1.5f;
    [SerializeField]                     float _slerpSpeed              = 45.0f;

    //private
    float _timer = 0;
    float _directionChangeTimer = 0.0f;
    float _screamChance = 0.0f;
    float _nextScream = 0.0f;
    float _screamFrecuency = 120.0f;
    /*
    --------------------------------------------------------------------------
    |  Name        : GetStateType                                            |
    |  Description : Returns the type of state                               |
    --------------------------------------------------------------------------
    */
    public override AIStateType GetStateType() { return AIStateType.Alerted; }
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
        Debug.Log("enter Alerted state");
        base.OnEnterState();

        if (_zombieStateMachine == null) return;

        //configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0.0f;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _timer = _maxDuration;
        _directionChangeTimer = 0.0f;

        _screamChance = _zombieStateMachine.ScreamChance - Random.value;
    }
    /*
    --------------------------------------------------------------------------
    |  Name        : OnUpdateState                                           |
    |  Description : Called by the state machine each frame                  |
    --------------------------------------------------------------------------
    */
    public override AIStateType OnUpdateState()
    {
        if (_zombieStateMachine == null) return AIStateType.Alerted;
        //timer reduction
        _timer -= Time.deltaTime;
        _directionChangeTimer += Time.deltaTime;

        //once the timer reaches 0 zombies enters patrol state
        if (_timer <= 0.0f)
        {
            _zombieStateMachine.navMeshAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navMeshAgent.isStopped = false;
            _timer = _maxDuration;
        }

        //do we have visual of the player
        if(_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);

            if(_screamChance > 0.0f && Time.time > _nextScream)
            {
                if(_zombieStateMachine.Scream())
                {
                    _screamChance = float.MinValue;
                    _nextScream = Time.time + _screamFrecuency;
                    return AIStateType.Alerted;
                }
            }
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            _timer = _maxDuration;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Lights)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            _timer = _maxDuration;
        }

        if(_zombieStateMachine.AudioThreat.type == AITargetType.None &&
           _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food &&
           _zombieStateMachine.targetType == AITargetType.None)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        float angle;

        if((_zombieStateMachine.targetType == AITargetType.Audio ||
           _zombieStateMachine.targetType == AITargetType.Visual_Lights) &&
           !_zombieStateMachine.isTargetReached)
        {
            angle = AIState.FindSinedAngle(_zombieStateMachine.transform.forward,
                                         (_zombieStateMachine.targetPosition - _zombieStateMachine.transform.position));
            if(_zombieStateMachine.targetType == AITargetType.Audio && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }
            if(_directionChangeTimer > _directionChangeTime)
            {
                if (Random.value < _zombieStateMachine.intelligence)
                {
                    _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                }
                else
                {
                    _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                }
                _directionChangeTimer = 0.0f;
            }

        }
        else if(_zombieStateMachine.targetType == AITargetType.Waypoint && !_zombieStateMachine.navMeshAgent.pathPending)
        {
            angle = AIState.FindSinedAngle(_zombieStateMachine.transform.forward,
                                         _zombieStateMachine.navMeshAgent.steeringTarget - _zombieStateMachine.transform.position);
            if (Mathf.Abs(angle) < _waypointAngleThreshold)
            {
                return AIStateType.Patrol;
            }
            if(_directionChangeTimer> _directionChangeTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                _directionChangeTimer = 0.0f;
            }
        }
        else
        {
            if (_directionChangeTimer > _directionChangeTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                _directionChangeTimer = 0.0f;
            }
        }

        if (!_zombieStateMachine.useRootRotation) 
            _zombieStateMachine.transform.Rotate(new Vector3(0.0f, _slerpSpeed * _zombieStateMachine.seeking*Time.deltaTime, 0.0f));
        return AIStateType.Alerted;
    }
}
