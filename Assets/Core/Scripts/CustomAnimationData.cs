﻿using UnityEngine;

[CreateAssetMenu(fileName = "New Custom Animation Data", menuName = "CustomAnimData", order = 51)]
public class CustomAnimationData : ScriptableObject
{
    [Tooltip("The parameter to be toggled in the animator")]
    public string animationToggleName;
    [Tooltip("How long the animation takes to complete")]
    public float executionTime;
    [Tooltip("If set to true, then another custom animation can stop the current one (including itself)")]
    public bool canBeInterrupted; //Probably don't need

    [Space(10), Tooltip("If set to true, will stop movement through direct input")]
    public bool blockMovement;
    [Tooltip("If set to true, will stop rotation through direct input")]
    public bool blockRotation;

    [Space(10), Tooltip("How much distance to cover over the execution time")]
    public float distanceTravelled;
    [Tooltip("How to map out the distance travelled based on the time passed")]
    public AnimationCurve travelMap = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
}
