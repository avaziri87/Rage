using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    //inpector assigned
    [SerializeField] CapsuleCollider    _meleeTrigger       = null;
    [SerializeField] CameraBloodEffect  _cameraBloodEffect  = null;
    [SerializeField] Camera             _camera             = null;
    [SerializeField] float              _health             = 100f;
    [SerializeField] int                _weaponDamage       = 10;
    [SerializeField] float              _weaponForce        = 35.0f;
    [SerializeField] AISoundEmitter     _soundEmitter       = null;
    [SerializeField] float              _walkRadius         = 0.0f;
    [SerializeField] float              _runRadius          = 7.0f;
    [SerializeField] float              _landingRadius      = 12.0f;
    [SerializeField] float              _bloodRadiusScale   = 6.0f;

    //pain Audio
    [SerializeField] AudioCollection _damageSounds      = null;
    [SerializeField] AudioCollection _painSounds        = null;
    [SerializeField] float          _nextPainSoundtime  = 0.0f;
    [SerializeField] float          _painSoundOffset    = 0.35f;

    //private
    Collider            _collider               = null;
    FPSController       _fpsController          = null;
    CharacterController _characterController    = null;
    GameSceneManager    _gameSceneManager       = null;
    int                 _aiBodyPartLayer        = -1;

    void Start()
    {
        _collider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.instance;
        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");
        if(_gameSceneManager != null)
        {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = _collider;
            info.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), info);
        }
    }
    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            DoDamage(0);
        }

        if(_fpsController != null || _soundEmitter != null)
        {
            float newRadius = Mathf.Max(_walkRadius, (100 - _health)/ _bloodRadiusScale);

            switch(_fpsController.moveStatus)
            {
                case PlayerMoveStatus.Landing: 
                    newRadius = Mathf.Max(newRadius, _landingRadius); 
                    break;
                case PlayerMoveStatus.Running:
                    newRadius = Mathf.Max(newRadius, _runRadius);
                    break;
                default:
                    break;
            }
            _soundEmitter.SetRadius(newRadius);
            _fpsController.dragMultiplierLimit = Mathf.Max(_health / 100.0f, 0.25f);
        }
    }

    private void DoDamage(int hitDirection = 0)
    {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;
        ray = _camera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << _aiBodyPartLayer);
        if(isSomethingHit)
        {
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if(stateMachine)
            {
                stateMachine.TakeDamage(hit.point, ray.direction * _weaponForce, _weaponDamage, hit.rigidbody, this, 0);
            }
        }
    }

    public void TakeDamage(float damageAmount, bool doDamage, bool doPain)
    {
        _health = Mathf.Max(0, _health - damageAmount*Time.deltaTime);
        if(_fpsController)
        {
            _fpsController.dragMultiplier = 0.0f;
        }

        if(_cameraBloodEffect != null)
        {
            _cameraBloodEffect.minBloodAmount = (1.0f - _health / 100.0f) * 0.5f;
            _cameraBloodEffect.bloodAmount = Mathf.Min(1, _cameraBloodEffect.minBloodAmount + 0.3f);
        }

        if(AudioManager.instance)
        {
            if(doDamage && _damageSounds != null)
            {
                AudioManager.instance.PlayOneShotSound(_damageSounds.audioGroup,
                                                        _damageSounds.audioClip,
                                                        transform.position,
                                                        _damageSounds.volume,
                                                        _damageSounds.spatialBlend,
                                                        _damageSounds.priority);
            }

            if (doPain && _painSounds != null && _nextPainSoundtime < Time.deltaTime)
            {
                AudioClip painClip = _painSounds.audioClip;

                if(painClip)
                {
                    _nextPainSoundtime = Time.deltaTime + painClip.length;
                }

                StartCoroutine(AudioManager.instance.PlayOneShotSoundDelayed(_painSounds.audioGroup,
                                                                             painClip,
                                                                             transform.position,
                                                                             _painSounds.volume,
                                                                             _painSounds.spatialBlend,
                                                                             _painSoundOffset,
                                                                             _painSounds.priority));
            }
        }
    }
}
