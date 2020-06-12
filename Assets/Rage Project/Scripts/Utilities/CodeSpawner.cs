using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeSpawner : MonoBehaviour
{
    [SerializeField] GameObject _prefab = null;
    [SerializeField] List<Transform> _spawnTransform = null;

    private void Awake()
    {
        if (_spawnTransform.Count == 0 || _prefab == null) return;

        Transform spawnPoint = _spawnTransform[Random.Range(0, _spawnTransform.Count)];
        Instantiate(_prefab, spawnPoint.position, spawnPoint.rotation);
    }
}
