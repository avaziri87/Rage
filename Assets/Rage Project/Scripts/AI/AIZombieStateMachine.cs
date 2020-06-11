using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim}
public enum AIScreamPosition { Entity, Player}
public class BodyPartSnapshot
{
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
}
/*
 ------------------------------------------------------------
 |  class       : AIZombieStateMachine                      |
 |  Description : State Machine used by zombie characters   |
 ------------------------------------------------------------
 */
public class AIZombieStateMachine : AIStateMachine
{
    //Inspector Assigned
    #region
    [SerializeField] [Range(10.0f, 360.0f)]     float               _fov                    = 50.0f;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _sight                  = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _hearing                = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _aggression             = 0.5f;
    [SerializeField] [Range(0, 100)]            int                 _health                 = 100;
    [SerializeField] [Range(0, 100)]            int                 _upperBodyDamage        = 0;
    [SerializeField] [Range(0, 100)]            int                 _lowerBodyDamage        = 0;
    [SerializeField] [Range(0, 100)]            int                 _upperBodyThreshold     = 30;
    [SerializeField] [Range(0, 100)]            int                 _limpThreshold          = 30;
    [SerializeField] [Range(0, 100)]            int                 _crawlThreshold         = 90;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _intelligence           = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _satisfaction           = 1.0f;
    [SerializeField]                            float               _replenishRate          = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)]        float               _screamChance           = 1.0f;
    [SerializeField] [Range(0.0f, 50.0f)]       float               _screamRadius           = 20.0f;
    [SerializeField]                            AIScreamPosition    _screamPosition         = AIScreamPosition.Player;
    [SerializeField]                            AISoundEmitter      _screamPrefab           = null;
    [SerializeField]                            float               _depletionRate          = 0.1f;
    [SerializeField]                            float               _reanimationBlendTime   = 0.5f;
    [SerializeField]                            float               _reanimationWaitTime    = 3.0f;
    [SerializeField]                            LayerMask           _geometryLayers         = 0;
    #endregion

    //Private
    #region
    int _seeking = 0;
    bool _feeding = false;
    bool _crawling = false;
    int _attackType = 0;
    float _speed = 0.0f;
    float _isScreaming = 0.0f;


    //Ragdoll
    AIBoneControlType _boneControlType = AIBoneControlType.Animated;
    List<BodyPartSnapshot> _bodyPartSnapshots = new List<BodyPartSnapshot>();
    float _ragdollEndTime = float.MinValue;
    Vector3 _ragdollHipPosition;
    Vector3 _ragdollFeetPosition;
    Vector3 _ragdollHeadPosition;
    IEnumerator _reanimationCoroutine = null;
    float _mechanimTransitiontime = 0.1f;
    #endregion

    //Animator Hash
    #region
    int _speedHash = Animator.StringToHash("Speed");
    int _feedingHash = Animator.StringToHash("Feeding");
    int _seekingHash = Animator.StringToHash("Seeking");
    int _attackHash = Animator.StringToHash("Attack");
    int _crawlingHash = Animator.StringToHash("Crawling");
    int _screamingHash = Animator.StringToHash("Screaming");
    int _screamHash = Animator.StringToHash("Scream");
    int _hitTriggergHash = Animator.StringToHash("Hit");
    int _hitTypeHash = Animator.StringToHash("Hit Type");
    int _reanimatedFromBackHash = Animator.StringToHash("Reanimate From Back");
    int _reanimatedFromFrontHash = Animator.StringToHash("Reanimate From Front");
    int _lowerBodyDamageHash = Animator.StringToHash("Lower Body Damage");
    int _upperBodyDamageHash = Animator.StringToHash("Upper Body Damage");
    int _stateHash = Animator.StringToHash("State");
    int _upperBodyLayer = -1;
    int _lowerBodyLayer = -1;
    #endregion

    //Public Properties
    #region
    public float fov { get { return _fov; } }
    public float hearing { get { return _hearing; } }
    public float sight { get { return _sight; } }
    public bool crawling { get { return _crawling; } }
    public float intelligence { get { return _intelligence; } }
    public float satisfaction { get { return _satisfaction; } set { _satisfaction = value; } }
    public float aggression { get { return _aggression; } set { _aggression = value; } }
    public int health { get { return _health; } set { _health = value; } }
    public int attackType { get { return _attackType; } set { _attackType = value; } }
    public bool feeding { get { return _feeding; } set { _feeding = value; } }
    public int seeking { get { return _seeking; } set { _seeking = value; } }
    public float speed { get { return _speed; } set { _speed = value; } }
    public float replenishRate { get { return _replenishRate; } }
    public bool isCrawling { get { return (_lowerBodyDamage >= _crawlThreshold); } }
    public bool isScreaming { get { return _isScreaming > 0.1f; } }
    public float ScreamChance { get{ return _screamChance; }
    }
    #endregion
    protected override void Start()
    {
        base.Start();
        if(_animator != null)
        {
            _lowerBodyLayer = _animator.GetLayerIndex("Lower Body");
            _upperBodyLayer = _animator.GetLayerIndex("Upper Body");
        }
        if(_rootBone != null)
        {
            Transform[] transforms = _rootBone.GetComponentsInChildren<Transform>();
            foreach(Transform trans in transforms)
            {
                BodyPartSnapshot snapShot = new BodyPartSnapshot();
                snapShot.transform = trans;
                _bodyPartSnapshots.Add(snapShot);

            }
        }
        UpdateAnimatorDamage();
    }
    /*
    -------------------------------------------------------------
    |  Name        : Update                                     |
    |  Description : Refresh the animator with up-to-date values|
    |                for its parameters                         |
    -------------------------------------------------------------
    */
    protected override void Update()
    {
        base.Update();
        if(_animator != null)
        {
            _animator.SetFloat  (_speedHash,   _speed);
            _animator.SetBool   (_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash,  _attackType);
            _animator.SetInteger(_stateHash, (int)_currentStateType);

            _isScreaming = IsLayerActive("Cinematic") ? 0.0f : _animator.GetFloat(_screamHash);
        }

        _satisfaction = Mathf.Max(0, _satisfaction -((_depletionRate * Time.deltaTime)/100)*Mathf.Pow(_speed,3));
    }
    public bool Scream()
    {
        if (isScreaming) return true;
        if (_animator == null || IsLayerActive("Cinematic") || _screamPrefab == null) return false;

        _animator.SetTrigger(_screamHash);
        Vector3 spawnPos = _screamPosition == AIScreamPosition.Entity ? transform.position : VisualThreat.position;
        AISoundEmitter screamEmitter = Instantiate(_screamPrefab, spawnPos, Quaternion.identity) as AISoundEmitter;

        if (screamEmitter != null) screamEmitter.SetRadius(_screamRadius);
        return true;
    }
    protected void UpdateAnimatorDamage()
    {
        if(_animator != null)
        {
            if(_lowerBodyLayer != -1)
            {
                _animator.SetLayerWeight(_lowerBodyLayer,(_lowerBodyDamage > _limpThreshold && _lowerBodyDamage < _crawlThreshold)? 1.0f:0.0f);
            }
            if (_upperBodyLayer != -1)
            {
                _animator.SetLayerWeight(_upperBodyLayer, (_upperBodyDamage > _upperBodyThreshold && _lowerBodyDamage < _crawlThreshold) ? 1.0f : 0.0f);
            }
            _animator.SetBool(_crawlingHash, isCrawling);
            _animator.SetInteger(_upperBodyDamageHash, _upperBodyDamage);
            _animator.SetInteger(_lowerBodyDamageHash, _lowerBodyDamage);

            if (_lowerBodyDamage > _limpThreshold && _lowerBodyDamage < _crawlThreshold) SetLayerActive("Lower Body", true);
            else SetLayerActive("Lower Body", false);

            if (_upperBodyDamage > _upperBodyThreshold && _lowerBodyDamage < _crawlThreshold) SetLayerActive("Upper Body", true);
            else SetLayerActive("Upper Body", false);
        }
    }
    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection = 0)
    {
        if (GameSceneManager.instance != null && GameSceneManager.instance.bloodParticle != null)
        {
            ParticleSystem sys = GameSceneManager.instance.bloodParticle;
            sys.transform.position = position;
            var main = sys.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            sys.Emit(60);
        }

        float hitStrength = force.magnitude;

        if(_boneControlType == AIBoneControlType.Ragdoll)
        {
            if(bodyPart != null)
            {
                if (hitStrength > 1.0f)
                {
                    bodyPart.AddForce(force, ForceMode.Impulse);
                }

                if(bodyPart.CompareTag("Head"))
                {
                    _health = Mathf.Max(0, health - damage);
                }
                else if(bodyPart.CompareTag("Upper Body"))
                {
                    _upperBodyDamage += damage;
                }
                else if (bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                if(health > 0)
                {
                    if (_reanimationCoroutine != null)
                    {
                        StopCoroutine(_reanimationCoroutine);
                    }
                    _reanimationCoroutine = Reanimate();
                    StartCoroutine(_reanimationCoroutine);
                }
            }
        }

        Vector3 attackerLocaPos = transform.InverseTransformPoint(characterManager.transform.position);
        Vector3 hitLocalPos     = transform.InverseTransformPoint(position);

        bool shouldRagdoll = (hitStrength > 1.0f);

        if (bodyPart != null)
        {
            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(0, health - damage);
                if (health == 0) shouldRagdoll = true;
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();
                shouldRagdoll = true;
            }
        }

        if (_boneControlType != AIBoneControlType.Animated || isCrawling || IsLayerActive("Cinematic") || attackerLocaPos.z < 0) shouldRagdoll = true;
        
        if(!shouldRagdoll)
        {
            float angle = 0.0f;
            if(hitDirection ==0)
            {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSinedAngle(vecToHit, transform.forward);
            }

            int hitType = 0;
            if(bodyPart.CompareTag("Head"))
            {
                if (angle < -10 || hitDirection == -1) hitType = 1;
                else if (angle >10 || hitDirection == 1) hitType = 3;
                else hitType = 2;
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                if (angle < -20 || hitDirection == -1) hitType = 4;
                else if (angle > 20 || hitDirection == 1) hitType = 6;
                else hitType = 5;
            }

            if(_animator)
            {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggergHash);
            }

            return;
        }
        else
        {
            if(_currentState)
            {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.None;
            }
            if (_navMeshAgent) _navMeshAgent.enabled = false;
            if (_animator) _animator.enabled = false;
            if (_collider) _collider.enabled = false;

            if (_layeredAudioSource != null) _layeredAudioSource.Mute(true);

            inMeleeRange = false;

            foreach(Rigidbody body in _bodyParts)
            {
                if(body)
                {
                    body.isKinematic = false;
                }
            }

            if(hitStrength > 1.0f)
            {
                if(bodyPart != null) bodyPart.AddForce(force, ForceMode.Impulse);
            }

            _boneControlType = AIBoneControlType.Ragdoll;

            if(health > 0)
            {
                if(_reanimationCoroutine != null)
                {
                    StopCoroutine(_reanimationCoroutine);
                }
                _reanimationCoroutine = Reanimate();
                StartCoroutine(_reanimationCoroutine);
            }
        }
    }
    /*
    -------------------------------------------------------------
    |  Name        : Reanimate (coroutine)                      |
    |  Description : Starts Reanimation                         |
    -------------------------------------------------------------
    */
    protected IEnumerator Reanimate()
    {
        //only animate in ragdoll state
        if (_boneControlType != AIBoneControlType.Ragdoll || _animator == null) yield break;

        //wait for reanimation time before initiating reanimation
        yield return new WaitForSeconds(_reanimationWaitTime);

        //record what time reanimation started
        _ragdollEndTime = Time.time;

        //change all body parts back to is kinematic RB
        foreach(Rigidbody body in _bodyParts)
        {
            body.isKinematic = true;
        }

        //put us in animation mode
        _boneControlType = AIBoneControlType.RagdollToAnim;

        //record all transforms and rotation of body parts
        foreach(BodyPartSnapshot snapShot in _bodyPartSnapshots)
        {
            snapShot.position       = snapShot.transform.position;
            snapShot.rotation       = snapShot.transform.rotation;
        }

        //record ragdoll head and feet position
        _ragdollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;
        _ragdollFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.RightFoot).position + _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position) * 0.5f;
        _ragdollHipPosition = _rootBone.position;
        //enable animator
        _animator.enabled = true;


        if(_rootBone != null)
        {
            float forwardTest;
            switch(_rootBoneAlignment)
            {
                case AIBoneAlignmentType.ZAxis:
                    forwardTest = _rootBone.forward.y;
                    break;
                case AIBoneAlignmentType.ZAxisInverted:
                    forwardTest = -_rootBone.forward.y;
                    break;
                case AIBoneAlignmentType.YAxis:
                    forwardTest = _rootBone.up.y;
                    break;
                case AIBoneAlignmentType.YAxisInverted:
                    forwardTest = -_rootBone.up.y;
                    break;
                case AIBoneAlignmentType.XAxis:
                    forwardTest = _rootBone.right.y;
                    break;
                case AIBoneAlignmentType.XAxisInverted:
                    forwardTest = -_rootBone.right.y;
                    break;
                default:
                    forwardTest = _rootBone.forward.y;
                    break;
            }
            if(forwardTest >=0)
            {
                _animator.SetTrigger(_reanimatedFromBackHash);
            }
            else
            {
                _animator.SetTrigger(_reanimatedFromFrontHash);
            }
        }
    }
    /*
    -------------------------------------------------------------
    |  Name        : LateUpdate                                 |
    |  Description : called by unity after every update, used to|
    |                reanimate                                  |
    -------------------------------------------------------------
    */
    protected void LateUpdate()
    {
        if(_boneControlType == AIBoneControlType.RagdollToAnim)
        {
            if(Time.time <= _ragdollEndTime + _mechanimTransitiontime)
            {
                Vector3 animatedToRagdoll = _ragdollHipPosition - _rootBone.position;
                Vector3 newRootPosition = transform.position + animatedToRagdoll;

                RaycastHit[] hits = Physics.RaycastAll(newRootPosition+ (Vector3.up * 0.25f), Vector3.down, float.MaxValue, _geometryLayers);
                newRootPosition.y = float.MinValue;

                foreach(RaycastHit hit in hits)
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        newRootPosition.y = Mathf.Max(hit.point.y, newRootPosition.y);
                    }
                }

                NavMeshHit navMeshHit;
                Vector3 baseOffset = Vector3.zero;
                if (_navMeshAgent) baseOffset.y = _navMeshAgent.baseOffset;
                if (NavMesh.SamplePosition(newRootPosition, out navMeshHit, 25.0f, NavMesh.AllAreas))
                {
                    transform.position = navMeshHit.position;
                }
                else
                {
                    transform.position = newRootPosition;
                }
                Vector3 ragdollDirection = _ragdollHeadPosition - _ragdollFeetPosition;

                ragdollDirection.y = 0.0f;

                Vector3 meanFeetPosition = 0.5f * (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = _animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                animatedDirection.y = 0.0f;
                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdollDirection.normalized);
            }

            float blendAmount = Mathf.Clamp01((Time.time - _ragdollEndTime - _mechanimTransitiontime) / _reanimationBlendTime);

            foreach(BodyPartSnapshot snapshot in _bodyPartSnapshots)
            {
                if(snapshot.transform == _rootBone)
                {
                    snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position, blendAmount);
                }
                snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation, snapshot.transform.rotation, blendAmount);
            }
            
            if (blendAmount == 1.0f)
            {
                _boneControlType = AIBoneControlType.Animated;
                if (_navMeshAgent)  _navMeshAgent.enabled   = true;
                if (_collider)      _collider.enabled       = true;

                AIState newState = null;
                if (_statesDicionary.TryGetValue(AIStateType.Alerted, out newState))
                {
                    if (_currentState != null) _currentState.OnExitState();
                    newState.OnEnterState();
                    _currentState = newState;
                    _currentStateType = AIStateType.Alerted;
                }
            }
        }
    }
}
