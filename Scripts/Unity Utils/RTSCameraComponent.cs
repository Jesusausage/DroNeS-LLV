﻿using UnityEngine;
using System.Collections;
using Drones.Interface;

namespace Drones
{
    using UI;
    using static Singletons;
    public class RTSCameraComponent : AbstractCamera
    {
        [SerializeField]
        private float _FollowDistance = 3f;
        public float FollowDistance
        {
            get
            {
                return _FollowDistance;
            }
            set
            {
                if (_FollowDistance > 0)
                {
                    _FollowDistance = value;
                }
            }
        }

        private void OnEnable()
        {
            EagleEye.gameObject.SetActive(false);
            transform.position = EagleEye.transform.position;
            ActiveCamera = this;
        }

        private void Update()
        {
            Controller.MoveLongitudinal(Input.GetAxis("Vertical") * SpeedScale);
            Controller.MoveLateral(Input.GetAxis("Horizontal") * SpeedScale);
            Controller.Rotate(Input.GetAxis("Rotate"));

            if (UIFocus.hover == 0)
            {
                Controller.Zoom(Input.GetAxis("Mouse ScrollWheel") * SpeedScale);
                //FPS mouse hold click
                if (Input.GetMouseButton(0) && !UIFocus.Controlling)
                {
                    if (!Controlling) 
                    {
                        StartCoroutine(ControlListener()); 
                    }
                    Controller.Pitch(Input.GetAxis("Mouse Y"));
                    Controller.Yaw(Input.GetAxis("Mouse X"));
                    Controller.ClampPitch();
                }
            }

            // Bounds
            Controller.ClampVertical();

            if (!_Following && Followee != null) { StartCoroutine(FollowObject()); }
        }

        protected override IEnumerator FollowObject()
        {
            _Following = true;
            while (!Input.GetKeyDown(KeyCode.Escape))
            {
                transform.position = Followee.transform.position - CameraTransform.forward * FollowDistance;
                yield return null;
            }
            _Following = false;
            Followee = null;
            yield break;
        }

        #region ICameraMovement Implementation
        public override void Zoom(float input)
        {
            Vector3 positiveDirection = CameraTransform.forward;
            // Cannot zoom when facing up
            if (positiveDirection.y < 0)
            {
                transform.position += input * positiveDirection * UnityEngine.Time.unscaledDeltaTime;
            }
        }

        public override void Pitch(float input)
        {
            transform.Rotate(input, 0, 0);
        }

        public override void Yaw(float input)
        {
            transform.Rotate(0, input, 0, Space.World);
        }

        public override void Rotate(float input)
        {
            float scale = (Controller.Floor - transform.position.y) / CameraTransform.forward.y;
            Vector3 point = transform.position + CameraTransform.forward * scale;
            transform.RotateAround(point, Vector3.up, input);
        }

        public override void ClampVertical(float lowerBound, float upperBound)
        {
            Vector3 position = transform.position;
            position.y = Mathf.Clamp(position.y, lowerBound, upperBound);
            transform.position = position;
        }

        public override void ClampPitch(float lowerAngle, float upperAngle)
        {
            Vector3 front = Vector3.Cross(CameraTransform.right, Vector3.up).normalized;
            if (CameraTransform.forward.y > 0)
            {
                float up = Vector3.Angle(front, CameraTransform.forward);
                if (up > upperAngle)
                {
                    transform.rotation *= Quaternion.AngleAxis(up - upperAngle, Vector3.right);
                }
                if (lowerAngle < 0 && up < -lowerAngle)
                {
                    transform.rotation *= Quaternion.AngleAxis(up + lowerAngle, Vector3.right);
                }
            }
            else
            {
                float down = Vector3.Angle(front, CameraTransform.forward);
                if (down > lowerAngle)
                {
                    transform.rotation *= Quaternion.AngleAxis(down - lowerAngle, -Vector3.right);
                }
                if (upperAngle < 0 && down < -upperAngle)
                {
                    transform.rotation *= Quaternion.AngleAxis(down + upperAngle, -Vector3.right);
                }
            }
        }

        #endregion

    }
}