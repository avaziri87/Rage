using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxRotate : MonoBehaviour
{
    [SerializeField] Material _skyMaterial = null;
    [SerializeField] float _speed = 1.0f;

    float _angle = 0;
    float _originalAngle = 0;

    private void OnEnable()
    {
        if (_skyMaterial) _originalAngle = _angle = _skyMaterial.GetFloat("_Rotation");
    }

    private void OnDisable()
    {
        if (_skyMaterial) _skyMaterial.SetFloat("_Rotation", _originalAngle);
    }

    private void Update()
    {
        if (_skyMaterial == null) return;

        _angle += _speed * Time.deltaTime;
        if (_angle > 360.0f) _angle -= 360.0f;
        else if (_angle < 0.0f) _angle += 360.0f;

        _skyMaterial.SetFloat("_Rotation", _angle);
    }
}
