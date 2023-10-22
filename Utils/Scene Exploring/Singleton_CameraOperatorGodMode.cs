using UnityEngine;
using QuizCanners.Inspect;
using QuizCanners.Lerp;
namespace QuizCanners.Utils
{

#pragma warning disable IDE0018 // Inline variable declaration

    /*
    public interface IGodModeCameraController
    {
        Vector3 GetTargetPosition();
        Vector3 GetCameraOffsetPosition();
        bool TryGetCameraHeight(out float height);
    }*/

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

        public virtual Quaternion Rotation
        {
            get => _mainCam.transform.rotation;
            set => _mainCam.transform.rotation = value;
        }

        public virtual Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        private bool mouseOutside = false;

        public float FOV 
        {
            get => _mainCam.fieldOfView;
            set => _mainCam.fieldOfView = value;
        }

        public override string InspectedCategory => Utils.Singleton.Categories.SCENE_MGMT;

        private static bool LEGACY_INPUT =>
#if ENABLE_LEGACY_INPUT_MANAGER
            true;
#else
    false;
#endif

        public Camera MainCam
        {
            get
            {
                if (_mainCam)
                    return _mainCam;
                _mainCam = Camera.main;
                return _mainCam;
            }
        }

        private void SpinAround()
        {
            var camTr = _mainCam.transform;

            bool downMMB = false;
            bool upMMB = false;
            bool pressedMMB = false;

            if (LEGACY_INPUT)
            {
                if (!mouseOutside)
                {
                    downMMB = Input.GetMouseButtonDown(2);
                }

                upMMB = Input.GetMouseButtonUp(2);
                pressedMMB = Input.GetMouseButton(2);
            }

            if (downMMB)
            {
                var ray = MainCam.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    spinCenter = hit.point;
                else 
                    return;

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

            if (upMMB)
                _orbitDistance = -1;

            if (_orbitDistance <= 0 || (!pressedMMB))
                return;

            if (LEGACY_INPUT)
            {
                if (!downMMB)
                {
                    camOrbit.x += Input.GetAxis("Mouse X") * 5;
                    camOrbit.y -= Input.GetAxis("Mouse Y") * 5;
                }
            }

            if (camOrbit.y <= -360)
                camOrbit.y += 360;
            if (camOrbit.y >= 360)
                camOrbit.y -= 360;

            var rot2 = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
            var campos = rot2 *
                             (new Vector3(0.0f, 0.0f, -_orbitDistance)) +
                             spinCenter;

            //if (!mouseOutside)

            transform.position = campos;

            if (!orbitingFocused)
            {
                camTr.localRotation = QcLerp.LerpBySpeed(camTr.localRotation, rot2, 200, unscaledTime: true);
                if (Quaternion.Angle(camTr.localRotation, rot2) < 1)
                    orbitingFocused = true;
            }
            else camTr.localRotation = rot2;

        }


        bool MouseOutsideOfView
        {
            get 
            {
                if (!Application.isEditor)
                    mouseOutside = false;
                else
                {
                    mouseOutside = _mainCam.IsMouseOutsideViewArea(Input.mousePosition);
                }

                return mouseOutside;
            }
        }

        public Vector3 GetNormalizedInput(out bool speedUp) 
        {
            var camTf = _mainCam.transform;
            speedUp = false;
            var add = Vector3.zero;
            if (LEGACY_INPUT)
            {
                mouseOutside = _mainCam.IsMouseOutsideViewArea(Input.mousePosition);

                if (Input.GetKey(KeyCode.W)) add += camTf.forward;
                if (Input.GetKey(KeyCode.A)) add -= camTf.right;
                if (Input.GetKey(KeyCode.S)) add -= camTf.forward;
                if (Input.GetKey(KeyCode.D)) add += camTf.right;
            }

            if (!simulateFlying)
                add.y = 0;

            if (LEGACY_INPUT)
            {
                if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
                if (Input.GetKey(KeyCode.E)) add += Vector3.up;
                speedUp = Input.GetKey(KeyCode.LeftShift);
            }

            add.Normalize();

            return add;
        }

        protected virtual void OnUpdateInternal()
        {
            var operatorTf = transform;

            _mainCam.transform.localPosition = Vector3.zero;

            bool rightMouseButon = false;

            var add = GetNormalizedInput(out var SpeedUp);

            var mainCameraVelocity = (SpeedUp ? 3f : 1f) * speed * add;

            operatorTf.localPosition += mainCameraVelocity * Mathf.Min(Time.unscaledDeltaTime, 0.016f);

            operatorTf.localRotation = QcLerp.LerpBySpeed(operatorTf.localRotation, Quaternion.identity, 160, unscaledTime: true);

            if (!Application.isPlaying || _disableRotation)
                return;

            if (LEGACY_INPUT)
            {
                rightMouseButon = Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1); // Ignore delta during first frame
            }


            if (rotateWithoutRmb || rightMouseButon)
            {
                RatateWithMouse();
            }

            SpinAround();

        }

        public void RatateWithMouse() 
        {
            if (MouseOutsideOfView)
                return;

            var camTf = _mainCam.transform;

            var eul = camTf.localEulerAngles;

            var rotationX = eul.y;
            var rotationY = eul.x;

            var rotationSpeed = sensitivity * FOV / 90f;

            if (LEGACY_INPUT)
            {
                rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
                rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            }

            rotationY = rotationY < 120 ? Mathf.Min(rotationY, 85) : Mathf.Max(rotationY, 270);

            camTf.localEulerAngles = new Vector3(rotationY, rotationX, 0);
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
            }
            else
            {
                if (_mainCam.transform == transform)
                {
                    "Camera should be a Child Object of the Camera Operator".PegiLabel().WriteWarning().Nl();
                } else if (!_mainCam.transform.IsChildOf(transform)) 
                {
                    "Camera should be a child of this transform".PegiLabel().WriteWarning().Nl();
                    if ("Move Camera".PegiLabel().Click().Nl())
                    {
                        _mainCam.transform.parent = transform;
                    }
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
