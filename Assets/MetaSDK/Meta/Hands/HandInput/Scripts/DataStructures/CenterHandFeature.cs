﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.HandInput
{
    public class CenterHandFeature : HandFeature
    {
        #region Member variables

        /// <summary>
        /// Unity event for when a grab occurs.
        /// </summary>
        [SerializeField]
        private HandFeatureEvent _onEngaged = new HandFeatureEvent();

        /// <summary>
        /// Unity event for when a release occurs.
        /// </summary>
        [SerializeField]
        private HandFeatureEvent _onDisengaged = new HandFeatureEvent();

        private bool _isNearObject;
        private bool _isGrabbing;

        private HandsProvider _handProvider;
        private readonly PalmStateMachine _palmState = new PalmStateMachine();

        private readonly List<Interaction> _previousNearObjects = new List<Interaction>();
        private readonly List<Interaction> _nearObjects = new List<Interaction>();
        private readonly List<Interaction> _grabbedInteractionBehaviours = new List<Interaction>();

        private const int ColliderBufferSize = 16;
        private readonly Collider[] _buffer = new Collider[ColliderBufferSize];

        /// <summary>
        /// A reference to the nearest gameobject for some state transitions in which the nearest GameObject
        /// is not known but had been previously known. 
        /// </summary>
        private GameObject _cachedNearestGameObject;

        private HandObjectReferences _handObjectReferences;

        #endregion

        #region Member properties

        /// <summary>
        /// The number of interactive objects being grabbed
        /// </summary>
        public int NumberOfGrabbedObjects
        {
            get { return _grabbedInteractionBehaviours.Count; }
        }

        /// <summary>
        /// The position of the center of the hand
        /// </summary>
        public override Vector3 Position
        {
            get { return HandData.Palm; }
        }

        /// <summary>
        /// Current state of the palm.
        /// </summary>
        public PalmState PalmState
        {
            get { return _palmState.CurrentState; }
        }

        /// <summary>
        /// Unity event for when a grab occurs
        /// </summary>
        public HandFeatureEvent OnEngagedEvent
        {
            get { return _onEngaged; }
        }

        /// <summary>
        /// Unity event for when a grab ends (a release)
        /// </summary>
        public HandFeatureEvent OnDisengagedEvent
        {
            get { return _onDisengaged; }
        }

        /// <summary>
        /// Is the hand's center currently near any interactible objects
        /// </summary>
        public bool IsNearObject
        {
            get { return _isNearObject; }
        }


        /// <summary>
        /// List of closest interaction objects, if any.
        /// </summary>
        public List<Interaction> NearObjects
        {
            get
            {
                return _nearObjects;
            }
        }

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            // Check if HandCursor exist and if not, add it.
            var cursor = GetComponent<HandCursor>();
            if(cursor == null)
            {
                gameObject.AddComponent<HandCursor>();
            }

            _handProvider = FindObjectOfType<HandsProvider>();

            _palmState.OnHoverEnter += HoverStart;
            _palmState.OnHoverExit += HoverEnd;
            _palmState.OnGrabStart += GrabStart;
            _palmState.OnGrabEnd += GrabEnd;
            _handObjectReferences = metaContext.Get<HandObjectReferences>();
            _palmState.Initialize();
        }

        protected override void Update()
        {
            base.Update();

            MaintainState();
        }

        #endregion

        #region Member Methods


        /// <summary>
        /// Event to get fired when hand leaves the scene
        /// </summary>
        public override void OnInvalid()
        {
            switch (_palmState.CurrentState)
            {
                case PalmState.Idle:
                    // Do nothing
                    break;
                case PalmState.Hovering:
                    HoverEnd();
                    _handObjectReferences.AcceptStateTransitionForObject(_cachedNearestGameObject, PalmState.Hovering, PalmState.Idle);
                    break;
                case PalmState.Grabbing:
                    GrabEnd();
                    HoverEnd();
                    _handObjectReferences.AcceptStateTransitionForObject(_cachedNearestGameObject, PalmState.Grabbing, PalmState.Idle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void MaintainState()
        {
            switch (_palmState.CurrentState)
            {
                case PalmState.Idle:

                    // Find nearby objects
                    FindObjectsWithinVicinity(HandsSettings.settings.PalmRadiusNear);

                    // Check if any
                    _isNearObject = _nearObjects.Count > 0;

                    // Update pre-grab requirements
                    if (Hand.GrabValue < _handProvider.settings.ReleaseThreshold)
                    {
                        return;
                    }

                    if (_isNearObject)
                    {
                        MoveStateMachine(PalmStateCommand.HoverEnter);
                    }

                    break;
                case PalmState.Hovering:
                    _isGrabbing = Hand.GrabValue <= _handProvider.settings.GrabThreshold;

                    if (_isGrabbing)
                    {
                        MoveStateMachine(PalmStateCommand.Grab);
                        return;
                    }

                    // Hand is not grabbing. Check hover state.

                    // Find nearby objects
                    FindObjectsWithinVicinity(HandsSettings.settings.PalmRadiusFar);

                    var isPreviousNearObject = _previousNearObjects.Count > 0
                                               &&
                                               _nearObjects.Any(
                                                   attachedInteraction =>
                                                       _previousNearObjects.Contains(attachedInteraction));
                    if (!isPreviousNearObject)
                    {
                        foreach (var previousNearObject in _previousNearObjects)
                        {
                            previousNearObject.OnHoverEnd(Hand);
                        }

                        foreach (var nearObject in _nearObjects)
                        {
                            nearObject.OnHoverStart(Hand);
                        }

                        if (_previousNearObjects.Count > 0 && _nearObjects.Count > 0)
                        {
                            _handObjectReferences.AcceptStateTransitionForObject(_previousNearObjects[0].gameObject, PalmState.Hovering, PalmState.Idle);
                            _handObjectReferences.AcceptStateTransitionForObject(_nearObjects[0].gameObject, PalmState.Idle, PalmState.Hovering);
                        }
                    }

                    // Check if any
                    _isNearObject = _nearObjects.Count > 0;

                    if (!_isNearObject)
                    {
                        MoveStateMachine(PalmStateCommand.HoverLeave);
                    }

                    break;
                case PalmState.Grabbing:
                    _isGrabbing = Hand.GrabValue <= _handProvider.settings.ReleaseThreshold;

                    if (!_isGrabbing)
                    {
                        MoveStateMachine(PalmStateCommand.Release);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveStateMachine(PalmStateCommand command)
        {
            PalmState beforeState = _palmState.CurrentState;
            _palmState.MoveNext(command);
            PalmState afterState = _palmState.CurrentState;

            GameObject nearestGameObject = null;
            if ((beforeState == PalmState.Grabbing || beforeState == PalmState.Hovering) && afterState == PalmState.Idle)
            {
                nearestGameObject = _cachedNearestGameObject;
            }
            else
            {
                _cachedNearestGameObject = Hand.Palm.NearObjects[0].gameObject;
                nearestGameObject = _cachedNearestGameObject;
            }

            if (!nearestGameObject)
            {
                Debug.LogError("Could not reference the nearest gameobject");
                return;
            }

            _handObjectReferences.AcceptStateTransitionForObject(nearestGameObject, beforeState, afterState);
        }

        private void GrabStart()
        {
            // Fire centralized grab event
            _handProvider.events.OnGrab.Invoke(Hand);

            // Fire object's grab event
            _onEngaged.Invoke(this);

            // Notify all near objects of grab
            foreach (var interactionBehaviour in _nearObjects)
            {
                // Store grabbed Object reference 
                _grabbedInteractionBehaviours.Add(interactionBehaviour);

                // Invoke OnGrab Event
                interactionBehaviour.OnGrabEngaged(Hand);
            }

            // Mark grab
            _isGrabbing = true;
        }

        private void GrabEnd()
        {
            // Fire centralized release event
            _handProvider.events.OnRelease.Invoke(Hand);

            // Fire object's release event
            _onDisengaged.Invoke(this);

            // Notify all grabbed objects of release
            foreach (var interactionBehaviour in _grabbedInteractionBehaviours)
            {
                // Invoke OnRelease Event
                interactionBehaviour.OnGrabDisengaged(Hand);
            }

            // [try] Remove grabbed Object reference 
            _grabbedInteractionBehaviours.Clear();

            // Mark release
            _isGrabbing = false;
        }

        private void HoverStart()
        {
            foreach (var interactibleObject in _nearObjects)
            {
                interactibleObject.OnHoverStart(Hand);
            }
        }

        private void HoverEnd()
        {
            var hoveredObjects = _previousNearObjects.Concat(_nearObjects);

            foreach (var interactibleObject in hoveredObjects)
            {
                interactibleObject.OnHoverEnd(Hand);
            }
        }

        private void FindObjectsWithinVicinity(float searchRadius)
        {
            _previousNearObjects.Clear();
            for (int i = 0; i < _nearObjects.Count; i++)
            {
                _previousNearObjects.Add(_nearObjects[i]);
            }
            _nearObjects.Clear();

            Interaction[] closestInteractions = null;
            var closestCollider = float.MaxValue;

            var queryTriggers = HandsSettings.settings.QueryTriggers;
            var layerMask = HandsSettings.settings.QueryLayerMask;


            var grabAnchor = HandData.GrabAnchor;
            var nearColliderCount = Physics.OverlapSphereNonAlloc(grabAnchor, searchRadius, _buffer, layerMask, queryTriggers);

            for (int i = 0; i < nearColliderCount; i++)
            {
                var nearCollider = _buffer[i];
                var parentInteraction = nearCollider.GetComponentInParent<Interaction>();
                // ensure valid grabbed object.
                if (parentInteraction == null)
                {
                    continue;
                }

                var attachedInteractions = parentInteraction.GetComponents<Interaction>();
                // Collect all attached interactions
                if (attachedInteractions == null)
                {
                    continue;
                }

                // Ensure near collider is not a Hand Feature.
                if (nearCollider.GetComponent<HandFeature>())
                {
                    continue;
                }

                var isPreviousNearObject = attachedInteractions.Any(attachedInteraction => _previousNearObjects.Contains(attachedInteraction));

                // Find closest point on collider.
                var closestPoint = nearCollider.ClosestPointOnBounds(transform.position);
                var distanceToObject = (closestPoint - transform.position).magnitude;

                if (isPreviousNearObject)
                {
                    distanceToObject -= HandsSettings.settings.ClosestObjectDebounce;
                }

                // Limit to one grabbed object.
                if (distanceToObject < closestCollider)
                {
                    closestCollider = distanceToObject;
                    closestInteractions = attachedInteractions;
                }
            }

            if (closestInteractions != null)
            {
                _nearObjects.AddRange(closestInteractions);
            }
        }

        #endregion
    }
}