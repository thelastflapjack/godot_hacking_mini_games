using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


public class LockPickSimple : Spatial
{

    /// Constants ///
    private const float _CORE_UNLOCK_ANGLE = -0.5f * Mathf.Pi; 

    /// Exported Fields ///
    [Export(PropertyHint.Range, "1,10,0.5")]
    private float _allowedDeviationDeg = 5;
    [Export(PropertyHint.Range, "1,10,0.5")]
    private float _hookDamagePerSecondMax = 8;
    [Export(PropertyHint.Range, "1,3,0.5")]
    private float _hookDamagePerSecondMin = 2;
    [Export(PropertyHint.Range, "-90,90,1")]
    private float _solutionAngleDeg = 0;


    /// Properties - public, protected, private ///
    private float HookHealth{
        set{
            _hookHealth = Mathf.Max(value, 0);
            _debugInfo.HookHealth = _hookHealth;
            if (_hookHealth == 0)
            {
                LoadNewHook();
            }
        }
    }

    /// Fields - protected or private ///
    private float _solutionAngle;
    private float _rotationMaxHook = 0.5f * Mathf.Pi;
    private float _rotationMinHook = -0.5f * Mathf.Pi;
    private float _rotationSpeedHook = Mathf.Deg2Rad(40);
    private float _rotationSpeedCore = Mathf.Deg2Rad(90);
    private float _hookHealthMax = 10;

    private float _coreRotationTarget = 0;
    private float _hookHealth;
    private float _allowedDeviation;
    private float _proximityDeviation;
    private bool _isLoadingNewHook = false;
    private bool _isAttemptingTurn = false;
    private bool _isLocked = true;

    private Camera _camera;
    private LockPickSimpleDebugInfo _debugInfo;
    private Spatial _lockCore;
    private Spatial _lockHook;
    private Spatial _lockPry;

    private AnimationPlayer _coreAnimPlayer;
    private AnimationPlayer _hookAnimPlayer;
    private AnimationPlayer _pryAnimPlayer;
    private AudioStreamPlayer _audioHookTurn;

    private AudioStreamPlayer _audioHookSnap;
    private AudioStreamPlayer _audioJammed;
    private AudioStreamPlayer _audioUnlock;


    //////////////////////////////
    // Engine Callback Methods  //
    //////////////////////////////
    public override void _Ready()
    {
        CacheNodeReferences();

        _solutionAngle = Mathf.Deg2Rad(_solutionAngleDeg);
        _allowedDeviation = Mathf.Deg2Rad(_allowedDeviationDeg);
        _proximityDeviation = _allowedDeviation * 2;
        HookHealth = _hookHealthMax;

        _debugInfo.HookScreenLocation = _camera.UnprojectPosition(_lockHook.GlobalTranslation);
        _debugInfo.RotationMin = _rotationMinHook;
        _debugInfo.RotationMax = _rotationMaxHook;
        _debugInfo.AllowedDeviation = _allowedDeviation;
        _debugInfo.ProximityDeviation = _proximityDeviation;
        _debugInfo.RotationTarget = _solutionAngle;
        _debugInfo.CoreRotation = 0.0f;

        _debugInfo.Update();
    }


    public override void _PhysicsProcess(float delta)
    {
        float hookRotationDirection = (Input.GetActionStrength("ui_left") - Input.GetActionStrength("ui_right"));
        if (hookRotationDirection != 0 && !_isLoadingNewHook)
        {
            RotateHook(delta, hookRotationDirection);
        }
        else
        {
            _audioHookTurn.Stop();
        }

        if (Input.IsActionPressed("ui_select") && (!_isLoadingNewHook))
        {
            AttemptUnlock(delta);
        }
        else
        {   
            RotateCoreToNeutral(delta);
        }
    }


    //////////////////////////////
    //      Private Methods     //
    //////////////////////////////
    private void CacheNodeReferences()
    {
        _camera = GetNode<Camera>("Camera");
        _debugInfo = GetNode<LockPickSimpleDebugInfo>("DebugInfo");
        _lockCore = GetNode<Spatial>("Lock/Core");
        _lockHook = GetNode<Spatial>("Lock/Core/MeshInstance/Hook");
        _lockPry = GetNode<Spatial>("Lock/Core/MeshInstance/Pry");

        _coreAnimPlayer = GetNode<AnimationPlayer>("Lock/Core/AnimationPlayer");
        _hookAnimPlayer = _lockHook.GetNode<AnimationPlayer>("AnimationPlayer");
        _pryAnimPlayer = _lockPry.GetNode<AnimationPlayer>("AnimationPlayer");

        _audioHookTurn = GetNode<AudioStreamPlayer>("AudioHookTurn");
        _audioHookSnap = GetNode<AudioStreamPlayer>("AudioHookSnap");
        _audioJammed = GetNode<AudioStreamPlayer>("AudioJammed");
        _audioUnlock = GetNode<AudioStreamPlayer>("AudioUnlock");
    }

