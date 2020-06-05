using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
---------------------------------------------------------------
|  Class       : AIZombieState_Feeding01                      |
|  Description : An AIState that implements a zombies Alert   |
|                Behaviour                                    |
---------------------------------------------------------------
*/
public class AIZombieState_Feeding01 : AIZombieState
{
    //inspector assigned
    [SerializeField]                        float       _slerpSpeed                 = 5.0f;
    [SerializeField]                        Transform   _bloodPSMount               = null;
    [SerializeField] [Range(0.01f, 1.0f)]   float       _bloodParticlesBurstTime    = 0.1f;
    [SerializeField] [Range(1, 100)]    int         _bloodParticlesBurstAmount  = 10;

    //private
    int     _eatingStateHas         = Animator.StringToHash("Feeding State");
    int     _crawlEatingStateHas    = Animator.StringToHash("Crawl Feeding State");
    int     _eatingLayerIndex       = -1;
    float   _bloodTimer             = 0.0f;

    public override AIStateType GetStateType() { return AIStateType.Feeding; }
    /*
    -------------------------------------------------------------------
    |  Name        : OnEnterState                                     |
    |  Description : set up the base values of the class when entered |
    -------------------------------------------------------------------
    */
    public override void OnEnterState()
    {
        //base class processing
        Debug.Log("Enter Feeding State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        //get index layer
        if (_eatingLayerIndex == -1)
            _eatingLayerIndex = _zombieStateMachine.animator.GetLayerIndex("Cinematic");

        _bloodTimer = 0.0f;

        //configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed       = 0;
        _zombieStateMachine.seeking     = 0;
        _zombieStateMachine.feeding     = true;
        _zombieStateMachine.attackType  = 0;
    }

    /*
    -------------------------------------------------------------------
    |  Name        : OnExitState                                      |
    |  Description : exits the state and resets the feeding to false  |
    -------------------------------------------------------------------
    */
    public override void OnExitState()
    {
        if (_zombieStateMachine != null) _zombieStateMachine.feeding = false;
    }
    /*
    -------------------------------------------------------------------
    |  Name        : OnUpdateState                                    |
    |  Description : set up the base values of the class when entered |
    -------------------------------------------------------------------
    */
    public override AIStateType OnUpdateState()
    {
        _bloodTimer += Time.deltaTime;
        //if zombie has feed enough drop to alerted state
        if(_zombieStateMachine.satisfaction > 0.9f)
        {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }

        //if visual threat then drop to alerted state
        if(_zombieStateMachine.VisualThreat.type != AITargetType.None &&_zombieStateMachine.VisualThreat.type != AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        //if audio threat then drop to alerted state
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        //if feeding animation playing now
        int currentHash = _zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash;
        if (currentHash == _eatingStateHas || currentHash == _crawlEatingStateHas)
        {
            _zombieStateMachine.satisfaction = Mathf.Min(_zombieStateMachine.satisfaction + (Time.deltaTime*_zombieStateMachine.replenishRate)/100, 1.0f);
            if(GameSceneManager.instance && GameSceneManager.instance.bloodParticle &&_bloodPSMount)
            {
                if(_bloodTimer > _bloodParticlesBurstTime)
                {
                    ParticleSystem system = GameSceneManager.instance.bloodParticle;
                    system.transform.position = _bloodPSMount.transform.position;
                    system.transform.rotation = _bloodPSMount.transform.rotation;
                    var main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    system.Emit(_bloodParticlesBurstAmount);
                    _bloodTimer = 0.0f;
                }
            }
        }

        if(!_zombieStateMachine.useRootRotation)
        {
            Vector3 targerPos = _zombieStateMachine.targetPosition;
            targerPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targerPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        Vector3 headToTarget = _zombieStateMachine.targetPosition - _zombieStateMachine.animator.GetBoneTransform(HumanBodyBones.Head).position;
        _zombieStateMachine.transform.position = Vector3.Lerp(transform.position, transform.position + headToTarget, Time.deltaTime);


        //stay feeding
        return AIStateType.Feeding;
    }
}
