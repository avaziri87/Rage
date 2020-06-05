using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 ---------------------------------------------------------------
 |  Class       : AIState                                      |
 |  Description : Base class of all AI States used by AI system|
 ---------------------------------------------------------------
 */
public abstract class AIState : MonoBehaviour
{
    //public method called by the parent state machine to assigne its refference
    public virtual void SetStateMachine(AIStateMachine stateMachine) { _stateMachine = stateMachine; }

    //Ddefault Handlers
    public virtual void         OnEnterState()       { }
    public virtual void         OnExitState()        { }
    public virtual void         OnAnimatorIKUpdate() { }
    public virtual void         OnTriggerEvent(AITriggerEventType eventType, Collider collider) { }
    public virtual void         OnDestinationReached(bool isReached) { }

    //Abstract Methods
    public abstract AIStateType GetStateType();
    public abstract AIStateType OnUpdateState();

    //Protected Fields
    protected AIStateMachine _stateMachine;
    /*
    ---------------------------------------------------------------
    |  Name        : OnAnimatorUpdated                            |
    |  Description : called by parent state machine to allow root |
    |                motion processing                            |
    ---------------------------------------------------------------
    */
    public virtual void OnAnimatorUpdated()
    {
        if (_stateMachine.useRootPosition)
            _stateMachine.navMeshAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;
        if (_stateMachine.useRootRotation)
            _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;
    }
    /*
    ---------------------------------------------------------------------------
    |  Name        : ConvertSphereColliderToWorldspace                        |
    |  Description : Converts the passed sphere collider's position and radius|
    |                into world space taking into acount hierarchical scaling |
    ---------------------------------------------------------------------------
    */
    public static void ConvertSphereColliderToWorldspace(SphereCollider col, out Vector3 pos, out float radius)
    {
        //Default values
        pos     = Vector3.zero;
        radius  = 0.0f;

        if (col == null) return;

        //calculate world space position of sphere center
        pos    = col.transform.position;
        pos.x += col.center.x * col.transform.lossyScale.x;
        pos.y += col.center.y * col.transform.lossyScale.y;
        pos.z += col.center.z * col.transform.lossyScale.z;

        // calculate world space radius of sphere center
        radius = Mathf.Max(col.radius * col.transform.lossyScale.x,
                           col.radius * col.transform.lossyScale.y);
        radius = Mathf.Max(radius, col.radius * col.transform.lossyScale.z);
    }
    /*
    ---------------------------------------------------------------------------
    |  Name        : FindSinAngle                                             |
    |  Description : returns Sin angle between two vectors in Degrees         |
    ---------------------------------------------------------------------------
    */
    public static float FindSinedAngle(Vector3 from, Vector3 to)
    {
        if (from == to) return 0.0f;

        float angle   = Vector3.Angle(from, to);
        Vector3 cross = Vector3.Cross(from, to);
        angle        *= Mathf.Sign(cross.y);
        return angle;
    }
}
