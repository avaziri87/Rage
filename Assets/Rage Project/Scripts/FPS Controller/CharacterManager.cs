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
    [SerializeField] PlayerHUD          _playerHUD          = null;
    [SerializeField] AudioCollection    _gunShotSounds      = null;
    [SerializeField] float              _shootIinterval     = 1.0f;
    [SerializeField] float              _gunShotEmitterRadius = 15.0f;
    [SerializeField] int                _ammoCount          = 10;
    [SerializeField] AudioCollection    _tauntSounds        = null;
    [SerializeField] float              _tauntRadius        = 10.0f;
    [SerializeField] float              _nextTauntSoundTime = 0;

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
    int                 _interactiveMask        = 0;
    float               _lastShotTime           = 0.0f;
    bool                _shotFired              = false;

    public float health { get { return _health; } }
    public float stamina { get { return _fpsController != null ? _fpsController.stamina : 0.0f; } }
    public int ammo { get { return _ammoCount; } }

    public FPSController fpsCotroller { get { return _fpsController; } }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _collider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.instance;
        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");
        _interactiveMask = 1 << LayerMask.NameToLayer("Interactive");

        if(_gameSceneManager != null)
        {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = _collider;
            info.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), info);
        }

        if(_playerHUD) _playerHUD.Fade(2.0f, ScreenFadeType.FadeIn);
    }
    private void Update()
    {
        Ray ray;
        RaycastHit hit;
        RaycastHit[] hits;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2,0));
        float raylength = Mathf.Lerp(1.0f, 1.8f, Mathf.Abs(Vector3.Dot(_camera.transform.forward, Vector3.up)));
        hits = Physics.RaycastAll(ray, raylength, _interactiveMask);

        if(hits.Length > 0)
        {
            int highestPriority = int.MinValue;
            InteractiveItem priorityObject = null;
            for (int i=0; i<hits.Length; i++)
            {
                hit = hits[i];
                InteractiveItem interactiveObject = _gameSceneManager.GetInteractiveItem(hit.collider.GetInstanceID());
                if(interactiveObject != null && interactiveObject.priority > highestPriority)
                {
                        priorityObject = interactiveObject;
                        highestPriority = priorityObject.priority;
                }
            }

            if(priorityObject != null)
            {
                if (_playerHUD != null) _playerHUD.SetInteractiveText(priorityObject.GetText());

                if(Input.GetButtonDown("Use"))
                {
                    priorityObject.Activate(this);
                }
            }
        }
        else
        {
            if (_playerHUD) _playerHUD.SetInteractiveText(null);
        }

        if (Input.GetMouseButtonDown(0) && _ammoCount > 0 && _lastShotTime < Time.time)
        {
            _ammoCount--;
            _lastShotTime = Time.time + _shootIinterval;
            _shotFired = true;
            Debug.Log("shot fired: " + _shotFired);
            if(AudioManager.instance != null)
            {
                AudioManager.instance.PlayOneShotSound(_gunShotSounds.audioGroup,
                                        _gunShotSounds.audioClip,
                                        transform.position,
                                        _gunShotSounds.volume,
                                        _gunShotSounds.spatialBlend,
                                        _gunShotSounds.priority);
            }
            
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
                    if(_shotFired)
                    {
                        Debug.Log("shot fired");
                        newRadius = Mathf.Max(newRadius, _gunShotEmitterRadius);
                        _shotFired = false;
                    }
                    break;
            }
            Debug.Log("sound emitter radius: " + newRadius);
            _soundEmitter.SetRadius(newRadius);
            _fpsController.dragMultiplierLimit = Mathf.Max(_health / 100.0f, 0.25f);
        }


        if (Input.GetMouseButtonDown(1) && _nextTauntSoundTime < Time.time)
        {
            DoTaunt();
        }

        if (_playerHUD) _playerHUD.Invalidate(this);
    }

    private void DoTaunt()
    {
        if (_tauntSounds == null) return;
        AudioClip clip = _tauntSounds[0];
        AudioManager.instance.PlayOneShotSound(_tauntSounds.audioGroup,
                                               clip,
                                               transform.position,
                                               _tauntSounds.volume,
                                               _tauntSounds.spatialBlend,
                                               _tauntSounds.priority);
        if(_soundEmitter != null)
            _soundEmitter.SetRadius(_tauntRadius);
        _nextTauntSoundTime = Time.time + clip.length;
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

        if(_health <= 0.0f)
        {
            DoDeath();
        }
    }

    private void DoDeath()
    {
        if (_fpsController)
            _fpsController.freezeMovement = true;

        if (_playerHUD)
        {
            _playerHUD.Fade(3.0f, ScreenFadeType.FadeOut);
            _playerHUD.ShowMissionText("Mission Failed!");
            _playerHUD.Invalidate(this);
        }
        Invoke("GameOver", 3.0f);
    }

    public void DoLevelComplete()
    {
        if (_fpsController)
            _fpsController.freezeMovement = true;

        if(_playerHUD)
        {
            _playerHUD.Fade(4.0f, ScreenFadeType.FadeOut);
            _playerHUD.ShowMissionText("Mission Acomplished!");
            _playerHUD.Invalidate(this);
        }
        Invoke("GameOver", 4.0f);
    }

    void GameOver()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (ApplicationManager.instance) ApplicationManager.instance.LoadMainMenu(); 
    }
}
