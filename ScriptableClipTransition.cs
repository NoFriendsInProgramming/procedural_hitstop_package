using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

[CreateAssetMenu(fileName = "ClipTransition", menuName = "AnimancerUtilities/ScriptableClipTransition", order = 100)]
public class ScriptableClipTransition : ScriptableObject
{
    [SerializeField] ClipTransition transition;

    public AnimationClip clip => transition.Clip;
    public ClipTransition Transition => transition;

    public static implicit operator ClipTransition(ScriptableClipTransition scriptableTransition) => scriptableTransition.transition;

    
}
