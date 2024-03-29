﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Meta.Mouse
{
    /// <summary>
    /// Handles positional translation of an object via pointer events. Left click to translate X and Y. 
    /// Left click and scroll to translate Z.
    /// </summary>
    public class DragTranslate : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// The transform to be translated.
        /// </summary>
        [SerializeField]
        private Transform _translateTransform;

        public Transform TranslateTransform
        {
            get { return _translateTransform; }
            set { _translateTransform = value; }
        }

        /// <summary>
        /// The increment at which the transform should be translated in depth using the scrollwheel.
        /// </summary>
        [SerializeField]
        private float _depthScrollIncrement = .08f;

        /// <summary>
        /// The max distance objects can be translated away from the camera.
        /// </summary>
        [SerializeField]
        private float _maxCameraDistance = 1.5f;

        /// <summary>
        /// Whether the transform should point at the event camera when a translation begins.
        /// </summary>
        [SerializeField]
        private bool _lookAtYOnDrag = true;

        /// <summary>
        /// Input Button that activates translation
        /// </summary>
        [SerializeField]
        private PointerEventData.InputButton _button = PointerEventData.InputButton.Left;
        /// <summary>
        /// Keys that should not be pressed for translation to occur
        /// </summary>
        [SerializeField]
        private KeySet _notPressedKeys;

        /// <summary>
        /// Event called when a pointer is first held down.
        /// </summary>
        private MetaInteractionDataEvent _onPointerDownEvent = new MetaInteractionDataEvent();

        /// <summary>
        /// Event called when a pointer is released from being held down.
        /// </summary>
        private MetaInteractionDataEvent _onPointerUpEvent = new MetaInteractionDataEvent();

        /// <summary>
        /// The minimum allowable distance an object can be translated depth-wise from the user.
        /// </summary>
        private const float MinCameraDistance = .2f;

        private Vector3 _priorPointerWorldPosition;
        private float _beginDistance;
        private Coroutine _heldCoroutine;

        /// <summary>
        /// Event called when a pointer is first held down.
        /// </summary>
        public MetaInteractionDataEvent OnPointerDownEvent
        {
            get { return _onPointerDownEvent; }
        }

        /// <summary>
        /// Event called when a pointer is released from being held down.
        /// </summary>
        public MetaInteractionDataEvent OnPointerUpEvent
        {
            get { return _onPointerUpEvent; }
        }

        private void Awake()
        {
            if (_translateTransform == null)
            {
                _translateTransform = transform;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!(eventData is MetaHandEventData) && eventData.button == _button)
            {
                eventData.useDragThreshold = false;
                _heldCoroutine = StartCoroutine(HeldCoroutine(eventData));

                if (_onPointerDownEvent != null)
                {
                    _onPointerDownEvent.Invoke(new MetaInteractionData(eventData, null));
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!(eventData is MetaHandEventData) && eventData.button == _button)
            {
                Ray cameraRay = eventData.pressEventCamera.ScreenPointToRay(eventData.pointerCurrentRaycast.screenPosition);
                SetBeginDistance(cameraRay, eventData);

            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!(eventData is MetaHandEventData) && eventData.button == _button && (_notPressedKeys == null || !_notPressedKeys.IsPressed()))
            {
                Ray cameraRay = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
                Vector3 worldPosition = cameraRay.GetPoint(_beginDistance);
                Vector3 delta = worldPosition - _priorPointerWorldPosition;
                if (_lookAtYOnDrag)
                {
                    _translateTransform.LookAtY(Camera.main.transform.position);
                }
                _translateTransform.Translate(delta, Space.World);
                _priorPointerWorldPosition = worldPosition;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!(eventData is MetaHandEventData) && eventData.button == _button)
            {
                StopCoroutine(_heldCoroutine);
                if (_onPointerUpEvent != null)
                {
                    _onPointerUpEvent.Invoke(new MetaInteractionData(eventData, null));
                }
            }
        }

        private IEnumerator HeldCoroutine(PointerEventData eventData)
        {
            while (true)
            {
                yield return null;
                float scrollAmount = Input.GetAxis("Mouse ScrollWheel");

                if (scrollAmount != 0)
                {
                    ScrollTranslateObject(eventData, scrollAmount);
                }
            }
        }

        /// <summary>
        /// Translates an object depth-wise based on the pointer and mousewheel input.
        /// </summary>
        /// <param name="eventData">Pointer down data for scroll translation.</param>
        /// <param name="amount">The value of the mouse scrollwheel (0 for backwards and 1 for forwards).</param>
        private void ScrollTranslateObject(PointerEventData eventData, float amount)
        {
            Ray cameraRay = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
            Vector3 translation = cameraRay.direction * amount * _depthScrollIncrement;
            Vector3 newPosition = translation + _translateTransform.position;

            float distance = Vector3.Distance(newPosition, eventData.pressEventCamera.transform.position);

            if ((distance > MinCameraDistance && amount < 0) || (distance < _maxCameraDistance && amount > 0))
            {
                _translateTransform.position = newPosition;
                SetBeginDistance(cameraRay, eventData);
            }
        }

        /// <summary>
        /// Updates the distance to be used when calculating translation amounts.
        /// </summary>
        /// <param name="cameraRay">Ray from the camera to the last pointer down event position.</param>
        /// <param name="eventData">Event data associated with the last pointer down event.</param>
        private void SetBeginDistance(Ray cameraRay, PointerEventData eventData)
        {
            float distance;
            //Raycast against an infinite plane because there are instances where the user
            //will drag the mouse in such a way that it's raycast no longer intersects with
            //the button it initially clicked on.
            Plane pressPlane = new Plane(-cameraRay.direction, eventData.pointerPressRaycast.gameObject.transform.position);
            pressPlane.Raycast(cameraRay, out distance);
            _beginDistance = distance;
            _priorPointerWorldPosition = cameraRay.GetPoint(_beginDistance);
        }
    }
}