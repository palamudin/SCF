using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class Table : MonoBehaviour
    {
        public GameObject[] Things;
        public Vector2 Radius;
        public void GenerateDetails()
        {
            for (int i = 0; i < Things.Length; i++)
            {
                if (Random.Range(0, 2) == 1)
                {
                    Vector3 pos = new Vector3(transform.position.x + Random.Range(-Radius.x, Radius.x), transform.position.y + transform.lossyScale.y / 2, transform.position.z + Random.Range(-Radius.y, Radius.y));
                    float rot = Random.Range(0, 360);
                    Instantiate(Things[i], pos, Quaternion.Euler(0, rot, 0), transform);
                }
            }
        }
    }
}
