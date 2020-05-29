using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 --------------------------------------------------------------------------------------------------------------
 |  Class       : AIZombieState                                                                               |
 |  Description : The immediate base class of all zombies states. It provides the event processing and storage|
 |                of the current events                                                                       |
 --------------------------------------------------------------------------------------------------------------
 */
public abstract class AIZombieState : AIState
{
    //protected
    protected int                  _playerLayerMask    = -1;
    protected int                  _bodyPartLayer      = -1;
    protected int                  _visualLayerMask    = -1;
    protected AIZombieStateMachine _zombieStateMachine = null;

    /*
    -----------------------------------------------------------------------------------------------------------
    |  Name        : Awake                                                                                    |
    |  Description : Calculate the masks and layers used for raycasting and layer testing                     |
    -----------------------------------------------------------------------------------------------------------
    */
    private void Awake()
    {
        //Get a mask for line of sight testing with the player. (+1) is a hack to include the default layer
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part")+1;
        //Get the layer index of the AI body part layer
        _bodyPartLayer   = LayerMask.NameToLayer("AI Body Part");
        _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1;
    }
    public override void SetStateMachine(AIStateMachine stateMachine)
    {
        if(stateMachine.GetType() == typeof(AIZombieStateMachine))
        {
            base.SetStateMachine(stateMachine);
            _zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }
    /*
    -----------------------------------------------------------------------------------------------------------
    |  Name        : OnTriggerEvent                                                                           |
    |  Description : Called by the parent state machine when threats enter/stay/exit the zombie's sensor      |
    |                trigger, this will be any colliders assigned to the visual or audio aggravator layers or |
    |                the player. It examines the threat and stores it in  the parent machine visual or audio  |
    |                threat members if found to be a higher priority threat.                                  |
    -----------------------------------------------------------------------------------------------------------
    */
    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        if (_zombieStateMachine == null) return;
        if(eventType != AITriggerEventType.Exit)
        {
            //what is the type of the current visual threat we have stored
            AITargetType curType = _zombieStateMachine.VisualThreat.type;

            //is the collider that entered our sensor the player
            if(other.CompareTag("Player"))
            {
                //get distance the sensor origin to the collider
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);
                if(curType!=AITargetType.Visual_Player ||
                  (curType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.distance))
                {
                    //is the collider within our fov and line of sight
                    RaycastHit hitInfo;
                    if(ColliderIsVisible(other, out hitInfo, _playerLayerMask))
                    {
                        //yes it is close and in FOV, store as current most dangerous threat
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            }
            else if(other.CompareTag("Flash Light") && curType != AITargetType.Visual_Player)
            {
                BoxCollider flashlightTrigger = (BoxCollider)other;

                float distanceToThreat   = Vector3.Distance(_zombieStateMachine.sensorPosition, flashlightTrigger.transform.position);
                float zSize              = flashlightTrigger.size.z * transform.lossyScale.z;
                float aggravatioinFactor = distanceToThreat / zSize;
                if(aggravatioinFactor <= _zombieStateMachine.intelligence && 
                   aggravatioinFactor <= _zombieStateMachine.sight)
                {
                    _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Lights, other, other.transform.position, distanceToThreat);
                }
            }
            else if(other.CompareTag("AI Sound Emitter"))
            {
                SphereCollider soundTrigger = (SphereCollider)other;
                if (soundTrigger == null) return;

                //get agent sensor position
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;
                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldspace(soundTrigger, out soundPos, out soundRadius);

                //get how far inside the sound's radius are we
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;
                float distanceFactor   = distanceToThreat / soundRadius;
                distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.hearing);

                if (distanceFactor > 1.0f) return;
                if(distanceToThreat < _zombieStateMachine.AudioThreat.distance || _zombieStateMachine.AudioThreat.distance <= 0)
                {
                    _zombieStateMachine.AudioThreat.Set(AITargetType.Audio, other, soundPos, distanceToThreat);
                }
            }
            else if (other.CompareTag("AI Food") &&
                     curType!=AITargetType.Visual_Player &&
                     curType!= AITargetType.Visual_Lights &&
                     _zombieStateMachine.AudioThreat.type == AITargetType.None &&
                     _zombieStateMachine.satisfaction <= 0.9f)
            {
                float distanceToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.transform.position);
                if(distanceToThreat < _zombieStateMachine.VisualThreat.distance || _zombieStateMachine.VisualThreat.distance <= 0)
                {
                    RaycastHit hitInfo;
                    if(ColliderIsVisible(other, out hitInfo, _visualLayerMask))
                    {
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food, other, other.transform.position, distanceToThreat);
                    }
                }
            }
        }
    }
    /*
    -----------------------------------------------------------------------------------------------------------
    |  Name        : ColliderIsVisible                                                                        |
    |  Description : Test the passed collider against zombie's FOV and using the passed layer mask for line of|
    |                sight.                                                                                    |
    -----------------------------------------------------------------------------------------------------------
    */
    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1)
    {
        hitInfo = new RaycastHit();
        //make sure state machine is AIZombieStateMachine
        if (_zombieStateMachine == null) return false;

        //get the angle between the sensor origin  and collider direction
        Vector3 head      = _zombieStateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float   angle     = Vector3.Angle(direction, transform.forward);

        //if angle is greater than 1/2 of FOV the it is outside of FOV, return false
        if (angle > _zombieStateMachine.fov * 0.5f) return false;

        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

        //Find the closest collider that is NOT the AI Own body part. if its not the target then the target is obstructed
        float    closestColliderDistance = float.MaxValue;
        Collider closestCollider         = null;

        for(int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if(hit.distance < closestColliderDistance)
            {
                if(hit.transform.gameObject.layer == _bodyPartLayer)
                {
                    if(_stateMachine != GameSceneManager.instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                    {
                        closestColliderDistance = hit.distance;
                        closestCollider         = hit.collider;
                        hitInfo                 = hit;
                    }
                }
                else
                {
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        if (closestCollider != null && closestCollider.gameObject == other.gameObject) return true;

        return false;

    }
}
