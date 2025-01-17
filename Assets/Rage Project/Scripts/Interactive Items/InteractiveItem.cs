﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveItem : MonoBehaviour
{
    [SerializeField] protected int _priority = 0;

    public int priority { get { return _priority; } }

    protected GameSceneManager _gameSceneManager = null;
    protected Collider _collider = null;

    public virtual string GetText() { return null; }
    public virtual void Activate(CharacterManager characterManager) { }
    protected virtual void Start()
    {
        _gameSceneManager = GameSceneManager.instance;
        _collider = GetComponent<Collider>();

        if(_gameSceneManager != null && _collider != null)
        {
            _gameSceneManager.RegisterInteractiveItem(_collider.GetInstanceID(), this);
        }
    }
}
