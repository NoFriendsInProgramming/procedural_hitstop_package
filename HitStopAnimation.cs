using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Animancer;
using UnityEngine.Animations;
//using System;

public class HitStopAnimation : MonoBehaviour
{
    
    struct IkController
    {
        public ChainIKConstraint constraint;
        public Transform impactPointReference;

        public IkController(ChainIKConstraint constraint, Transform impactPointReference)
        {
            this.constraint = constraint;
            this.impactPointReference = impactPointReference;
        }
    }

    bool hasBeenInialized = false;

    Transform target;
    Dictionary<Transform, IkController> ikControllers;
    Rig rig;
    RigBuilder builder;
    GameObject ikGameObject;
    ChainIKConstraint ik;
    [SerializeField] Transform[] manuallyPlacedHitstopPoints;
    [SerializeField] Transform tip;
    [SerializeField] Transform root;
    [SerializeField] bool hasHitStop = true;
    [SerializeField] float hitstopDuration = 0.1f;
    [SerializeField] float hitstopSpeedMultiplier = 0.2f;
    [SerializeField] float hitstopReturnDuration = 0.3f;
    [SerializeField] bool hasJitter = false;
    [SerializeField] float timeJitter = 0.1f;
    [SerializeField] [Range(0,1f)] float animationSpeed = 1;
    [SerializeField] AnimationCurve returnCurve;
    [field: SerializeField] public ScriptableClipTransition scriptableTransition { get; private set; }

    HaltableRig _referenceRig;
    HaltableRig haltableRig;
    HaltableRig referenceRig => _referenceRig ??= CreateReferenceRig();
    HaltableRig CreateReferenceRig()
    {
        var rig = Instantiate(haltableRig);
        Destroy(rig.GetComponentInChildren<Rig>()?.gameObject);
        rig.transform.SetParent(transform, false);
        rig.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach(var renderer in rig.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.enabled = false;
        }
        return rig;
    }


    private void Awake()
    {
        if(manuallyPlacedHitstopPoints != null && manuallyPlacedHitstopPoints.Length > 0)
        {
            Initialize(manuallyPlacedHitstopPoints, root);
        }
        //PrepareIK();
    }

    public void IncurHitStop(Transform impactPoint, float delay = 0.9f)
    {
        StartCoroutine(HitStop(impactPoint, delay));
    }

    public void Initialize(Transform[] hitstopPoints, Transform ikRoot)
    {
        if(!hasBeenInialized)
        {
            var animator = GetComponentInChildren<Animator>();
            haltableRig = animator.GetComponent<HaltableRig>();
            if(haltableRig == null)
            {
                haltableRig = animator.AddComponent<HaltableRig>();
            }

            hasBeenInialized = true;
            root = ikRoot;
            builder = haltableRig.AddComponent<RigBuilder>();

            rig = new GameObject("Rig").AddComponent<Rig>();
            rig.transform.SetParent(haltableRig.transform);
            rig.weight = 1;
            builder.layers.Add(new RigLayer(rig, true));

            target = new GameObject("Target").transform;

            haltableRig.hitstopPoints = hitstopPoints;

            ikControllers = new Dictionary<Transform, IkController>();
            for(int i = 0; i < hitstopPoints.Length; i++)
            {
                CreateChainIkConstraint(i, ikRoot);
            }

            builder.Build();

        }
    }

    void CreateChainIkConstraint(int hitstopPointIndex, Transform ikRoot)
    {
        Transform hitstopPoint = haltableRig.hitstopPoints[hitstopPointIndex];
        if(!ikControllers.ContainsKey(hitstopPoint))
        {
            ChainIKConstraint ikConstraint;
            ikControllers.Add(hitstopPoint, 
                              new IkController(ikConstraint = (ikGameObject = new GameObject("IK" + hitstopPoint.name)).AddComponent<ChainIKConstraint>(), 
                              referenceRig.hitstopPoints[hitstopPointIndex]));
            ikConstraint.Reset();
            ikConstraint.transform.SetParent(rig.transform);
            ikConstraint.data.target = target;
            ikConstraint.data.root = root;
            ikConstraint.data.tip = hitstopPoint;
            ikConstraint.weight = 0;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        if(scriptableTransition != null)
        {
            scriptableTransition.Transition.Events.OnEnd += PlayAnimation;
            PlayAnimation();
        }
        
    }

    // Update is called once per frame
    void Update()
    {      
        Time.timeScale = animationSpeed;
    }

    void PlayAnimation()
    {
        haltableRig.PlayAnimation(scriptableTransition);
        referenceRig.PlayAnimation(scriptableTransition);
        if(hasHitStop) IncurHitStop(manuallyPlacedHitstopPoints[Random.Range(0,2)]);
    }


    void MatchIKTargetWithBone(Transform bone)
    {
        target.SetParent(bone);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.Euler(Vector3.zero);
    }

    IEnumerator HitStop(Transform impactPoint, float delay = 0)
    {
        //Destroy(ik);
        yield return delay == 0 ? null : new WaitForSeconds(delay);

        if (ikControllers.ContainsKey(impactPoint))
        {
            ChainIKConstraint ikConstraint = ikControllers[impactPoint].constraint;
            /*
            ik = ikGameObject.AddComponent<ChainIKConstraint>();
            ik.Reset();
            ik.data.target = target;
            ik.data.root = root;
            ik.data.tip = impactPoint;

            haltableRig.animator.enabled = false;
            builder.Build();
            haltableRig.animator.enabled = true;
            */
            //SetNewPointOfImpact(impactPoint);

            ikConstraint.weight = 1;
            //float animationTimeBeforeHitStop = referenceRig.CurrentAnimationTime();
            MatchIKTargetWithBone(ikControllers[impactPoint].impactPointReference);// referenceRig.pointOfImpact);

            referenceRig.SetCurrentAnimationSpeed(hitstopSpeedMultiplier);

            //
            if (hasJitter)
            {
                for (float i = 0; i <= hitstopDuration; i += Time.deltaTime)
                {
                    referenceRig.MoveAnimationTime((2 * Random.Range(0, 2) - 1) * timeJitter);
                    yield return null;
                }

                //referenceRig.SetAnimationTime(animationTimeBeforeHitStop + hitstopDuration * hitstopSpeedMultiplier);
            }
            else
            {
                yield return new WaitForSeconds(hitstopDuration);
            }

            float postImpactTime = referenceRig.CurrentAnimationTime();
            var durationToCatchUpOn = (1 - hitstopSpeedMultiplier) * hitstopDuration + hitstopReturnDuration;
            var normalizeMultiplier = 1 / hitstopReturnDuration;

            for (float i = 0; i <= hitstopReturnDuration; i += Time.deltaTime)
            {
                referenceRig.SetAnimationTime(postImpactTime + (returnCurve.Evaluate(i * normalizeMultiplier) * durationToCatchUpOn));
                Debug.Log(i + " Normalized = " + i * normalizeMultiplier);
                yield return null;
            }

            referenceRig.SetAnimationTime(haltableRig.CurrentAnimationTime());
            referenceRig.SetCurrentAnimationSpeed();

            //MatchIKTargetWithTransform();
            ikConstraint.weight = 0;
        }
        else
        {
            Debug.LogError("Point of impact inputted for the hitstop mechanism not found in the original list of usable transforms."
                + " If you want to use this transform to create a hitstop effect make sure to pass it to the Initialize function or in the manuallyPlacedHitstopPoints array variable");
        }
    
        
    }

}
