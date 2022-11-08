using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


public class LockPickSimpleDebugInfo : Control
{
    public float RotationMin{set; private get;}

    public float RotationMax{set; private get;}

    public float RotationTarget{set; private get;}

    public float AllowedDeviation{set; private get;}

    public float ProximityDeviation{set; private get;}

    public float CoreRotation{set; private get;}

    public Vector2 HookScreenLocation{set; private get;}

    public float HookAngle{set; private get;}

    public float CorrectnessScore{
        set{
            _labelCorrectness.Text = value.ToString();
        }
    }

    public float HookHealth{
        set{
            _labelHookHealth.Text = Mathf.Stepify(value, 0.01f).ToString();
        }
    }


    private float _drawRotationCorrection = Mathf.Deg2Rad(-90);
    private float _debugArcRadius = 200f;
    private float _debugArcWidth = 10f;
    private int _debugArcPointCount = 300;
    private float _debugHookAngelWidth = 60f;
    private Label _labelCorrectness;
    private Label _labelHookHealth;


    public override void _Ready()
    {
        _labelCorrectness = GetNode<Label>("Correctness");
        _labelHookHealth = GetNode<Label>("HookHP");
    }


    public override void _Draw()
    {   
        float start = -CoreRotation + RotationMin + _drawRotationCorrection;
        float end = -CoreRotation + RotationMax + _drawRotationCorrection;

        DrawArc(
            HookScreenLocation, _debugArcRadius, start, end, 
            _debugArcPointCount, Colors.Black, _debugArcWidth
        );

        float proximity_start = -CoreRotation + _drawRotationCorrection + RotationTarget - (
            AllowedDeviation + ProximityDeviation
        );
        proximity_start = Mathf.Clamp(proximity_start, start, end);
        float proximity_end = -CoreRotation + _drawRotationCorrection + RotationTarget + (
            AllowedDeviation + ProximityDeviation
        );
        proximity_end = Mathf.Clamp(proximity_end, start, end);

        DrawArc(
            HookScreenLocation, _debugArcRadius, proximity_start, proximity_end, 
            _debugArcPointCount, Colors.Yellow, _debugArcWidth
        );

        float allowed_start = -CoreRotation + _drawRotationCorrection + RotationTarget - AllowedDeviation;
        allowed_start = Mathf.Clamp(allowed_start, start, end);
        float allowed_end = -CoreRotation + _drawRotationCorrection + RotationTarget + AllowedDeviation;
        allowed_end = Mathf.Clamp(allowed_end, start, end);

        DrawArc(
            HookScreenLocation, _debugArcRadius, allowed_start, allowed_end, 
            _debugArcPointCount, Colors.Red, _debugArcWidth
        );

        float hookStart = -CoreRotation - HookAngle + Mathf.Deg2Rad(0.25f) + _drawRotationCorrection;
        float hookEnd = -CoreRotation - HookAngle - Mathf.Deg2Rad(0.25f) + _drawRotationCorrection;

        DrawArc(
            HookScreenLocation, _debugArcRadius, hookStart, hookEnd, 
            3, Colors.Green, _debugHookAngelWidth
        );
    }

    public void ShowCorrectnessLabel(bool show)
    {
        _labelCorrectness.Visible = show;
    }
}

