using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class ChangeRoom : MonoBehaviour
    {
        public Renderer Mat;
        public void AssignRoom(Material[] Rooms)
        {
            if (Mat == null || Rooms == null || Rooms.Length == 0)
            {
                return;
            }

            int texture = Random.Range(0, Rooms.Length);
            Mat.material = Rooms[texture];
        }
    }
}
