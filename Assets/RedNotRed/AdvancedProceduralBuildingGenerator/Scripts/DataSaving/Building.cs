using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Build
{
    [CreateAssetMenu(fileName = "BuildingScriptableObject", menuName = "ConstructorScriptableObjects")]
    public class Building : ScriptableObject
    {
        public bool READY;
        public List<Builder.GenerateBuilding.Floor> floors;
        public string Name;
        public bool Interior;
        public float Quality;
        public Builder.GenerateBuilding.lod CreateLOD;
        public GameObject Window;
        public bool AutoSize;
        public GameObject RoofBordure;
        public float RoofBordureHeight;
        public float RoofBordureWidth;
        public GameObject Column;
        public GameObject BordureBetweenFloors;
        public Vector2 BordureBetweenFloorsSize;
        public GameObject Balcony;
        public int BalconyPerWindow;
        public GameObject Door;
        public GameObject Roof;
        public GameObject[] RoofMods;
        public Material BuildingMaterial;
        public Material[] Rooms;
        public Material WallMaterial;
        public Material FloorMaterial;
        public Material CeilingMaterial;
        public float MinimalScale;
        public float MaximalHeight;
        public GameObject InteriorDoor;
        public GameObject CeilingLights;
        public GameObject StairWay;
        public bool StairWayWalls;
        public List<Builder.GenerateBuilding.Furniture> Furnitures;
        public bool PlaceFurniture;
    }
}