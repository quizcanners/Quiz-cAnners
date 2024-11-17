using UnityEngine;
using QuizCanners.Inspect;
using QuizCanners.Lerp;
using static UnityEditor.PlayerSettings;

namespace QuizCanners.Utils
{

    [ExecuteInEditMode]
    public class Singleton_CameraOperatorGodMode : Singleton.BehaniourBase, IPEGI
    {
        public float speed = 20;
        public float offsetClip01 = 0;
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
        private bool _spinStarted;


        [SerializeField] protected Camera _mainCam;

        public virtual Quaternion Rotation
        {
            get => _mainCam.transform.rotation;
            set => _mainCam.transform.rotation = value;
        }

        public virtual Vector3 Position
        {
            get => transform.position;
            set
            {
                transform.position = value;
            }
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

        public class PointedPositionState
        {
            public Gate.Frame _pointedPositionUpdateGate = new(Gate.InitialValue.StartArmed);
            Vector3 _pointedPosition_Cached;
            bool _isPositionPointed_Cached;

            public bool TryGetPointedPosition(Camera cam, out Vector3 pos)
            {
                if (_pointedPositionUpdateGate.TryEnter())
                {
                    _isPositionPointed_Cached = Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit);
                    _pointedPosition_Cached = hit.point;
                }

                pos = _pointedPosition_Cached;
                return _isPositionPointed_Cached;
            }
        }

        readonly PointedPositionState _pointedPosition = new();

        public bool TryGetPointedPosition(out Vector3 pos)
         => _pointedPosition.TryGetPointedPosition(Camera.main, out pos);

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
                if (!TryGetPointedPosition(out var pos))
                {
                    _spinStarted = false;
                    return;
                }

                StartSpin(pos);
            }

            if (upMMB || !pressedMMB)
                _spinStarted = false;

            if (!_spinStarted)
                return;

            if (LEGACY_INPUT && !downMMB)
            {
                camOrbit.x += Input.GetAxis("Mouse X") * 5;
                camOrbit.y -= Input.GetAxis("Mouse Y") * 5;
            }

            if (camOrbit.y <= -360)
                camOrbit.y += 360;
            if (camOrbit.y >= 360)
                camOrbit.y -= 360;

            var rot2 = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
            var campos = rot2 * new Vector3(0.0f, 0.0f, -_orbitDistance) + spinCenter;

            transform.position = campos;

            if (!orbitingFocused)
            {
                camTr.localRotation = QcLerp.LerpBySpeed(camTr.localRotation, rot2, 200, unscaledTime: true);
                if (Quaternion.Angle(camTr.localRotation, rot2) < 1)
                    orbitingFocused = true;
            }
            else 
                camTr.localRotation = rot2;

            return;

            void StartSpin(Vector3 pos)
            {
                spinCenter = pos;

                var before = camTr.localRotation;
                camTr.LookAt(spinCenter);
                var rot = camTr.localRotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                _orbitDistance = (spinCenter - transform.position).magnitude;

                camTr.rotation = before;
                orbitingFocused = false;
                spinStartTime = Time.time;
                _spinStarted = true;
            }

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

        public bool TryGetRelativeDirectionFromInput(float forward, float right, out Vector3 input)
        {
            if (forward == 0 && right == 0)
            {
                input = Vector3.zero;
                return false;
            }
            var mainCamTf = _mainCam.transform;
            var forwardDir = mainCamTf.forward;
            var rightDir = mainCamTf.right;
            input = (forwardDir * forward + rightDir * right).normalized;
            return true;        
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

            if (TryDrag())
                return;

            if (rotateWithoutRmb || rightMouseButon)
            {
                RatateWithMouse();
            }

            SpinAround();
           
            UpdateScroll();

            return;

            void UpdateScroll()
            {
                float ScrollWheelChange = Input.GetAxisRaw("Mouse ScrollWheel");

                if (!mouseOutside && ScrollWheelChange != 0 && TryGetPointedPosition(out var pos))
                {
                    var delta = (pos - transform.position);

                    if (ScrollWheelChange > 0)
                        transform.position += 0.5f * Mathf.Clamp01(ScrollWheelChange * 5f) * delta;
                    else
                        transform.position -= 0.5f * Mathf.Clamp01(-ScrollWheelChange * 5f) * delta;
                }
            }
        }

