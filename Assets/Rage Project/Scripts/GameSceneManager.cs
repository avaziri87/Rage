using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}
/*
-------------------------------------------------------------------
|  Class       : GameSceneManager                                 |
|  Description : Singleton class that acts as the scene data base |
-------------------------------------------------------------------
*/
public class GameSceneManager : MonoBehaviour
{
    //Inspector assigned
    [SerializeField] ParticleSystem _bloodPArticles = null;

    //statics
    private static GameSceneManager _instance = null;
    public static GameSceneManager instance
    {
        get
        {
            if (_instance == null)
                _instance = (GameSceneManager)FindObjectOfType(typeof(GameSceneManager));
            return _instance;
        }
    }

    //private vairables
    Dictionary<int, AIStateMachine> _stateMachines  = new Dictionary<int, AIStateMachine>();
    Dictionary<int, PlayerInfo>     _playerInfo     = new Dictionary<int, PlayerInfo>();

    public ParticleSystem bloodParticle { get { return _bloodPArticles; } }
    /*
    -------------------------------------------------------------------
    |  Name        : RegisterAIStatemachine                           |
    |  Description : Stores the passed state machine in the dictionary|
    |                with the supplied key                            |
    -------------------------------------------------------------------
    */
    public void RegisterAIStatemachine(int key, AIStateMachine stateMachine)
    {
        if(!_stateMachines.ContainsKey(key))
        {
            _stateMachines[key] = stateMachine;
        }
    }
    /*
-------------------------------------------------------------------
|  Name        : GetAIStateMachine                                |
|  Description : Returns an AI state Machine reference searched on|
|                by the instance ID of an object                  |
-------------------------------------------------------------------
*/
    public AIStateMachine GetAIStateMachine(int key)
    {
        AIStateMachine _machine = null;
        if (_stateMachines.TryGetValue(key, out _machine))
        {
            return _machine;
        }
        return null;
    }
    /*
    -------------------------------------------------------------------
    |  Name        : RegisterPlayerInfo                               |
    |  Description : Stores the passed Player Info in the dictionary  |
    |                with the supplied key                            |
    -------------------------------------------------------------------
    */
    public void RegisterPlayerInfo(int key, PlayerInfo playerInfo)
    {
        if (!_playerInfo.ContainsKey(key))
        {
            _playerInfo[key] = playerInfo;
        }
    }
    /*
    -------------------------------------------------------------------
    |  Name        : GetPlayerInfo                                    |
    |  Description : Returns an Player Info reference searched on     |
    |                by the instance ID of an object                  |
    -------------------------------------------------------------------
    */
    public PlayerInfo GetPlayerInfo(int key)
    {
        PlayerInfo info = null;
        if (_playerInfo.TryGetValue(key, out info))
        {
            return info;
        }
        return null;
    }
}
