using UnityEngine;
using QuizCanners.Inspect;
using QuizCanners.Lerp;
using System;
using System.Diagnostics.Contracts;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0018 // Inline variable declaration

    public interface IGodModeCameraController 
    {
        Vector3 GetTargetPosition();
        Vector3 GetCameraOffsetPosition();
        bool TryGetCameraHeight(out float height);
    }

    [ExecuteInEditMode]
    public class Singleton_CameraOperatorGodMode : Singleton.BehaniourBase, IPEGI
    {
        public float speed = 20;
        public float offsetClip = 0;
        public float sensitivity = 5;
        public bool _disableRotation;
        public bool rotateWithoutRmb;
        public bool simulateFlying;
        public bool _onlyInEditor;
        public Vector2 camOrbit;
        public Vector3 spinCenter;
        private float _orbitDistance;
        public bool orbitingFocused;
        public float spinStartTime;

        [SerializeField] protected Camera _mainCam;
       
        public override string InspectedCategory => Utils.Singleton.Categories.SCENE_MGMT;

        public Camera MainCam
        {
            get
            {
                if (!_mainCam)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }

        private void SpinAround()
        {

            var camTr = _mainCam.transform;

            if (Input.GetMouseButtonDown(2))
            {
                var ray = MainCam.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    spinCenter = hit.point;
                else return;

                var before = camTr.localRotation;
                camTr.LookAt(spinCenter);
                var rot = camTr.localRotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                _orbitDistance = (spinCenter - transform.position).magnitude;

                camTr.rotation = before;
                orbitingFocused = false;
                spinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                _orbitDistance = 0;

            if ((!(_orbitDistance > 0)) || (!Input.GetMouseButton(2)))
                return;
            
            camOrbit.x += Input.GetAxis("Mouse X") * 5;
            camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

            if (camOrbit.y <= -360)
                camOrbit.y += 360;
            if (camOrbit.y >= 360)
                camOrbit.y -= 360;

            var rot2 = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
            var campos = rot2 *
                             (new Vector3(0.0f, 0.0f, -_orbitDistance)) +
                             spinCenter;

            transform.position = campos;
            if (!orbitingFocused)
            {
                camTr.localRotation = LerpUtils.LerpBySpeed(camTr.localRotation, rot2, 200, unscaledTime: true);
                if (Quaternion.Angle(camTr.localRotation, rot2) < 1)
                    orbitingFocused = true;
            }
            else camTr.localRotation = rot2;
            
        }

        protected virtual void OnUpdateInternal() 
        {
            var operatorTf = transform;
            var camTf = _mainCam.transform;

            camTf.localPosition = Vector3.zero;

            var add = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) add += camTf.forward;
            if (Input.GetKey(KeyCode.A)) add -= camTf.right;
            if (Input.GetKey(KeyCode.S)) add -= camTf.forward;
            if (Input.GetKey(KeyCode.D)) add += camTf.right;

            if (!simulateFlying)
                add.y = 0;

            if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
            if (Input.GetKey(KeyCode.E)) add += Vector3.up;

            add.Normalize();

            var mainCameraVelocity = (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f) * speed * add;

            operatorTf.localPosition += mainCameraVelocity * Time.deltaTime;

            operatorTf.localRotation = LerpUtils.LerpBySpeed(operatorTf.localRotation, Quaternion.identity, 160, unscaledTime: true);

            if (!Application.isPlaying || _disableRotation) 
                return;

            if (rotateWithoutRmb || Input.GetMouseButton(1))
            {
                var eul = camTf.localEulerAngles;

                var rotationX = eul.y;
                float _rotationY = eul.x;

                rotationX += Input.GetAxis("Mouse X") * sensitivity;
                _rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                _rotationY = _rotationY < 120 ? Mathf.Min(_rotationY, 85) : Mathf.Max(_rotationY, 270);

                camTf.localEulerAngles = new Vector3(_rotationY, rotationX, 0);
            }

            SpinAround();
            
        }

        public void Update()
        {
            if (!_mainCam || (_onlyInEditor && !Application.isEditor))
                return;

            OnUpdateInternal();
        }

        #region Inspector

        

        public override void Inspect()
        {
         
            pegi.Nl();

            if (MainCam)
                "Main Camera".PegiLabel(width: 90).Edit(ref _mainCam).Nl();
            
            if (!_mainCam)
            {
                "Main Camera".PegiLabel().SelectInScene(ref _mainCam).Nl();
                "Camera is missing, spin around will not work".PegiLabel().WriteWarning();
            } else 
            {
                if (_mainCam.transform == transform) 
                {
                    "Camera should be a Child Object of the Camera Operator".PegiLabel().WriteWarning().Nl();
                }
            }

            pegi.Nl();

            "Speed:".PegiLabel("Speed of movement", 50).Edit(ref speed).Nl();

            "Sensitivity:".PegiLabel("How fast camera will rotate", 50).Edit(ref sensitivity).Nl();

            "Flying".PegiLabel("Looking up/down will make camera move up/down.").ToggleIcon(ref simulateFlying).Nl();

            "Disable Rotation".PegiLabel().ToggleIcon(ref _disableRotation).Nl();

            if (!_disableRotation)
                "Rotate without RMB".PegiLabel().ToggleIcon(ref rotateWithoutRmb).Nl();
            

            pegi.Nl();

            "Editor Only".PegiLabel().ToggleIcon(ref _onlyInEditor).Nl();

         

        }

        #endregion

        protected override void OnRegisterServiceInterfaces()
        {
            base.OnRegisterServiceInterfaces();
            RegisterServiceAs<Singleton_CameraOperatorGodMode>();
        }
    }

[PEGI_Inspector_Override(typeof(Singleton_CameraOperatorGodMode))] internal class CameraOperatorGodModeDrawer : PEGI_Inspector_Override { }

}
