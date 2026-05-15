using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class CameraScript : MonoBehaviour
    {
        public bool Restart;
        public float Speed;
        public Transform[] CheckPoints;
        private Vector3 pos;
        private Quaternion rot;
        private Vector3 InitialPos;
        private Quaternion InitialRot;
        private void Start()
        {
            InitialPos = transform.position;
            InitialRot = transform.rotation;
            StartCoroutine(Go());
        }
        public IEnumerator Go()
        {
            transform.position = InitialPos;
            transform.rotation = InitialRot;
            Restart = false;
            foreach (Transform t in CheckPoints)
            {
                pos = t.position;
                rot = t.rotation;
                yield return new WaitForSeconds(1f / (Speed * 10f));
            }
        }
        private void Update()
        {
            if (Restart)
            {
                StopAllCoroutines();
                StartCoroutine(Go());
            }
            transform.position = Vector3.Lerp(transform.position, pos, Speed);
            transform.rotation = Quaternion.Lerp(transform.rotation, rot, Speed);
        }
    }
}
