using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class Balcony : MonoBehaviour
    {
        [Header("Chair")]
        public Transform ChairPosition;
        public GameObject[] Chairs;
        [Header("Table")]
        public Transform TablePosition;
        public GameObject[] Tables;
        public void GenerateDetails()
        {
            //Chair
            if (ChairPosition != null && Chairs != null && Chairs.Length > 0 && Random.Range(0, 2) == 1)
            {
                int chairnum = Random.Range(0, Chairs.Length);
                Instantiate(Chairs[chairnum], ChairPosition.position, ChairPosition.rotation, transform);
            }
            //Table
            if (TablePosition != null && Tables != null && Tables.Length > 0 && Random.Range(0, 2) == 1)
            {
                int tablenum = Random.Range(0, Tables.Length);
                GameObject table = Instantiate(Tables[tablenum], TablePosition.position, TablePosition.rotation, transform);
                Builder.Table tableDetails = table.GetComponent<Builder.Table>();
                if (tableDetails != null)
                {
                    tableDetails.GenerateDetails();
                }
            }
        }
    }
}
