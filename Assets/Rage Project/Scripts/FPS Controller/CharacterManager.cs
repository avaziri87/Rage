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
                stateMachine.TakeDamage(hit.point, ray.direction * 35.0f, 75, hit.rigidbody, this, 0);
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        _health = Mathf.Max(0, _health - damageAmount*Time.deltaTime);
        if(_cameraBloodEffect != null)
        {
            _cameraBloodEffect.minBloodAmount = (1.0f - _health / 100.0f);
            _cameraBloodEffect.bloodAmount = Mathf.Min(1, _cameraBloodEffect.minBloodAmount + 0.3f);
        }
    }
}