        const int MAX_ROTATION = 85;

        public void Rotate(Vector2 input)
        {
            var camTf = _mainCam.transform;

            var eul = camTf.localEulerAngles;

            var rotationCoefficient = FOV / 90f;

            input *= rotationCoefficient;

            if (input.magnitude > MAX_ROTATION)
                input = input.normalized * MAX_ROTATION;

            var rotationX = eul.y + input.x;
            var rotationY = eul.x - input.y;
            
            rotationY = rotationY < 120 ? Mathf.Min(rotationY, 85) : Mathf.Max(rotationY, 270);

            camTf.localEulerAngles = new Vector3(rotationY, rotationX, eul.z);
        }

        public void RotateRelative(Vector2 input, Transform platform)
        {
            var camTf = _mainCam.transform;

            var platformSpaceRotation = Quaternion.Inverse(platform.rotation) * camTf.rotation;
            var platformSpaceEul = platformSpaceRotation.eulerAngles;
            var rotationCoefficient = FOV / 90f;

            input *= rotationCoefficient;

            if (input.magnitude > MAX_ROTATION)
                input = input.normalized * MAX_ROTATION;

            var rotationX = platformSpaceEul.y + input.x;
            var rotationY = platformSpaceEul.x - input.y;

            rotationY = rotationY < 120 ? Mathf.Min(rotationY, 85) : Mathf.Max(rotationY, 270);
            platformSpaceEul = new Vector3(rotationY, rotationX, platformSpaceEul.z);
            camTf.rotation = platform.rotation * Quaternion.Euler(platformSpaceEul);
        }

        public void RatateWithMouse() 
        {
            if (MouseOutsideOfView)
                return;

            Rotate(GetInput() * sensitivity);
        }

        Vector2 GetInput() => (LEGACY_INPUT)
            ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"))
            : Vector2.zero;

        public void RatateWithMouse(Transform platform)
        {
            if (MouseOutsideOfView)
                return;

            RotateRelative(GetInput() * sensitivity, platform);
        }

        public void Update()
        {
            if (!_mainCam || (_onlyInEditor && !Application.isEditor))
                return;

            mouseOutside = _mainCam.IsMouseOutsideViewArea(Input.mousePosition);

            OnUpdateInternal();
        }

        [Header("Drag")]
        [SerializeField] private float _dragCoefficient = 0.1f;

        private class DragState
        {
            public Vector3 DragCenterLonLat;
            public Vector2 Delta;
            public Vector3 DeltaFromStartCenter;
            public bool Started;

            public Gate.Bool IsPressed = new();

            public bool TryStart(Camera cam)
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);

                Started = false;

                if (!Physics.Raycast(ray, out RaycastHit hit))
                    return false;

                DragCenterLonLat = hit.point;
                DeltaFromStartCenter = cam.transform.position - hit.point;
                Delta = Vector2.zero;
                Started = true;

                return true;
            }

        }


        private readonly DragState Drag = new();

        public bool TryDrag()
        {

            bool pressedMB = Input.GetMouseButton(0) && Input.GetMouseButton(1);
            mouseOutside = MouseOutsideOfView;

            if (!mouseOutside && pressedMB && Drag.IsPressed.TryChange(true))
            {
                return Drag.TryStart(MainCam);
            }

            if (!Drag.Started)
                return false;

            if (!pressedMB)
            {
                Drag.IsPressed.TryChange(false);
                return false;
            }

            Drag.Delta += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            float dragCoefficient = Drag.DeltaFromStartCenter.magnitude * _dragCoefficient * Mathf.Tan(Mathf.Deg2Rad * MainCam.fieldOfView);

            transform.position = Drag.DragCenterLonLat
                - Drag.Delta.x * dragCoefficient * MainCam.transform.right.Y(0).normalized
                - Drag.Delta.y * dragCoefficient * MainCam.transform.forward.Y(0).normalized
                + Drag.DeltaFromStartCenter;

            return true;
        }

        #region Inspector

        public override void Inspect()
        {

            pegi.Nl();

            if (MainCam)
                "Main Camera".ConstLabel().Edit(ref _mainCam).Nl();

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
