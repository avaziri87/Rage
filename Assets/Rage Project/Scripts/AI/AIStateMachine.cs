using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Public Enums of the AI System
public enum AIStateType     { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead}
public enum AITargetType    { None, Waypoint, Visual_Player , Visual_Lights, Visual_Food, Audio}
public enum AITriggerEventType { Enter, Stay, Exit}
/*
 ---------------------------------------------------------------
 |  Class       : AITarget                                     |
 |  Description : Describe a potencial target to the AI system |
 ---------------------------------------------------------------
 */
public struct AITarget
{
    AITargetType _type;
    Collider     _collider;
    Vector3      _position;
    float        _distance;
    float        _time;

    public AITargetType type        { get { return _type; } }
    public Collider     collider    { get { return _collider; } }
    public Vector3      position    { get { return _position; } }
    public float        distance    { get { return _distance; } set { _distance = value; } }
    public float        time        { get { return _time; } }

    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = UnityEngine.Time.time;
    }
    public void Clear()
    {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _distance = 0.0f;
        _time = Mathf.Infinity;
    }
}
/*
 ---------------------------------------------------------------
 |  Class       : AIStateMachine                               |
 |  Description : Base class for all state machines            |
 ---------------------------------------------------------------
 */
public abstract class AIStateMachine : MonoBehaviour
{
    //Public
    public AITarget VisualThreat = new AITarget();
    public AITarget AudioThreat = new AITarget();

    //Protected Inspector Assigned
    [SerializeField] protected AIStateType          _currentStateType   = AIStateType.Idle;
    [SerializeField] protected SphereCollider       _targetTrigger      = null;
    [SerializeField] protected SphereCollider       _sensorTrigger      = null;
    [SerializeField] protected AIWaypointNetwork    _waypointNetwork    = null;
    [SerializeField] protected bool                 _randomPatrol       = false;
    [SerializeField] protected int                  _currentWaypoint    = -1;

    [SerializeField] [Range(0, 15)] protected float _stopingDistance = 1.0f;

    //Protected
    protected AIState       _currentState                       = null;
    protected Dictionary<AIStateType, AIState> _statesDicionary = new Dictionary<AIStateType, AIState>();
    protected AITarget      _target                             = new AITarget();
    protected int           _rootPositionRefCount               = 0;
    protected int           _rootRotationRefCount               = 0;
    protected bool          _isTargetReached                    = false;

    //Component Cache
    protected Animator      _animator                           = null;
    protected NavMeshAgent  _navMeshAgent                       = null;
    protected Collider      _collider                           = null;
    protected Transform     _transform                          = null;

