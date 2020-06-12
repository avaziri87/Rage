using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveKeypad : InteractiveItem
{
    [SerializeField] Transform _elevator = null;
    [SerializeField] AudioCollection _collection = null;
    [SerializeField] int _bank = 0;
    [SerializeField] float _activationDelay = 2.2f;

    bool _isActivated = false;

    public override string GetText()
    {
        ApplicationManager appDatabase = ApplicationManager.instance;
        if (!appDatabase) return string.Empty;

        string powerState = appDatabase.GetGameState("POWER");
        string lockdownState = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        // If we have not turned on the power
        if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE"))
        {
            return "Keypad : No Power";
        }
        else
        // Or we have not deactivated lockdown
        if (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE"))
        {
            return "Keypad : Under Lockdown";
        }
        else
        // or we don't have the access code yet
        if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE"))
        {
            return "Keypad : Access Code Required";
        }

        // We have everything we need
        return "Keypad";
    }

    public override void Activate(CharacterManager characterManager)
    {
        if (_isActivated) return;
        ApplicationManager appDatabase = ApplicationManager.instance;
        if (!appDatabase) return;

        string powerState = appDatabase.GetGameState("POWER");
        string lockdownState = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE")) return;
        if (string.IsNullOrEmpty(lockdownState) || !lockdownState.Equals("FALSE")) return;
        if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE")) return;

        // Delay the actual animation for the desired number of seconds
        StartCoroutine(DoDelayedActivation(characterManager));

        _isActivated = true;
    }

    private IEnumerator DoDelayedActivation(CharacterManager characterManager)
    {
        if (!_elevator) yield break;

        if (_collection != null)
        {
            AudioClip clip = _collection[_bank];
            if(clip)
            {
                if(AudioManager.instance) AudioManager.instance.PlayOneShotSound(_collection.audioGroup,
                                                                                _collection.audioClip,
                                                                                transform.position,
                                                                                _collection.volume,
                                                                                _collection.spatialBlend,
                                                                                _collection.priority);
            }
        }

        yield return new WaitForSeconds(_activationDelay);

        if (characterManager != null)
            characterManager.transform.parent = _elevator;
        Animator animator = _elevator.GetComponent<Animator>();
        if(animator) animator.SetTrigger("Active");

        if(characterManager.fpsCotroller)
        {
            characterManager.fpsCotroller.freezeMovement = true;
        }
    }
}
