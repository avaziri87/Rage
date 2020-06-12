using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum ScreenFadeType { FadeIn, FadeOut}
public class PlayerHUD : MonoBehaviour
{
    //inspecter assigend
    [SerializeField] GameObject _crossair               = null;
    [SerializeField] Text _healthText             = null;
    [SerializeField] Text _staminaText            = null;
    [SerializeField] Text _ammoText = null;
    [SerializeField] Text _interactText           = null;
    [SerializeField] Image      _screenFade             = null;
    [SerializeField] Text _missionText            = null;
    [SerializeField] float      _missionTextDisplayTime = 3.0f;

    float _currentFadeLevel = 1.0f;
    IEnumerator _coroutine = null;
    void Start()
    {
        if(_screenFade)
        {
            Color color = _screenFade.color;
            color.a = _currentFadeLevel;
            _screenFade.color = color;
        }
        if(_missionText)
        {
            Invoke("HideMissionText", _missionTextDisplayTime);
        }
    }
    public void Invalidate(CharacterManager characterManager)
    {
        if (characterManager == null) return;

        if (_healthText) _healthText.text = "Health " + ((int)characterManager.health).ToString();
        if (_staminaText) _staminaText.text = "Stamina " + ((int)characterManager.stamina).ToString();
        if (_staminaText) _ammoText.text = "Ammo " + ((int)characterManager.ammo).ToString();
    }
    public void SetInteractiveText(string text)
    {
        if (_interactText)
        {
            if(text == null)
            {
                _interactText.text = null;
                _interactText.gameObject.SetActive(false);
            }
            else
            {
                _interactText.text = text;
                _interactText.gameObject.SetActive(true);
            }
        }
    }
    public void ShowMissionText(string text)
    {
        if(_missionText)
        {
            _missionText.text = text;
            _missionText.gameObject.SetActive(true);
        }
    }
    public void HideMissionText()
    {
        if (_missionText)
        {
            _missionText.gameObject.SetActive(false);
        }
    }
    public void Fade(float seconds, ScreenFadeType direction)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        float targetFade = 0.0f;

        switch(direction)
        {
            case ScreenFadeType.FadeIn:
                targetFade = 0.0f;
                break;
            case ScreenFadeType.FadeOut:
                targetFade = 1.0f;
                break;
            default:
                break;
        }

        _coroutine = FadeInternal(seconds, targetFade);
        StartCoroutine(_coroutine);
    }
    private IEnumerator FadeInternal(float seconds, float targetFade)
    {
        if (!_screenFade) yield break;

        float timer = 0;
        float srcFade = _currentFadeLevel;
        Color oldColor = _screenFade.color;
        if (seconds < 0.1f) seconds = 0.1f;

        while (timer < seconds)
        {
            timer += Time.deltaTime;
            _currentFadeLevel = Mathf.Lerp(srcFade, targetFade, timer/seconds);
            oldColor.a = _currentFadeLevel;
            _screenFade.color = oldColor;
            yield return null;
        }

        oldColor.a = _currentFadeLevel = targetFade;
        _screenFade.color = oldColor;
    }
}