    private void RotateHook(float delta, float direction)
    {
        float rotationChange = delta * _rotationSpeedHook * direction;
        float rotationTarget = Mathf.Clamp(
            _lockHook.Rotation.x + rotationChange,
            _rotationMinHook, _rotationMaxHook
        );

        if (rotationTarget != _lockHook.Rotation.x)
        {
            _lockHook.Rotation = new Vector3(rotationTarget, 0, 0);
            _debugInfo.HookAngle = _lockHook.Rotation.x;
            _debugInfo.Update();

            _audioHookTurn.Play(_audioHookTurn.Stream.GetLength() * GD.Randf());
        }
        else
        {
            _audioHookTurn.Stop();
        }
    }

    private void RotateCoreToNeutral(float delta)
    {
        _audioJammed.Stop();
        _coreAnimPlayer.Stop();
        if (_isAttemptingTurn)
        {
            _pryAnimPlayer.PlayBackwards("turn");
            _isAttemptingTurn = false;
        }

        if (_lockCore.Rotation.x != 0)
        {
            _coreRotationTarget = 0;
            _lockCore.RotateX(delta * _rotationSpeedCore);
            _lockCore.Rotation = new Vector3(
                Mathf.Clamp(_lockCore.Rotation.x, _CORE_UNLOCK_ANGLE, 0),
                0, 0
            );
            _debugInfo.CoreRotation = _lockCore.Rotation.x;
            _debugInfo.HookScreenLocation = _camera.UnprojectPosition(_lockHook.GlobalTranslation);
            _debugInfo.HookAngle = _lockHook.Rotation.x;
            _debugInfo.Update();
            _debugInfo.ShowCorrectnessLabel(false);
        }
        else
        {
            _isLocked = true;
        }
    }

    private void RotateLockCore(float delta, float correctness)
    {
        float targetRotation = correctness * _CORE_UNLOCK_ANGLE;
        // To prevent the core from rotating back towards the initial rotation
        _coreRotationTarget = Mathf.Min(targetRotation, _coreRotationTarget);

        _lockCore.RotateX(delta * -_rotationSpeedCore);
        _lockCore.Rotation = new Vector3(
            Mathf.Clamp(_lockCore.Rotation.x, _coreRotationTarget, 0),
            0, 0
        );

        _debugInfo.CoreRotation = _lockCore.Rotation.x;
        _debugInfo.HookAngle = _lockHook.Rotation.x;
        _debugInfo.HookScreenLocation = _camera.UnprojectPosition(_lockHook.GlobalTranslation);
        _debugInfo.Update();
    }

    private void LockJam(float delta, float correctness)
    {   
        _audioJammed.Play(_audioJammed.Stream.GetLength() * GD.Randf());
        _coreAnimPlayer.Play("jammed_loop");
        float hookDamageRange = _hookDamagePerSecondMax - _hookDamagePerSecondMin;
        float hookDamage = ((1 - correctness) * hookDamageRange) + _hookDamagePerSecondMin;
        HookHealth = _hookHealth - (hookDamage * delta);
    }

    private void Unlock()
    {
        _audioUnlock.Play();
        _isLocked = false;
        GD.Print("Unlocked");
    }

    private float CalcCorrectnessScore()
    {
        float allowedMax = -_solutionAngle + _allowedDeviation;
        float allowedMin = -_solutionAngle - _allowedDeviation;
        if ((_lockHook.Rotation.x > allowedMin) && (_lockHook.Rotation.x < allowedMax))
        {
            return 1;
        }
        
        float proximityMax = allowedMax + _proximityDeviation;
        float proximityMin = allowedMin - _proximityDeviation;
        if ((_lockHook.Rotation.x > proximityMin) && (_lockHook.Rotation.x < allowedMin))
        {
            float angleToAllowed = Mathf.Abs(_lockHook.Rotation.x - allowedMin);
            return 1 - (angleToAllowed / _proximityDeviation);
        }
        else if ((_lockHook.Rotation.x > allowedMax) && (_lockHook.Rotation.x < proximityMax))
        {
            float angleToAllowed = Mathf.Abs(allowedMax - _lockHook.Rotation.x);
            return 1 - (angleToAllowed / _proximityDeviation);
        }
        
        return 0;
    }

    private void AttemptUnlock(float delta)
    {   
        if (!_isAttemptingTurn)
        {
            _pryAnimPlayer.Play("turn");
            _isAttemptingTurn = true;
        }

        float correctness = CalcCorrectnessScore();
        if (correctness > 0)
        {
            RotateLockCore(delta, correctness);
        }

        if (correctness != 1 && (_coreRotationTarget >= _lockCore.Rotation.x))
        {
            // Only jam when the hook is at or beyond the core rotation allowed by the current hook angle
            LockJam(delta, correctness);
        }
        else if (correctness == 1 && _lockCore.Rotation.x == _CORE_UNLOCK_ANGLE && _isLocked)
        {
            _audioJammed.Stop();
            _coreAnimPlayer.Stop();
            Unlock();  
        }
        
        _debugInfo.CorrectnessScore = correctness;
        _debugInfo.ShowCorrectnessLabel(true);
    }

    private async void LoadNewHook()
    {
        _isLoadingNewHook = true;
        GD.Print("!! Hook Broke !!");
        _audioHookSnap.Play();
        _hookAnimPlayer.Play("load_new_hook");
        await ToSignal(_hookAnimPlayer, "animation_finished");
        HookHealth = _hookHealthMax;
        _isLoadingNewHook = false;
    }
}

