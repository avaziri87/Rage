using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();

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
}
