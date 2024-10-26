using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace QuizCanners
{

    public class C_GridInstancer : MonoBehaviour, IPEGI
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private List<GameObject> _instances = new List<GameObject>();
        [SerializeField] private float _gap = 1;
        [SerializeField] private int x_Max, y_Max, z_Max;

        private int InstanceCount 
        {
            get => _instances.Count;
            set 
            {
                while (_instances.Count < value) 
                {
                    if (Application.isPlaying)
                        _instances.Add(Instantiate(_prefab, transform));
#if UNITY_EDITOR
                    else
                        _instances.Add(PrefabUtility.InstantiatePrefab(_prefab, transform) as GameObject);
#endif
                }

                while (_instances.Count > value) 
                {
                    _instances.RemoveLast().DestroyWhatever();
                }
            }
        }

        private void Reposition() 
        {

            int index = 0;

            for (int x=0; x<x_Max; x++)
                for (int y = 0; y < y_Max; y++)
                    for (int z = 0; z < z_Max; z++)
                    {
                        var vec = new Vector3(x, y, z);

                        var tf = _instances[index].transform;

                        tf.localPosition = (vec + Random.insideUnitSphere*0.5f) * _gap;

                        tf.rotation = Quaternion.Euler(Random.insideUnitSphere);

                        tf.localScale = Vector3.one * (0.5f + Random.value);

                        index++;
                    }
        }
        public int Count => x_Max * y_Max * z_Max;

        void IPEGI.Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            if (!_prefab || InstanceCount == 0)
            {
                "Prefab".PegiLabel().Edit(ref _prefab).Nl();
            }

            if (InstanceCount > 0)
            {
                "Total Count: {0}".F(Count).PegiLabel(pegi.Styles.BaldText).Write();

                if (Icon.Delete.Click().IgnoreChanges()) 
                {
                    foreach (var i in _instances)
                        i.DestroyWhatever();

                    _instances.Clear();
                }


                pegi.Nl();
            }

            "X".PegiLabel(20).Edit(ref x_Max, 1, 100).Nl();
            "Y".PegiLabel(20).Edit(ref y_Max, 1, 100).Nl();
            "Z".PegiLabel(20).Edit(ref z_Max, 1, 100).Nl();
            "Gap".ConstLabel().Edit(ref _gap, 0.1f, 3f).Nl();

            if (changed && _prefab) 
            {
                InstanceCount = Count;
                Reposition();
            }
        }

     
    }

    [PEGI_Inspector_Override(typeof(C_GridInstancer))] internal class C_GridInstancerDrawer : PEGI_Inspector_Override { }
}