    //Public properties
    public bool         inMeleeRange    { get; set; }
    public Animator     animator        { get { return _animator; } }
    public NavMeshAgent navMeshAgent    { get { return _navMeshAgent; } }
    public bool         useRootPosition { get { return _rootPositionRefCount > 0; } }
    public bool         useRootRotation { get { return _rootRotationRefCount > 0; } }
    public AITargetType targetType      { get { return _target.type; } }
    public Vector3      targetPosition  { get { return _target.position; } }
    public bool         isTargetReached { get { return _isTargetReached; } }
    public int          targetColliderID
    {
        get
        {
            if (_target.collider)
                return _target.collider.GetInstanceID();
            else
                return -1;
        }
    }
    public Vector3      sensorPosition
    {
        get
        {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point = _sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }
    public float        sensorRadius
    {
        get
        {
            if (_sensorTrigger == null) return 0.0f;
            float radius = Mathf.Max(   _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x,
                                        _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);
             radius = Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
            return radius;
        }
    }
    /*
    ----------------------------------------------------------------
    |  Name        : Awake                                         |
    |  Description : Cache components                              |
    ----------------------------------------------------------------
    */
    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
        if(GameSceneManager.instance != null)
        {
            if (_collider) GameSceneManager.instance.RegisterAIStatemachine(_collider.GetInstanceID(), this);
            if (_sensorTrigger) GameSceneManager.instance.RegisterAIStatemachine(_sensorTrigger.GetInstanceID(), this);
        }

    }
    /*
    ----------------------------------------------------------------
    |  Name        : Start                                         |
    |  Description : Called by Unity prior to first Update to setup|
    |                the object                                    |
    ----------------------------------------------------------------
    */
    protected virtual void Start()
    {
        if(_sensorTrigger != null)
        {
            AISensor script = _sensorTrigger.GetComponent<AISensor>();
            if(script != null)
            { 
                script.ParentStateMachine = this;
            }
        }
        AIState[] states = GetComponents<AIState>();
        foreach(AIState state in states)
        {
            if(state != null && !_statesDicionary.ContainsKey(state.GetStateType()))
            {
                _statesDicionary[state.GetStateType()] = state;
                state.SetStateMachine(this);
            }
        }
        if (_statesDicionary.ContainsKey(_currentStateType))
        {
            _currentState = _statesDicionary[_currentStateType];
            _currentState.OnEnterState();
        }
        else
        {
            _currentState = null;
        }
        if(_animator)
        {
            AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
            foreach (AIStateMachineLink script in scripts)
            {
                script.StateMachine = this;
            }
        }
    }
    public Vector3 GetWaypointPosition(bool increment)
    {
        if(_currentWaypoint ==-1)
        {
            if (_randomPatrol)
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            else
                _currentWaypoint = 0;
        }
        else
        {
            if (increment)
                NextWaypoint();
        }


        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];
            SetTarget(AITargetType.Waypoint,
                                          null,
                                          newWaypoint.position,
                                          Vector3.Distance(newWaypoint.position, transform.position)
                                          );
        return newWaypoint.position;
        }
        return Vector3.zero;
    }
    /*
    ---------------------------------------------------------------------------------
    |  Name        : NextWaypoint                                                   |
    |  Description : Called to select a new waypoint. Either randomly selects a new |
    |                waypoint from the waypoint network or increments the current   |
    |                waypoint index (with wrap-around) to visit the waypoints in    |
    |                the network in sequence. Sets the new waypoint as the the      |
    |                target and generates a nav agent path for it                   |      
    ---------------------------------------------------------------------------------
    */
    private void NextWaypoint()
    {
        // increase the current waypoint with wrap-around to zero, or chose random waypoint
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = UnityEngine.Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
        {
            _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
        }
    }
    /*
    ----------------------------------------------------------------
    |  Name        : SetTarget (Overload)                          |
    |  Description : Sets the current target and configures the    |
    |                target triggers                               |
    ----------------------------------------------------------------
    */
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        //Set target info
        _target.Set(t, c, p, d);

        //Configures and enables the target trigger at the correct
        //position and with the correct radius
        if(_targetTrigger != null)
        {
            _targetTrigger.radius = _stopingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    /*
    ----------------------------------------------------------------
    |  Name        : SetTarget (Overload)                          |
    |  Description : Sets the current target and configures the    |
    |                target triggers. This method allows for       |
    |                specifying a custom stopping distance.        |
    ----------------------------------------------------------------
    */
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        //Set the target data
        _target.Set(t, c, p, d);

        //Configures and enables the target trigger at the correct
        //position and with the correct radius
        if (_targetTrigger != null)
        {
            _targetTrigger.radius = s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    /*
    ----------------------------------------------------------------
    |  Name        : SetTarget (Overload)                          |
    |  Description : Sets the current target and configures the    |
    |                target trigger                                |
    ----------------------------------------------------------------
    */
    public void SetTarget(AITarget t)
    {
        _target = t;
        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stopingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    public void ClearTarget()
    {
        _target.Clear();
        if(_targetTrigger != null)
        {
            _targetTrigger.enabled = false;
        }
    }
    /*
    ----------------------------------------------------------------
    |  Name        : FixedUpdate                                   |
    |  Description : called by Unity for each tic of the physics   |
    |                system. It clears the audio and visual threats|
    |                each update and re-calculates the distance to |
    |                the current target                            |
    ----------------------------------------------------------------
    */
    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear();
        AudioThreat.Clear();

        if(_target.type != AITargetType.None)
        {
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }
        _isTargetReached = false;
    }
    /*
    ----------------------------------------------------------------
    |  Name        : Update                                        |
    |  Description : Called by Unity each frame. Gives the current |
    |                state a chance to update itself and preform   |
    |                transitions                                   |
    ----------------------------------------------------------------
    */
    protected virtual void Update()
    {
        if (_currentState == null) return;
        AIStateType newStateType = _currentState.OnUpdateState();
        if(newStateType != _currentStateType)
        {
            AIState newState = null;
            if(_statesDicionary.TryGetValue(newStateType, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            else if (_statesDicionary.TryGetValue(AIStateType.Idle, out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            _currentStateType = newStateType;
        }
    }
    /*
    -----------------------------------------------------------------------------
    |  Class       : OnTriggerEnter                                             |
    |  Description : Called by Physics system when the AI's Main collider enters|
	|				its trigger. This allows the child state to know when it has|
	|				entered the sphere of influence	of a waypoint or last player|
	|				sighted position.                                           |
    -----------------------------------------------------------------------------
    */
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;
        _isTargetReached = true;
        if(_currentState) _currentState.OnDestinationReached(true);
    }
    protected virtual void OnTriggerStay(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;
        _isTargetReached = true;
    }
    /*
    ----------------------------------------------------------------------------
    |  Class       : OnTriggerExit                                             |
    |  Description : Informs the child state that the AI entity is no longer at|
    |                its destination (typically true when a new target has been|
    |                set by the child.                                         |
    ----------------------------------------------------------------------------
    */
    protected void OnTriggerExit(Collider other)
    {
        if (_targetTrigger == null || _targetTrigger != other) return;
        _isTargetReached = false;
        if (_currentState) _currentState.OnDestinationReached(false);
    }

    /*
    --------------------------------------------------------------------------
    |  Class       : OnTriggerEvent                                          |
    |  Description : Called by our AISensor component when an AI Aggravator  |
    |			     has entered/exited the sensor trigger.                  |
    --------------------------------------------------------------------------
    */
    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
    {
        if (_currentState != null)
            _currentState.OnTriggerEvent(type, other);
    }
    /*
    --------------------------------------------------------------------------
    |  Class       : OnAnimatorMove                                          |
    |  Description : Called by Unity after root motion has been              |
    |                evaluated but not applied to the object.                |
    |                This allows us to determine via code what to do         |
    |                with the root motion information                        |
    --------------------------------------------------------------------------
    */
    protected void OnAnimatorMove()
    {
        if (_currentState != null)
            _currentState.OnAnimatorUpdated();
    }
    /*
    --------------------------------------------------------------------------
    |  Class       : OnAnimatorIK                                            |
    |  Description : Called by Unity just prior to the IK system being       |
    |                updated giving us a chance to setup up IK Targets       |
    |                and weights.                                            |
    --------------------------------------------------------------------------
    */
    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState != null)
            _currentState.OnAnimatorIKUpdate();
    }
    /*
    --------------------------------------------------------------------------
    |  Class       : NavAgentControl                                         |
    |  Description : Configure the NavMeshAgent to enable/disable auto       |
    |			updates of position/rotation to our transform                |
    --------------------------------------------------------------------------
    */
    public void NavAgentControl(bool posUpdate, bool rotUpdate)
    {
        if(_navMeshAgent)
        {
            _navMeshAgent.updatePosition = posUpdate;
            _navMeshAgent.updateRotation = rotUpdate;
        }
    }
    /*
    --------------------------------------------------------------------------
    |  Class       : AddRootMotionRequest                                    |
    |  Description : Called by the State Machine Behaviours to               |
	|                Enable/Disable root motion                              |
    --------------------------------------------------------------------------
    */
    public void AddRootMotionRequest(int rootPos, int rootRot)
    {
        _rootPositionRefCount += rootPos;
        _rootRotationRefCount += rootRot;
    }
}
