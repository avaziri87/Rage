using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISoundEmitter : MonoBehaviour
{
    //inspector Assigned
    [SerializeField] float _decayRate = 1.0f;

    //Internal
    SphereCollider  _collider           = null;
    float           _srcRadius          = 0.0f;
    float           _tgtRadius          = 0.0f;
    float           _interpolator       = 0.0f;
    float           _interpolateSpeed   = 0.0f;

    private void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        if (!_collider) return;

        _srcRadius = _tgtRadius = _collider.radius;

        _interpolator = 0.0f;
        if(_decayRate > 0.2f)
        {
            _interpolateSpeed = 1.0f / _decayRate;
        }
        else
        {
            _interpolateSpeed = 0.0f;
        }
    }

    private void FixedUpdate()
    {
        if (!_collider) return;

        _interpolator = Mathf.Clamp01(_interpolator + Time.deltaTime*_interpolateSpeed);
        _collider.radius = Mathf.Lerp(_srcRadius, _tgtRadius, _interpolator);
        if (_collider.radius < Mathf.Epsilon) _collider.enabled = false;
        else                           _collider.enabled = true;
    }

    public void SetRadius(float newRadius, bool instanteResize = false)
    {
        if(!_collider || newRadius == _tgtRadius) return;

        _srcRadius = (instanteResize || newRadius > _collider.radius)? newRadius : _collider.radius;
        _tgtRadius = newRadius;
        _interpolator = 0.0f;
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.R)) SetRadius(15.0f);
    //}
}
