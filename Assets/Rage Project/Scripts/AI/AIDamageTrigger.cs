using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    //inspector assigned
    [SerializeField] string _parameter                  = "";
    [SerializeField] int    _bloodParticleBurstAmount   = 10;
    [SerializeField] float  _damageAmount               = 0.1f;
    [SerializeField] bool   _doDamageSound              = true;
    [SerializeField] bool   _doPainSound                = true;


    AIStateMachine      _stateMachine = null;
    Animator            _animator = null;
    int                 _parameterHash = -1;
    GameSceneManager    _gameSceneManager = null;
    bool                _firstContact       = false;
    private void Start()
    {
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

        if (_stateMachine != null)
            _animator = _stateMachine.GetComponent<Animator>();

        _parameterHash = Animator.StringToHash(_parameter);
        _gameSceneManager = GameSceneManager.instance;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!_animator) return;
        if (other.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
        {
            _firstContact = true;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (!_animator) return;
        if (other.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
        {
            if(GameSceneManager.instance && GameSceneManager.instance.bloodParticle)
            {
                ParticleSystem system = GameSceneManager.instance.bloodParticle;
                system.transform.position = transform.position;
                system.transform.rotation = Camera.main.transform.rotation;
                var main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                system.Emit(_bloodParticleBurstAmount);
            }
            
            if(_gameSceneManager != null)
            {
                PlayerInfo info = _gameSceneManager.GetPlayerInfo(other.GetInstanceID());

                if(info != null && info.characterManager != null)
                {
                    info.characterManager.TakeDamage(_damageAmount, _doDamageSound && _firstContact, _doPainSound);
                }
            }
            _firstContact = false;
        }
    }
}
