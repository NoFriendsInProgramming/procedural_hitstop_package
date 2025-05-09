using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaltableRig : MonoBehaviour
{
    [HideInInspector] public Transform[] hitstopPoints;

    AnimancerComponent _animancer;
    AnimancerState currentState;
    AnimancerComponent animancer => _animancer ??= GetComponent<AnimancerComponent>();

    public Animator animator => animancer.Animator;

    public void PlayAnimation(ClipTransition transition) => currentState = animancer.Play(transition);
    public float CurrentAnimationTime() => currentState.Time;
    public void HaltAnimation()   => currentState.Speed = 0;
    public void SetCurrentAnimationSpeed(float speed = 1) => currentState.Speed = speed;

    public void MoveAnimationTime(float deltaTime) => currentState.Time += deltaTime;
    public void SetAnimationTime(float time) => currentState.Time = time;



}
