using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum PlayerMoveStatus { NotMoving, Crouchin, Walking, Running, NotGrounded, Landing }
public enum CurveControlledBobCallbackType { Horizontal, Vertical}

public delegate void CurvedControlledBobCallback();
[System.Serializable]
public class CurveControlledBobEvent
{
    public float Time = 0;
    public CurvedControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.Vertical;
}
[System.Serializable]
public class CurveControlledBob
{
    [SerializeField] AnimationCurve _bobCurve = new AnimationCurve( new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                                    new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                                    new Keyframe(2f, 0f));

    [SerializeField] float          _horizontalMultiplier           = 0.01f;
    [SerializeField] float          _verticalMultiplier             = 0.02f;
    [SerializeField] float          _verticalHorizontalSpeedRation  = 2.0f;
    [SerializeField] float          _baseInterval                   = 1.0f;

    float _prevXPlayHead;
    float _prevYPlayHead;
    float _xPlayHead;
    float _yPlayHead;
    float _curvedEndTime;
    List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();
    public void Initialize()
    {
        _curvedEndTime      = _bobCurve[_bobCurve.length - 1].time;
        _xPlayHead          = 0.0f;
        _yPlayHead          = 0.0f;
        _prevXPlayHead      = 0.0f;
        _prevYPlayHead      = 0.0f;
    }
    public void RegisterEventCallback(float time, CurvedControlledBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        _events.Sort(
        delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2)
        {
            return (t1.Time.CompareTo(t2.Time));
        });
    }
    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval)*_verticalHorizontalSpeedRation;

        if (_xPlayHead > _curvedEndTime)
            _xPlayHead -= _curvedEndTime;

        if (_yPlayHead > _curvedEndTime)
            _yPlayHead -= _curvedEndTime;

        for (int i = 0; i < _events.Count; i++)
        {
            CurveControlledBobEvent ev = _events[i];
            if (ev != null)
            {
                if (ev.Type == CurveControlledBobCallbackType.Vertical)
                {
                    if ((_prevYPlayHead < ev.Time && _yPlayHead >= ev.Time) ||
                        (_prevYPlayHead > _yPlayHead && (ev.Time > _prevYPlayHead || ev.Time <= _yPlayHead)))
                    {
                        ev.Function();
                    }
                }
                else
                {
                    if ((_prevXPlayHead < ev.Time && _xPlayHead >= ev.Time) ||
                        (_prevXPlayHead > _xPlayHead && (ev.Time > _prevXPlayHead || ev.Time <= _xPlayHead)))
                    {
                        ev.Function();
                    }
                }
            }
        }
        float xPos = _bobCurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobCurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        return new Vector3(xPos, yPos, 0);
    }
}

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [SerializeField] List<AudioSource> audioSources = new List<AudioSource>();
    int _audioActive = 0;

    [SerializeField] float _walkSpeed           = 2.0f;
    [SerializeField] float _runSpeed            = 4.0f;
    [SerializeField] float _jumpSeed            = 7.5f;
    [SerializeField] float _crouchedSpeed       = 1.0f;
    [SerializeField] float _stickToGroundForce  = 5.0f;
    [SerializeField] float _gravityMultiplier   = 2.5f;
    [SerializeField] float _runStepLengthen     = 0.75f;
    [SerializeField] GameObject _flashlight = null;
    [SerializeField] CurveControlledBob _headBob = new CurveControlledBob();

    [SerializeField] UnityStandardAssets.Characters.FirstPerson.MouseLook _mouseLook = new UnityStandardAssets.Characters.FirstPerson.MouseLook();

    Camera      _camera                 = null;
    bool        _jumpButtonPressed      = false;
    Vector2     _inputVector            = Vector2.zero;
    Vector3     _moveDirection          = Vector3.zero;
    bool        _previouslyGrounded     = false;
    bool        _isWalking              = true;
    bool        _isJumping              = false;
    bool        _isCrounching           = false;
    float       _fallingTimer           = 0.0f;
    Vector3     _localSpaceCameraPos    = Vector3.zero;
    float       _controllerHeight       = 0.0f;

    CharacterController _characterController = null;
    [SerializeField]PlayerMoveStatus _moveStatus = PlayerMoveStatus.NotMoving;

    public PlayerMoveStatus moveStatus { get { return _moveStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }

    protected void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _controllerHeight = _characterController.height;
        _camera = Camera.main;
        _moveStatus = PlayerMoveStatus.NotMoving;
        _fallingTimer = 0.0f;
        _mouseLook.Init(transform, _camera.transform);
        _localSpaceCameraPos = _camera.transform.localPosition;
        _headBob.Initialize();
        _headBob.RegisterEventCallback(1.5f, PlayFootStepSound, CurveControlledBobCallbackType.Vertical);

        if(_flashlight != null)
        {
            _flashlight.SetActive(false);
        }
    }
    private void Update()
    {
        if (_characterController.isGrounded)    _fallingTimer = 0.0f;
        else                                    _fallingTimer += Time.deltaTime;

        if (Time.timeScale > Mathf.Epsilon)     _mouseLook.LookRotation(transform, _camera.transform);

        if (!_jumpButtonPressed && !_isCrounching)                _jumpButtonPressed = Input.GetButtonDown("Jump");
        if(Input.GetButtonDown("Crouch"))
        {
            _isCrounching = !_isCrounching;
            _characterController.height = _isCrounching == true ? _controllerHeight / 2.0f : _controllerHeight;
        }

        if(Input.GetButtonDown("Flashlight"))
        {
            if (_flashlight) _flashlight.SetActive(!_flashlight.activeSelf);
        }

        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 0.5f)
            {
                //TODO: Play lading stuff
            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _moveStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
        {
            _moveStatus = PlayerMoveStatus.NotGrounded;
        }
        else if (_characterController.velocity.sqrMagnitude > 0.01f)
        {
            _moveStatus = PlayerMoveStatus.NotMoving;
        }
        else if(_isCrounching)
        {
            _moveStatus = PlayerMoveStatus.Crouchin;
        }
        else if (_isWalking)
        {
            _moveStatus = PlayerMoveStatus.Walking;
        }
        else
        {
            _moveStatus = PlayerMoveStatus.Running;
        }

        _previouslyGrounded = _characterController.isGrounded;
    }
    private void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        //set the desired player speed to be either walking or running
        float speed = _isCrounching? _crouchedSpeed: _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

        if (_inputVector.sqrMagnitude > 1) _inputVector.Normalize();

        //always move along the camera forward as it is the direction
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        //get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        if(Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height/2f,1))
        {
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
        }

        //scale movement by our current speed (walking value or running vlaue)
        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;

        //if grounded
        if(_characterController.isGrounded)
        {
            //apply severe down force to keep control sticking to the floor
            _moveDirection.y = -_stickToGroundForce;

            // if jump was pressed the apply speed in up direction and set jump to true
            if(_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSeed;
                _jumpButtonPressed = false;
                _isJumping = true;
            }
        }
        else
        {
            //we are not on the ground so apply standard system gravity multiplied by our multiplier
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.deltaTime;
        }

        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.z); 
        if(speedXZ.magnitude > 0.01f)
        {
            _camera.transform.localPosition = _localSpaceCameraPos + _headBob.GetVectorOffset(speedXZ.magnitude * (_isCrounching||_isWalking ? 1.0f : _runStepLengthen));
        }
        else
        {
            _camera.transform.localPosition = _localSpaceCameraPos;
        }
    }

    void PlayFootStepSound()
    {
        if (_isCrounching)
            return;
        audioSources[_audioActive].Play();
        _audioActive = (_audioActive == 0) ? 1 : 0;
    }
}
