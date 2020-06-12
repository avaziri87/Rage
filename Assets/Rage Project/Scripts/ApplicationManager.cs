using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[System.Serializable]
public class GameState
{
    public string Key = null;
    public string Value = null;
}
public class ApplicationManager : MonoBehaviour
{
    [SerializeField] List<GameState> _startingGameStates = new List<GameState>();
    
    Dictionary<string, string> _gameStateDictionary = new Dictionary<string, string>();
    static ApplicationManager _instance = null;

    public static ApplicationManager instance 
    { 
        get 
        { 
            if(_instance == null)
            {
                _instance = (ApplicationManager)FindObjectOfType(typeof(ApplicationManager));
            }
            return _instance;
        } 
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        ResetGameState();
    }

    private void ResetGameState()
    {
        _gameStateDictionary.Clear();
        for (int i = 0; i < _startingGameStates.Count; i++)
        {
            GameState gs = _startingGameStates[i];
            _gameStateDictionary[gs.Key] = gs.Value;
        }
    }

    public string GetGameState(string key)
    {
        string result = null;
        _gameStateDictionary.TryGetValue(key, out result);
        return result;
    }
    public bool SetGameState(string key, string value)
    {
        if (key == null || value == null) return false;
        _gameStateDictionary[key] = value;
        return true;
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
    public void LoadGame()
    {
        ResetGameState();
        SceneManager.LoadScene("Creeper_01");
    }
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
