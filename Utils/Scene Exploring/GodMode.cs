using UnityEngine;
using QuizCanners.Inspect;
using QuizCanners.Lerp;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0018 // Inline variable declaration

    [ExecuteInEditMode]
    public class GodMode : Singleton.BehaniourBase, IPEGI
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
                camTr.localRotation = LerpUtils.LerpBySpeed(camTr.localRotation, rot2, 200);
                if (Quaternion.Angle(camTr.localRotation, rot2) < 1)
                    orbitingFocused = true;
            }
            else camTr.localRotation = rot2;
            
        }

        protected virtual void OnUpdateInternal() 
        {
            var operatorTf = transform;
            var camTf = _mainCam.transform;

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

            operatorTf.localRotation = LerpUtils.LerpBySpeed(operatorTf.localRotation, Quaternion.identity, 160);

            if (!Application.isPlaying || _disableRotation) return;

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
         
            pegi.nl();

            if (MainCam)
                "Main Camera".PegiLabel(width: 90).edit(ref _mainCam).nl();
            
            if (!_mainCam)
            {
                "Main Camera".PegiLabel().selectInScene(ref _mainCam).nl();
                "Camera is missing, spin around will not work".PegiLabel().writeWarning();
            }

            pegi.nl();

            "Speed:".PegiLabel("Speed of movement", 50).edit(ref speed).nl();

            "Sensitivity:".PegiLabel("How fast camera will rotate", 50).edit(ref sensitivity).nl();

            "Flying".PegiLabel("Looking up/down will make camera move up/down.").toggleIcon(ref simulateFlying).nl();

            "Disable Rotation".PegiLabel().toggleIcon(ref _disableRotation).nl();

            if (!_disableRotation)
                "Rotate without RMB".PegiLabel().toggleIcon(ref rotateWithoutRmb).nl();
            

            pegi.nl();

            "Editor Only".PegiLabel().toggleIcon(ref _onlyInEditor).nl();
        }

        #endregion

        protected override void OnRegisterServiceInterfaces()
        {
            base.OnRegisterServiceInterfaces();
            RegisterServiceAs<GodMode>();
        }
    }

[PEGI_Inspector_Override(typeof(GodMode))] internal class GodModeDrawer : PEGI_Inspector_Override { }

}
