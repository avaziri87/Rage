using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] [Range(10.0f, 360.0f)] float _fov           = 50.0f;
    [SerializeField] [Range(0.0f, 1.0f)]    float _sight         = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)]    float _hearing       = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)]    float _aggression    = 0.5f;
    [SerializeField] [Range(0, 100)]        int   _health        = 100;
    [SerializeField] [Range(0.0f, 1.0f)]    float _intelligence  = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)]    float _satisfaction  = 1.0f;
    [SerializeField]                        float _replenishRate = 0.5f;
    [SerializeField]                        float _depletionRate = 0.1f;
    #endregion

    //Private
    #region
    int     _seeking    = 0;
    bool    _feeding    = false;
    bool    _crawling   = false;
    int     _attackType = 0;
    float   _speed      = 0.0f;
    #endregion

    //Animator Hash
    #region
    int _speedHash   = Animator.StringToHash("Speed");
    int _feedingHash = Animator.StringToHash("Feeding");
    int _seekingHash = Animator.StringToHash("Seeking");
    int _attackHash  = Animator.StringToHash("Attack");
    #endregion

    //Public Properties
    #region
    public float    fov           { get { return _fov; } }
    public float    hearing       { get { return _hearing; } }
    public float    sight         { get { return _sight; } }
    public bool     crawling      { get { return _crawling; } }
    public float    intelligence  { get { return _intelligence; } }
    public float    satisfaction  { get { return _satisfaction; } set { _satisfaction = value; } }
    public float    aggression    { get { return _aggression; }   set { _aggression = value; } }
    public int      health        { get { return _health; }       set { _health = value; } }
    public int      attackType    { get { return _attackType; }   set { _attackType = value; } }
    public bool     feeding       { get { return _feeding; }      set { _feeding = value; } }
    public int      seeking       { get { return _seeking; }      set { _seeking = value; } }
    public float    speed         { get { return _speed; }        set { _speed = value; } }
    public float    replenishRate { get { return _replenishRate; } }
    #endregion
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
        }
    }
}
