using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine;
using Parabox.CSG;
using static Builder.GenerateBuilding;

namespace Builder
{

    [HelpURL("https://irinamavromatis85.wixsite.com/buildinggenerator")]
    public class GenerateBuilding : MonoBehaviour
    {
        [Header("Building")]
        [HideInInspector] public string Name;
        private int FloorNumber;
        private string[] Names = new string[] { "Building", "House", "Home", "Construction" };
        [HideInInspector]
        public static Transform Constructor;
        [Tooltip("Gets or assigns info from a BuildingScriptableObject. Allows saving buildings. Assigns if the object is checked as not ready and gets if it's ready.")]
        [HideInInspector] public Build.Building Info;
        [Header("Interior")]
        [HideInInspector] public bool Interior;
        [HideInInspector] public float MinimalScale;
        [HideInInspector] public float MaximalHeight;
        [HideInInspector] public Material FloorMaterial;
        [HideInInspector] public Material WallMaterial;
        [HideInInspector] public Material CeilingMaterial;
        [HideInInspector] public GameObject InteriorDoor;
        [HideInInspector] public bool PlaceCeilingLights;
        [HideInInspector] public GameObject CeilingLights;
        [HideInInspector] public bool PlaceFurniture;
        [HideInInspector] public GameObject StairWay;
        [HideInInspector] public bool StairWayWalls;
        [HideInInspector] public GameObject Elevator;
        public enum FurniturePlacementType { OnWall, NearWall, AwayFromWall }
        public enum RoomType { Salon, Kitchen, Bedroom }
        [Serializable]
        public class Furniture
        {
            public GameObject Object;
            public RoomType RoomToBePlacedIn;
            public int MaxNumberPerRoom;
            public FurniturePlacementType PlacementType;
        }
        public List<Furniture> Furnitures = new List<Furniture>();
        [Header("Optimization")]
        [HideInInspector] public float Quality;
        [Tooltip("LODGroups are going to be added to your building to optimize it, however, the time it will take you to select it will be much higher!")]
        public enum lod { None, Partial, Full }
        [HideInInspector] public lod CreateLOD;
        public enum DoorDirection { Nord, South, East, West }
        [Serializable]
        public class DoorModel
        {
            public GameObject Model;
            public Vector3 Position;
            public DoorDirection Direction;
        }
        [Serializable]
        public class WallMod
        {
            public GameObject Model;
            public Vector2 Position;
            public DoorDirection Direction;
            public bool Obstacle;
        }
        [Serializable]
        public class Extenction
        {
            [HideInInspector]
            public Transform Model;
            public Vector3 Position;
            public float width;
            public int height;
            public float lenght;
            public int WindowsX;
            public int WindowsY;
            public int WindowsZ;
            public float WindowsOffset;
            public float WindowWidth;
            public float WindowHeight;
            public float WindowLenght;
            public bool NordWindow;
            public bool SouthWindow;
            public bool EastWindow;
            public bool WestWindow;
            public bool NordColumn;
            public bool SouthColumn;
            public bool EastColumn;
            public bool WestColumn;
            public bool Deform;
            public List<DoorModel> Doors = new List<DoorModel>();
            public List<WallMod> WallMods = new List<WallMod>();
        }
        [Serializable]
        public class Floor
        {
            public int width;
            public int height;
            public int lenght;
            public int WindowsX;
            public int WindowsY;
            public int WindowsZ;
            public float WindowsOffset;
            public float WindowWidth;
            public float WindowHeight;
            public float WindowLenght;
            public bool NordWindow;
            public bool SouthWindow;
            public bool EastWindow;
            public bool WestWindow;
            public List<Material> WindowMaterials;
            public bool NordColumn;
            public bool SouthColumn;
            public bool EastColumn;
            public bool WestColumn;
            public bool Deform;
            public List<DoorModel> Doors = new List<DoorModel>();
            public List<WallMod> WallMods = new List<WallMod>();
            public List<Extenction> Extenctions = new List<Extenction>();
        }
        [Header("Floors")]
        [Tooltip("Floors of the building")]
        public List<Floor> Floors;
        [Serializable]
        public class FloorModel
        {
            public Transform Model;
            public int width;
            public int height;
            public int lenght;
            public int WindowsX;
            public int WindowsY;
            public int WindowsZ;
            public float WindowsOffset;
            public float WindowWidth;
            public float WindowHeight;
            public float WindowLenght;
            public bool NordWindow;
            public bool SouthWindow;
            public bool EastWindow;
            public bool WestWindow;
            public List<Material> WindowMaterials;
            public bool NordColumn;
            public bool SouthColumn;
            public bool EastColumn;
            public bool WestColumn;
            public bool Deform;
            public List<DoorModel> Doors = new List<DoorModel>();
            public List<WallMod> WallMods = new List<WallMod>();
            public List<Extenction> Extenctions = new List<Extenction>();
        }
        [Header("Windows")]
        [HideInInspector] public GameObject WindowModel;
        [Tooltip("When on, this will auto size windows basing on its number per floor.")]
        [HideInInspector] public bool AutoSize;
        [Tooltip("Rooms that you can see in the window or door. It requires the Change Room component!!!")]
        [HideInInspector]
        public List<Transform> Windows = new List<Transform>();
        [Header("Bordure")]
        [HideInInspector] public GameObject RoofBordure;
        [HideInInspector] public float BordureHeight;
        [HideInInspector] public float BordureWidth;
        [Header("Column")]
        [HideInInspector] public GameObject Column;
        [HideInInspector] public GameObject CentralColumn;
        [Header("Bordure Between Floors")]
        [HideInInspector] public GameObject BordureBetweenFloors;
        [HideInInspector] public Vector2 FloorBordureScale;
        [Header("Balcony")]
        [HideInInspector] public GameObject Balcony;
        private int WindowsPerBalcony;
        [HideInInspector] public GameObject Door;
        [Header("Roof")]
        [HideInInspector] public GameObject Roof;
        [HideInInspector] public GameObject[] RoofMods;
        private float TimeToGenerate = 0.1f;
        [Header("Materials")]
        [HideInInspector] public Material BuildingMaterial;
        [HideInInspector] public Material[] Rooms;
        [Header("Random Generation")]
        [HideInInspector] public float RandomWindowsOffset;
        [HideInInspector] public GameObject DoorGen;
        [Header("Info")]
        [Tooltip("Can be null, it will be then assigned to a new created building or you can assign it by yourself. It allows you to simplify the selected building.")]
        [HideInInspector] public Transform Building;
        [HideInInspector] public Material TransparentMaterial;
        [Serializable]
        public class RoofSection
        {
            public GameObject Obj;
            public float Radius;
            public Vector3 Position;
            public float Rotation;
            public int Num;
        }
        private Dictionary<Transform, Transform> InteriorsDictionnary = new Dictionary<Transform, Transform>();
        [Serializable]
        public class fl
        {
            public Vector3 Scale;
            public Vector3 Position;
            public Transform Model;
            public bool Deform;
        }
        private List<fl> fls = new List<fl>();
        private Dictionary<Transform, Color> GizmosDictionary = new Dictionary<Transform, Color>();
        public enum VisualType { Normal, Rooms, Doors, Furniture }
        [HideInInspector] public VisualType Visualization;
        private List<float> FloorLevels = new List<float>();
        private bool FloorLevelsDone = false;
        [Serializable]
        public class RoomWall
        {
            public Transform Wall;
            public Vector3 Position;
            public Vector3 Scale;
            public Quaternion Rotation;
        }
        private List<RoomWall> RoomWalls = new List<RoomWall>();
        private Dictionary<Transform, RoomType> RoomsType = new Dictionary<Transform, RoomType>();
        private int NumberOfSalons = 0;
        private int NumberOfKitchens = 0;
        private int NumberOfBedrooms = 0;
        [HideInInspector] public GameObject ObjectToDeform;
        [HideInInspector] public GameObject DeformingShape;
        public enum DeformingType { Subtract, Unify, Intersection }
        [HideInInspector] public DeformingType DeformType;
        private Vector3 FirstFloorPosition;
        private bool FirstFloorPositionAssigned;
#if UNITY_EDITOR
        [MenuItem("Window/Constructor/Create Module")]
        static void CreateModule()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = "Constructor";
            DestroyImmediate(obj.GetComponent<Renderer>());
            DestroyImmediate(obj.GetComponent<MeshFilter>());
            DestroyImmediate(obj.GetComponent<Collider>());
            obj.AddComponent<GenerateBuilding>();
        }
        [CustomEditor(typeof(GenerateBuilding))]
        public class BuildingGeneratorUI : Editor
        {
            float inter = 0;
            float balc = 0;
            float bordfl = 0;
            float bord = 0;
            float rof = 0;
            float cl = 0;
            float stw = 0;
            float ele = 0;
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var InitialClass = target as GenerateBuilding;
                InitialClass.Fuse(); //VERY IMPORTANT!!!
                InitialClass.MakeSingleton();
                InitialClass.UseInfo();
                //Name
                SerializedProperty name = serializedObject.FindProperty("Name");
                EditorGUILayout.PropertyField(name);
                if (InitialClass.Name == "")
                {
                    InitialClass.Name = InitialClass.Names[UnityEngine.Random.Range(0, InitialClass.Names.Length)];
                }
                //Info
                SerializedProperty info = serializedObject.FindProperty("Info");
                EditorGUILayout.PropertyField(info);
                //Interior
                if (InitialClass.Interior == true)
                {
                    if (inter < 0.99f)
                    {
                        inter = Mathf.Lerp(inter, 1f, 0.1f);
                    }
                    else
                    {
                        inter = 1;
                    }
                }
                else
                {
                    if (inter > 0.01f)
                    {
                        inter = Mathf.Lerp(inter, 0f, 0.1f);
                    }
                    else
                    {
                        inter = 0;
                    }
                }
                if (InitialClass.PlaceCeilingLights == true)
                {
                    if (cl < 0.99f)
                    {
                        cl = Mathf.Lerp(cl, 1f, 0.1f);
                    }
                    else
                    {
                        cl = 1;
                    }
                }
                else
                {
                    if (cl > 0.01f)
                    {
                        cl = Mathf.Lerp(cl, 0f, 0.1f);
                    }
                    else
                    {
                        cl = 0;
                    }
                }
                float numberoffloors = InitialClass.CalculateNumberOfFloors();
                if (numberoffloors > 0)
                {
                    if (stw < 0.99f)
                    {
                        stw = Mathf.Lerp(stw, 1f, 0.1f);
                    }
                    else
                    {
                        stw = 1;
                    }
                }
                else
                {
                    if (stw > 0.01f)
                    {
                        stw = Mathf.Lerp(stw, 0f, 0.1f);
                    }
                    else
                    {
                        stw = 0;
                    }
                }
                if (numberoffloors > 5)
                {
                    if (ele < 0.99f)
                    {
                        ele = Mathf.Lerp(ele, 1f, 0.1f);
                    }
                    else
                    {
                        ele = 1;
                    }
                }
                else
                {
                    if (ele > 0.01f)
                    {
                        ele = Mathf.Lerp(ele, 0f, 0.1f);
                    }
                    else
                    {
                        ele = 0;
                    }
                }
                SerializedProperty interior = serializedObject.FindProperty("Interior");
                EditorGUILayout.PropertyField(interior);
                if (EditorGUILayout.BeginFadeGroup(inter))
                {
                    EditorGUI.indentLevel++;
                    InitialClass.MinimalScale = EditorGUILayout.FloatField("Minimal Scale", InitialClass.MinimalScale);
                    if (InitialClass.MinimalScale < 1f)
                    {
                        InitialClass.MinimalScale = 1f;
                    }
                    InitialClass.MaximalHeight = EditorGUILayout.FloatField("Maximal Height", InitialClass.MaximalHeight);
                    if (InitialClass.MaximalHeight < 1f)
                    {
                        InitialClass.MaximalHeight = 1f;
                    }
                    SerializedProperty wallmat = serializedObject.FindProperty("WallMaterial");
                    EditorGUILayout.PropertyField(wallmat);
                    SerializedProperty floormat = serializedObject.FindProperty("FloorMaterial");
                    EditorGUILayout.PropertyField(floormat);
                    SerializedProperty ceilingmat = serializedObject.FindProperty("CeilingMaterial");
                    EditorGUILayout.PropertyField(ceilingmat);
                    SerializedProperty interiordoor = serializedObject.FindProperty("InteriorDoor");
                    EditorGUILayout.PropertyField(interiordoor);
                    SerializedProperty placeinteriorlights = serializedObject.FindProperty("PlaceCeilingLights");
                    EditorGUILayout.PropertyField(placeinteriorlights);
                    if (EditorGUILayout.BeginFadeGroup(cl))
                    {
                        EditorGUI.indentLevel++;
                        SerializedProperty interiorlights = serializedObject.FindProperty("CeilingLights");
                        EditorGUILayout.PropertyField(interiorlights);
                        SerializedProperty placefurniture = serializedObject.FindProperty("PlaceFurniture");
                        EditorGUILayout.PropertyField(placefurniture);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFadeGroup();
                    if (EditorGUILayout.BeginFadeGroup(stw))
                    {
                        SerializedProperty stairway = serializedObject.FindProperty("StairWay");
                        EditorGUILayout.PropertyField(stairway);
                        SerializedProperty stairwaywalls = serializedObject.FindProperty("StairWayWalls");
                        EditorGUILayout.PropertyField(stairwaywalls);
                    }
                    EditorGUILayout.EndFadeGroup();
                    if (EditorGUILayout.BeginFadeGroup(ele))
                    {
                        SerializedProperty elevator = serializedObject.FindProperty("Elevator");
                        EditorGUILayout.PropertyField(elevator);
                    }
                    EditorGUILayout.EndFadeGroup();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                SerializedProperty showrooms = serializedObject.FindProperty("Visualization");
                EditorGUILayout.PropertyField(showrooms);
                //Optimization
                SerializedProperty lod = serializedObject.FindProperty("CreateLOD");
                EditorGUILayout.PropertyField(lod);
                SerializedProperty quality = serializedObject.FindProperty("Quality");
                EditorGUILayout.PropertyField(quality);
                if (InitialClass.Quality > 1)
                {
                    InitialClass.Quality = 1f;
                }
                if (InitialClass.Quality < 0.001f)
                {
                    InitialClass.Quality = 0.001f;
                }
                if (GUILayout.Button("Simplify"))
                {
                    InitialClass.Simplify();
                }
                //Windows
                SerializedProperty windowmodel = serializedObject.FindProperty("WindowModel");
                EditorGUILayout.PropertyField(windowmodel);
                SerializedProperty autosize = serializedObject.FindProperty("AutoSize");
                EditorGUILayout.PropertyField(autosize);
                //Roof
                if (InitialClass.RoofBordure != null && InitialClass.Roof == null)
                {
                    if (bord < 0.99f)
                    {
                        bord = Mathf.Lerp(bord, 1f, 0.1f);
                    }
                    else
                    {
                        bord = 1;
                    }
                }
                else
                {
                    if (bord > 0.01f)
                    {
                        bord = Mathf.Lerp(bord, 0f, 0.1f);
                    }
                    else
                    {
                        bord = 0;
                    }
                }
                if (InitialClass.Roof == null)
                {
                    if (rof < 0.99f)
                    {
                        rof = Mathf.Lerp(rof, 1f, 0.1f);
                    }
                    else
                    {
                        rof = 1;
                    }
                }
                else
                {
                    if (rof > 0.01f)
                    {
                        rof = Mathf.Lerp(rof, 0f, 0.1f);
                    }
                    else
                    {
                        rof = 0;
                    }
                }
                SerializedProperty roof = serializedObject.FindProperty("Roof");
                EditorGUILayout.PropertyField(roof);
                if (EditorGUILayout.BeginFadeGroup(rof))
                {
                    EditorGUI.indentLevel++;
                    SerializedProperty roofmods = serializedObject.FindProperty("RoofMods");
                    EditorGUILayout.PropertyField(roofmods);
                    SerializedProperty roofbordure = serializedObject.FindProperty("RoofBordure");
                    EditorGUILayout.PropertyField(roofbordure);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                if (EditorGUILayout.BeginFadeGroup(bord))
                {
                    EditorGUI.indentLevel += 2;
                    SerializedProperty bordureheight = serializedObject.FindProperty("BordureHeight");
                    EditorGUILayout.PropertyField(bordureheight);
                    SerializedProperty bordurewidth = serializedObject.FindProperty("BordureWidth");
                    EditorGUILayout.PropertyField(bordurewidth);
                    EditorGUI.indentLevel -= 2;
                }
                EditorGUILayout.EndFadeGroup();
                //Column
                SerializedProperty column = serializedObject.FindProperty("Column");
                EditorGUILayout.PropertyField(column);
                SerializedProperty centralcolumn = serializedObject.FindProperty("CentralColumn");
                EditorGUILayout.PropertyField(centralcolumn);
                //Bordure Between Floors
                if (InitialClass.BordureBetweenFloors != null)
                {
                    if (bordfl < 0.99f)
                    {
                        bordfl = Mathf.Lerp(bordfl, 1f, 0.1f);
                    }
                    else
                    {
                        bordfl = 1;
                    }
                }
                else
                {
                    if (bordfl > 0.01f)
                    {
                        bordfl = Mathf.Lerp(bordfl, 0f, 0.1f);
                    }
                    else
                    {
                        bordfl = 0;
                    }
                }
                SerializedProperty bordurebetweenfloors = serializedObject.FindProperty("BordureBetweenFloors");
                EditorGUILayout.PropertyField(bordurebetweenfloors);
                if (EditorGUILayout.BeginFadeGroup(bordfl))
                {
                    EditorGUI.indentLevel++;
                    InitialClass.FloorBordureScale = EditorGUILayout.Vector2Field("FloorBordureScale", InitialClass.FloorBordureScale);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                //Balcony
                if (InitialClass.Balcony != null)
                {
                    if (balc < 0.99f)
                    {
                        balc = Mathf.Lerp(balc, 1f, 0.1f);
                    }
                    else
                    {
                        balc = 1;
                    }
                }
                else
                {
                    if (balc > 0.01f)
                    {
                        balc = Mathf.Lerp(balc, 0f, 0.1f);
                    }
                    else
                    {
                        balc = 0;
                    }
                }
                SerializedProperty balcony = serializedObject.FindProperty("Balcony");
                EditorGUILayout.PropertyField(balcony);
                if (InitialClass.WindowsPerBalcony < 1)
                {
                    InitialClass.WindowsPerBalcony = 1;
                }
                if (EditorGUILayout.BeginFadeGroup(balc))
                {
                    EditorGUI.indentLevel++;
                    InitialClass.WindowsPerBalcony = EditorGUILayout.IntField("WindowsPerBalcony", InitialClass.WindowsPerBalcony);
                    SerializedProperty door = serializedObject.FindProperty("Door");
                    EditorGUILayout.PropertyField(door);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();
                //Materials
                SerializedProperty buildingmaterial = serializedObject.FindProperty("BuildingMaterial");
                EditorGUILayout.PropertyField(buildingmaterial);
                SerializedProperty rooms = serializedObject.FindProperty("Rooms");
                EditorGUILayout.PropertyField(rooms);
                //RandomGeneration
                SerializedProperty windowsoffset = serializedObject.FindProperty("RandomWindowsOffset");
                EditorGUILayout.PropertyField(windowsoffset);
                SerializedProperty doorgen = serializedObject.FindProperty("DoorGen");
                EditorGUILayout.PropertyField(doorgen);
                //Info
                SerializedProperty building = serializedObject.FindProperty("Building");
                EditorGUILayout.PropertyField(building);
                SerializedProperty transmat = serializedObject.FindProperty("TransparentMaterial");
                EditorGUILayout.PropertyField(transmat);
                if (GUILayout.Button("Generate"))
                {
                    InitialClass.StopAllCoroutines();
                    InitialClass.StartGeneratorCoroutine(InitialClass.CreateBuilding(InitialClass.transform.position));
                }
                if (GUILayout.Button("Random"))
                {
                    InitialClass.GenerateRandom();
                }
                //Tools
                EditorGUILayout.LabelField("---Tools---");
                SerializedProperty fobj = serializedObject.FindProperty("ObjectToDeform");
                EditorGUILayout.PropertyField(fobj);
                SerializedProperty sobj = serializedObject.FindProperty("DeformingShape");
                EditorGUILayout.PropertyField(sobj);
                SerializedProperty dtype = serializedObject.FindProperty("DeformType");
                EditorGUILayout.PropertyField(dtype);
                if (GUILayout.Button("Deform"))
                {
                    InitialClass.Deform();
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
        private void StartGeneratorCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorCoroutineUtility.StartCoroutine(routine, this);
                return;
            }
#endif

            StartCoroutine(routine);
        }
        void Fuse()
        {
            foreach (Floor fl in Floors)
            {
                if (fl.Deform)
                {
                    if (fl.WindowsX + fl.WindowsY + fl.WindowsZ > 15)
                    {
                        fl.Deform = false;
                    }
                }
                foreach (Extenction ex in fl.Extenctions)
                {
                    if (ex.Deform)
                    {
                        if (ex.WindowsX + ex.WindowsY + ex.WindowsZ > 15)
                        {
                            ex.Deform = false;
                        }
                    }
                }
            }
        }
        float CalculateNumberOfFloors()
        {
            float num = 0;
            bool Started = false;
            if (Interior)
            {
                foreach (var fl in Floors)
                {
                    if (fl.Deform)
                    {
                        Started = true;
                        num += Mathf.CeilToInt(fl.height / MaximalHeight);
                    }
                    else if (Started)
                    {
                        break;
                    }
                }
            }
            return num;
        }
        void MakeSingleton()
        {
            if (Constructor == null)
            {
                Constructor = transform;
            }
            else if (Constructor != transform)
            {
                DestroyImmediate(Constructor.gameObject);
                Constructor = transform;
                Debug.LogError("You can have only one constructor per scene!");
            }
        }
        void UseInfo()
        {
            if (Info != null)
            {
                if (Info.READY)
                {
                    Name = Info.name;
                    Floors = Info.floors;
                    CreateLOD = Info.CreateLOD;
                    Interior = Info.Interior;
                    Rooms = Info.Rooms;
                    Balcony = Info.Balcony;
                    WindowsPerBalcony = Info.BalconyPerWindow;
                    Column = Info.Column;
                    BordureBetweenFloors = Info.BordureBetweenFloors;
                    FloorBordureScale = Info.BordureBetweenFloorsSize;
                    RoofBordure = Info.RoofBordure;
                    BordureHeight = Info.RoofBordureHeight;
                    BordureWidth = Info.RoofBordureWidth;
                    Roof = Info.Roof;
                    RoofMods = Info.RoofMods;
                    BuildingMaterial = Info.BuildingMaterial;
                    Door = Info.Door;
                    AutoSize = Info.AutoSize;
                    WindowModel = Info.Window;
                    Quality = Info.Quality;
                    WallMaterial = Info.WallMaterial;
                    FloorMaterial = Info.FloorMaterial;
                    CeilingMaterial = Info.CeilingMaterial;
                    MinimalScale = Info.MinimalScale;
                    MaximalHeight = Info.MaximalHeight;
                    InteriorDoor = Info.InteriorDoor;
                    StairWay = Info.StairWay;
                    StairWayWalls = Info.StairWayWalls;
                    CeilingLights = Info.CeilingLights;
                    Furnitures = Info.Furnitures;
                    PlaceFurniture = Info.PlaceFurniture;
                }
                else
                {
                    Info.Name = Name;
                    Info.floors = Floors;
                    Info.CreateLOD = CreateLOD;
                    Info.Interior = Interior;
                    Info.Rooms = Rooms;
                    Info.Balcony = Balcony;
                    Info.BalconyPerWindow = WindowsPerBalcony;
                    Info.Column = Column;
                    Info.BordureBetweenFloors = BordureBetweenFloors;
                    Info.BordureBetweenFloorsSize = FloorBordureScale;
                    Info.RoofBordure = RoofBordure;
                    Info.RoofBordureHeight = BordureHeight;
                    Info.RoofBordureWidth = BordureWidth;
                    Info.Roof = Roof;
                    Info.RoofMods = RoofMods;
                    Info.BuildingMaterial = BuildingMaterial;
                    Info.Door = Door;
                    Info.AutoSize = AutoSize;
                    Info.Window = WindowModel;
                    Info.Quality = Quality;
                    Info.MaximalHeight = MaximalHeight;
                    Info.MinimalScale = MinimalScale;
                    Info.WallMaterial = WallMaterial;
                    Info.CeilingMaterial = CeilingMaterial;
                    Info.FloorMaterial = FloorMaterial;
                    Info.InteriorDoor = InteriorDoor;
                    Info.StairWay = StairWay;
                    Info.StairWayWalls = StairWayWalls;
                    Info.CeilingLights = CeilingLights;
                    Info.Furnitures = Furnitures;
                    Info.PlaceFurniture = PlaceFurniture;
                    Info.READY = true;
                }
            }
        }
#if UNITY_EDITOR
        void Simplify()
        {
            Transform[] parts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in parts)
            {
                if (t.GetComponent<MeshFilter>())
                {
                    if (t.GetComponent<MeshFilter>().sharedMesh.vertices.Length * Quality > 100)
                    {
                        var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
                        meshSimplifier.Initialize(t.GetComponent<MeshFilter>().sharedMesh);
                        meshSimplifier.SimplifyMesh(Quality);
                        var destMesh = meshSimplifier.ToMesh();
                        t.GetComponent<MeshFilter>().sharedMesh = destMesh;
                    }
                }
            }
        }
#endif
        void Deform()
        {
            if (ObjectToDeform != null && DeformingShape != null)
            {
                if (DeformType == DeformingType.Subtract)
                {
                    Model result = CSG.Subtract(ObjectToDeform, DeformingShape);
                    Mesh mesh = result.mesh;
                    var composite = new GameObject();
                    composite.name = "DeformationResult";
                    composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                    composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                    composite.AddComponent<MeshCollider>();
                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
                if (DeformType == DeformingType.Unify)
                {
                    Model result = CSG.Union(ObjectToDeform, DeformingShape);
                    Mesh mesh = result.mesh;
                    var composite = new GameObject();
                    composite.name = "DeformationResult";
                    composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                    composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                    composite.AddComponent<MeshCollider>();
                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
                if (DeformType == DeformingType.Intersection)
                {
                    Model result = CSG.Intersect(ObjectToDeform, DeformingShape);
                    Mesh mesh = result.mesh;
                    var composite = new GameObject();
                    composite.name = "DeformationResult";
                    composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                    composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                    composite.AddComponent<MeshCollider>();
                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
        }
        void GenerateRandom()
        {
            Floors.Clear();
            string[] types = new string[] { "Normal", "Empire State Building" };
            string type = types[UnityEngine.Random.Range(0, types.Length)];
            int numberoffloors = 0;
            if (type == "Normal")
            {
                numberoffloors = UnityEngine.Random.Range(1, 101); ;
            }
            if (type == "Empire State Building")
            {
                numberoffloors = UnityEngine.Random.Range(1, 6); ;
            }
            for (int i = 0; i < numberoffloors; i++)
            {
                Floor floor = new Floor();
                //Floors
                int width = 0;
                int height = 0;
                int lenght = 0;
                if (i == 0)
                {
                    if (type == "Normal")
                    {
                        width = UnityEngine.Random.Range(20, 30);
                        height = UnityEngine.Random.Range(2, 5);
                        lenght = UnityEngine.Random.Range(20, 30);
                    }
                    if (type == "Empire State Building")
                    {
                        width = UnityEngine.Random.Range(30, 50);
                        height = UnityEngine.Random.Range(10, 15);
                        lenght = UnityEngine.Random.Range(30, 50);
                    }
                }
                else
                {
                    if (type == "Normal")
                    {
                        width = Floors[i - 1].width;
                        height = UnityEngine.Random.Range(2, 5);
                        lenght = Floors[i - 1].lenght;
                    }
                    if (type == "Empire State Building")
                    {
                        width = UnityEngine.Random.Range(Floors[i - 1].width - 5, Floors[i - 1].width);
                        height = UnityEngine.Random.Range(10, 50);
                        lenght = UnityEngine.Random.Range(Floors[i - 1].lenght - 5, Floors[i - 1].lenght);
                    }
                }
                //Windows
                floor.width = width;
                floor.height = height;
                floor.lenght = lenght;
                floor.WindowsOffset = RandomWindowsOffset;
                floor.WindowsX = width;
                floor.WindowsY = height;
                floor.WindowsZ = lenght;
                floor.NordWindow = true;
                floor.SouthWindow = true;
                floor.EastWindow = true;
                floor.WestWindow = true;
                AutoSize = true;
                //Doors
                if (i == 0)
                {
                    int doorsnum = UnityEngine.Random.Range(1, 5);
                    for (int door = 0; door < doorsnum; door++)
                    {
                        DoorModel model = new DoorModel();
                        model.Model = DoorGen;
                        int doordir = UnityEngine.Random.Range(0, 4);
                        int posx = 0;
                        if (doordir == 0)
                        {
                            model.Direction = DoorDirection.Nord;
                            posx = UnityEngine.Random.Range(-floor.width / 2 + 1, floor.width / 2 - 1);
                        }
                        if (doordir == 1)
                        {
                            model.Direction = DoorDirection.South;
                            posx = UnityEngine.Random.Range(-floor.width / 2 + 1, floor.width / 2 - 1);
                        }
                        if (doordir == 2)
                        {
                            model.Direction = DoorDirection.East;
                            posx = UnityEngine.Random.Range(-floor.lenght / 2 + 1, floor.lenght / 2 - 1);
                        }
                        if (doordir == 3)
                        {
                            model.Direction = DoorDirection.West;
                            posx = UnityEngine.Random.Range(-floor.lenght / 2 + 1, floor.lenght / 2 - 1);
                        }
                        model.Position = new Vector3(posx, 0, 0);
                        floor.Doors.Add(model);
                    }
                }
                Floors.Add(floor);
                //Balconies
                float buildingheight = 0;
                foreach (var fl in Floors)
                {
                    buildingheight += fl.height;
                }
                if (Balcony != null && buildingheight <= 30)
                {
                    WindowsPerBalcony = (width + lenght) / 6;
                }
            }
            StartGeneratorCoroutine(CreateBuilding(transform.position));
        }
        bool ClassEquals(Floor ost, Floor tst)
        {
            bool equals = false;
            if (ost.height == tst.height && ost.lenght == tst.lenght && ost.width == tst.width && ost.WindowsX == tst.WindowsX && ost.WindowsY == tst.WindowsY && ost.WindowsZ == tst.WindowsZ && ost.WindowHeight == tst.WindowHeight && ost.WindowLenght == tst.WindowLenght && ost.WindowWidth == tst.WindowWidth && ost.WindowsOffset == tst.WindowsOffset && ost.NordWindow == tst.NordWindow && ost.SouthWindow == tst.SouthWindow && ost.EastWindow == tst.EastWindow && ost.WestWindow == tst.WestWindow && ost.NordColumn == tst.NordColumn && ost.SouthColumn == tst.SouthColumn && ost.EastColumn == tst.EastColumn && ost.WestColumn == tst.WestColumn && ost.Deform == tst.Deform && ost.Doors.SequenceEqual(tst.Doors) && ost.Extenctions.SequenceEqual(tst.Extenctions) && ost.WallMods.SequenceEqual(tst.WallMods) && ost.WindowMaterials.SequenceEqual(tst.WindowMaterials))
            {
                equals = true;
            }
            else
            {
                equals = false;
            }
            return equals;
        }
        public IEnumerator CreateBuilding(Vector3 position)
        {
            Windows.Clear();
            InteriorsDictionnary.Clear();
            GizmosDictionary.Clear();
            FirstFloorPositionAssigned = false;
            GameObject Construction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Construction.name = Name;
            Construction.transform.position = position;
            Building = Construction.transform;
            DestroyImmediate(Construction.GetComponent<Collider>());
            DestroyImmediate(Construction.GetComponent<MeshFilter>());
            DestroyImmediate(Construction.GetComponent<MeshRenderer>());
            List<FloorModel> FloorModels = new List<FloorModel>();
            //Floors
            for (int floor = 0; floor < Floors.Count; floor++)
            {
                var fl = Floors[floor];
                //Building-------------------------------------------------------------------------------------------------
                if (floor == 0)
                {
                    GameObject Building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Building.name = "Floor";
                    Building.transform.position = position + new Vector3(0, fl.height * 0.5f, 0);
                    Building.transform.localScale = new Vector3(fl.width, fl.height, fl.lenght);
                    Building.transform.GetComponent<Renderer>().sharedMaterial = BuildingMaterial;
                    Building.transform.parent = Construction.transform;
                    FloorModel newmodel = new FloorModel() { Model = Building.transform, width = fl.width, height = fl.height, lenght = fl.lenght, WindowWidth = fl.WindowWidth, WindowHeight = fl.WindowHeight, WindowLenght = fl.WindowLenght, WindowsOffset = fl.WindowsOffset, WindowsX = fl.WindowsX, WindowsY = fl.WindowsY, WindowsZ = fl.WindowsZ, NordWindow = fl.NordWindow, SouthWindow = fl.SouthWindow, EastWindow = fl.EastWindow, WestWindow = fl.WestWindow, NordColumn = fl.NordColumn, SouthColumn = fl.SouthColumn, EastColumn = fl.EastColumn, WestColumn = fl.WestColumn, Deform = fl.Deform, Doors = fl.Doors, Extenctions = fl.Extenctions, WallMods = fl.WallMods, WindowMaterials = fl.WindowMaterials };
                    FloorModels.Add(newmodel);
                    if (fl.width > 0 && fl.height > 0 && fl.lenght > 0 && fl.Deform)
                    {
                        if (!FirstFloorPositionAssigned)
                        {
                            FirstFloorPositionAssigned = true;
                            FirstFloorPosition = Building.transform.position - new Vector3(0, fl.height * 0.5f, 0);
                        }
                    }
                }
                else
                {
                    bool samefloors = ClassEquals(Floors[floor], Floors[floor - 1]);
                    if (!samefloors)
                    {
                        GameObject Building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Building.name = "Floor";
                        Building.transform.position = new Vector3(position.x, FloorModels[FloorModels.Count - 1].Model.position.y + fl.height * 0.5f + FloorModels[FloorModels.Count - 1].Model.localScale.y * 0.5f, position.z);
                        Building.transform.localScale = new Vector3(fl.width, fl.height, fl.lenght);
                        Building.transform.GetComponent<Renderer>().sharedMaterial = BuildingMaterial;
                        Building.transform.parent = Construction.transform;
                        FloorModel newmodel = new FloorModel() { Model = Building.transform, width = fl.width, height = fl.height, lenght = fl.lenght, WindowWidth = fl.WindowWidth, WindowHeight = fl.WindowHeight, WindowLenght = fl.WindowLenght, WindowsOffset = fl.WindowsOffset, WindowsX = fl.WindowsX, WindowsY = fl.WindowsY, WindowsZ = fl.WindowsZ, NordWindow = fl.NordWindow, SouthWindow = fl.SouthWindow, EastWindow = fl.EastWindow, WestWindow = fl.WestWindow, NordColumn = fl.NordColumn, SouthColumn = fl.SouthColumn, EastColumn = fl.EastColumn, WestColumn = fl.WestColumn, Deform = fl.Deform, Doors = fl.Doors, Extenctions = fl.Extenctions, WallMods = fl.WallMods, WindowMaterials = fl.WindowMaterials };
                        FloorModels.Add(newmodel);
                        if (fl.width > 0 && fl.height > 0 && fl.lenght > 0 && fl.Deform)
                        {
                            if (!FirstFloorPositionAssigned)
                            {
                                FirstFloorPositionAssigned = true;
                                FirstFloorPosition = Building.transform.position - new Vector3(0, fl.height * 0.5f, 0);
                            }
                        }
                    }
                    else
                    {
                        FloorModels[FloorModels.Count - 1].Model.localScale += new Vector3(0, fl.height, 0);
                        FloorModels[FloorModels.Count - 1].Model.position = FloorModels[FloorModels.Count - 1].Model.position + new Vector3(0, fl.height * 0.5f, 0);
                    }
                }
            }
            //Extenctions
            for (int floor = 0; floor < FloorModels.Count; floor++)
            {
                FloorModel flmodel = FloorModels[floor];
                foreach (Extenction x in flmodel.Extenctions)
                {
                    GameObject Building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Building.name = "Extenction";
                    Building.transform.position = flmodel.Model.position - new Vector3(0, flmodel.Model.transform.lossyScale.y * 0.5f, 0) + new Vector3(x.Position.x * flmodel.Model.lossyScale.x * 0.5f + x.width * 0.5f * x.Position.x, x.height * 0.5f + x.Position.y, x.Position.z * flmodel.Model.lossyScale.z * 0.5f + x.lenght * 0.5f * x.Position.z);
                    Building.transform.localScale = new Vector3(x.width, x.height, x.lenght);
                    Building.transform.GetComponent<Renderer>().sharedMaterial = BuildingMaterial;
                    Building.transform.parent = flmodel.Model.transform;
                    x.Model = Building.transform;
                }
            }
            Debug.Log("Carcass generated!");
            yield return new WaitForSeconds(1f);
            //Other
            for (int floor = 0; floor < FloorModels.Count; floor++)
            {
                //Windows--------------------------------------------------------------------------------------------------
                FloorModel model = FloorModels[floor];
                float Width = model.WindowWidth;
                float Height = model.WindowHeight;
                float Lenght = model.WindowLenght;
                Transform Building = model.Model;
                float WindowsX = model.WindowsX;
                float WindowsY = model.WindowsY;
                float WindowsZ = model.WindowsZ;
                float WindowsOffset = model.WindowsOffset;
                Extenction[] exts = model.Extenctions.ToArray();
                List<Material> materials = new List<Material>();
                if (model.WindowMaterials.Count > 0)
                {
                    materials = model.WindowMaterials;
                }
                if (model.WindowMaterials.Count == 0)
                {
                    materials = Rooms.ToList();
                }
                //Floors----------------------------------------------------------------------------------------------------
                if (AutoSize)
                {
                    Width = Building.transform.localScale.x / (WindowsX * 1.3f);
                    Height = Building.transform.localScale.y / (WindowsY * 1.3f);
                    Lenght = Building.transform.localScale.z / (WindowsZ * 1.3f);
                }
                for (int currentfloor = 0; currentfloor < WindowsY; currentfloor++)
                {
                    for (int currentwindow = 0; currentwindow < WindowsX; currentwindow++)
                    {
                        float currentheight = (Building.position.y - Building.lossyScale.y / 2) + (Building.lossyScale.y / WindowsY) * currentfloor + Height * 0.75f;
                        //FrontWindows
                        if (FloorModels[floor].NordWindow)
                        {
                            Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z + Building.transform.lossyScale.z * 0.5f - Lenght * 0.001f - WindowsOffset);
                            Vector3 scale = new Vector3(Width * 0.99f, Height, Lenght * 0.01f);
                            Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                            if (cols.Length < 2)
                            {
                                GameObject Window = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                Window.name = "Window";
                                Window.transform.localScale = scale;
                                Window.transform.position = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z + Building.transform.lossyScale.z * 0.5f - Lenght * 0.001f - WindowsOffset);
                                Window.transform.eulerAngles = new Vector3(0, 0, 0);
                                GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                Vector3 WallPos = new Vector3(Window.transform.position.x, Window.transform.position.y, Building.transform.position.z + Building.transform.lossyScale.z / 2);
                                WindowBox.transform.localScale = new Vector3(Window.transform.lossyScale.x, Window.transform.lossyScale.y, Window.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window.transform.position.x, Window.transform.position.y, Window.transform.position.z + Window.transform.lossyScale.z / 2), WallPos));
                                WindowBox.transform.position = (Window.transform.position + WallPos) * 0.5f;
                                WindowBox.name = "WindowBox";
                                WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                WindowBox.GetComponent<MeshRenderer>().sharedMaterial = TransparentMaterial;
                                WindowBox.transform.parent = Building.transform;
                                Window.transform.parent = WindowBox.transform;
                                Window.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                {
                                    Windows.Add(Window.transform);
                                }
                            }
                        }
                        //BackWindows
                        if (FloorModels[floor].SouthWindow)
                        {
                            Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z - Building.transform.lossyScale.z * 0.5f + Lenght * 0.001f + WindowsOffset);
                            Vector3 scale = new Vector3(Width * 0.99f, Height, Lenght * 0.01f);
                            Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                            if (cols.Length < 2)
                            {
                                GameObject Window2 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                Window2.name = "Window";
                                Window2.transform.localScale = scale;
                                Window2.transform.position = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z - Building.transform.lossyScale.z * 0.5f + Lenght * 0.001f + WindowsOffset);
                                Window2.transform.eulerAngles = new Vector3(0, 180, 0);
                                GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                Vector3 WallPos = new Vector3(Window2.transform.position.x, Window2.transform.position.y, Building.transform.position.z - Building.transform.lossyScale.z / 2);
                                WindowBox.transform.localScale = new Vector3(Window2.transform.lossyScale.x, Window2.transform.lossyScale.y, Window2.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window2.transform.position.x, Window2.transform.position.y, Window2.transform.position.z + Window2.transform.lossyScale.z / 2), WallPos));
                                WindowBox.transform.position = (Window2.transform.position + WallPos) * 0.5f;
                                WindowBox.name = "WindowBox";
                                WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                WindowBox.GetComponent<MeshRenderer>().sharedMaterial = TransparentMaterial;
                                WindowBox.transform.parent = Building.transform;
                                Window2.transform.parent = WindowBox.transform;
                                Window2.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                {
                                    Windows.Add(Window2.transform);
                                }
                            }
                        }
                    }
                    for (int currentwindow = 0; currentwindow < WindowsZ; currentwindow++)
                    {
                        float currentheight = (Building.position.y - Building.lossyScale.y / 2) + (Building.lossyScale.y / WindowsY) * currentfloor + Height * 0.75f;
                        //LeftWindows
                        if (FloorModels[floor].EastWindow)
                        {
                            Vector3 pos = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x * 0.5f + Width * 0.001f + WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                            Vector3 scale = new Vector3(Lenght * 0.99f, Height, Width * 0.01f);
                            Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                            if (cols.Length < 2)
                            {
                                GameObject Window3 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                Window3.name = "Window";
                                Window3.transform.localScale = scale;
                                Window3.transform.position = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x * 0.5f + Width * 0.001f + WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                Window3.transform.eulerAngles = new Vector3(0, 270, 0);
                                GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                Vector3 WallPos = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x / 2, Window3.transform.position.y, Window3.transform.position.z);
                                WindowBox.transform.localScale = new Vector3(Window3.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window3.transform.position.x + Window3.transform.lossyScale.z / 2, Window3.transform.position.y, Window3.transform.position.z), WallPos), Window3.transform.lossyScale.y, Window3.transform.lossyScale.x);
                                WindowBox.transform.position = (Window3.transform.position + WallPos) * 0.5f;
                                WindowBox.name = "WindowBox";
                                WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                WindowBox.GetComponent<MeshRenderer>().sharedMaterial = TransparentMaterial;
                                WindowBox.transform.parent = Building.transform;
                                Window3.transform.parent = WindowBox.transform;
                                Window3.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                {
                                    Windows.Add(Window3.transform);
                                }
                            }
                        }
                        //RightWindows
                        if (FloorModels[floor].WestWindow)
                        {
                            Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x * 0.5f - Width * 0.001f - WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                            Vector3 scale = new Vector3(Lenght * 0.99f, Height, Width * 0.01f);
                            Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                            if (cols.Length < 2)
                            {
                                GameObject Window4 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                Window4.name = "Window";
                                Window4.transform.localScale = scale;
                                Window4.transform.position = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x * 0.5f - Width * 0.001f - WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                Window4.transform.eulerAngles = new Vector3(0, 90, 0);
                                GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                Vector3 WallPos = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x / 2, Window4.transform.position.y, Window4.transform.position.z);
                                WindowBox.transform.localScale = new Vector3(Window4.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window4.transform.position.x + Window4.transform.lossyScale.z / 2, Window4.transform.position.y, Window4.transform.position.z), WallPos), Window4.transform.lossyScale.y, Window4.transform.lossyScale.x);
                                WindowBox.transform.position = (Window4.transform.position + WallPos) * 0.5f;
                                WindowBox.name = "WindowBox";
                                WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                WindowBox.GetComponent<MeshRenderer>().sharedMaterial = TransparentMaterial;
                                WindowBox.transform.parent = Building.transform;
                                Window4.transform.parent = WindowBox.transform;
                                Window4.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                {
                                    Windows.Add(Window4.transform);
                                }
                            }
                        }
                    }
                }
                //RoofBordure----------------------------------------------------------------------------------------------
                if (floor != FloorModels.Count - 1)
                {
                    if (Building.position.y + Building.lossyScale.y / 2 > 10 && RoofBordure != null)
                    {
                        //FrontBordure
                        Vector3 scale = new Vector3(Building.transform.lossyScale.x, BordureHeight, BordureWidth);
                        Vector3 pos = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.lossyScale.y / 2 + BordureHeight / 2, Building.transform.lossyScale.z / 2 - BordureWidth / 2));
                        bool hasobstacles = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                        foreach (Collider col in cols)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles = true;
                            }
                        }
                        if (!hasobstacles)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale;
                            Bordure.transform.position = pos;
                            Bordure.transform.parent = Building.transform;
                        }
                        //BackBordure
                        Vector3 scale1 = new Vector3(Building.transform.lossyScale.x, BordureHeight, BordureWidth);
                        Vector3 pos1 = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.lossyScale.y / 2 + BordureHeight / 2, -Building.transform.lossyScale.z / 2 + BordureWidth / 2));
                        bool hasobstacles1 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols1 = Physics.OverlapBox(pos1, scale1 * 0.5f);
                        foreach (Collider col in cols1)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles1 = true;
                            }
                        }
                        if (!hasobstacles1)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale1;
                            Bordure.transform.position = pos1;
                            Bordure.transform.parent = Building.transform;
                        }
                        //RightBordure
                        Vector3 scale2 = new Vector3(BordureWidth, BordureHeight, Building.transform.lossyScale.z);
                        Vector3 pos2 = Building.transform.position + Building.transform.TransformDirection(new Vector3(Building.transform.lossyScale.x / 2 - BordureWidth / 2, Building.transform.lossyScale.y / 2 + BordureHeight / 2, 0));
                        bool hasobstacles2 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols2 = Physics.OverlapBox(pos2, scale2 * 0.5f);
                        foreach (Collider col in cols2)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles2 = true;
                            }
                        }
                        if (!hasobstacles2)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale2;
                            Bordure.transform.position = pos2;
                            Bordure.transform.parent = Building.transform;
                        }
                        //LeftBordure
                        Vector3 scale3 = new Vector3(BordureWidth, BordureHeight, Building.transform.lossyScale.z);
                        Vector3 pos3 = Building.transform.position + Building.transform.TransformDirection(new Vector3(-Building.transform.lossyScale.x / 2 + BordureWidth / 2, Building.transform.lossyScale.y / 2 + BordureHeight / 2, 0));
                        bool hasobstacles3 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols3 = Physics.OverlapBox(pos3, scale3 * 0.5f);
                        foreach (Collider col in cols3)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles3 = true;
                            }
                        }
                        if (!hasobstacles3)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale3;
                            Bordure.transform.position = pos3;
                            Bordure.transform.parent = Building.transform;
                        }
                    }
                }
                else
                {
                    if (Roof == null)
                    {
                        if (Building.position.y + Building.lossyScale.y / 2 > 10 && RoofBordure != null)
                        {
                            //FrontBordure
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = new Vector3(Building.transform.localScale.x, BordureHeight, BordureWidth);
                            Bordure.transform.position = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.localScale.y / 2 + BordureHeight / 2, Building.transform.localScale.z / 2 - BordureWidth / 2));
                            Bordure.transform.parent = Building.transform;
                            //BackBordure
                            GameObject Bordure1 = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure1.name = "Bordure";
                            Bordure1.transform.localScale = new Vector3(Building.transform.localScale.x, BordureHeight, BordureWidth);
                            Bordure1.transform.position = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.localScale.y / 2 + BordureHeight / 2, -Building.transform.localScale.z / 2 + BordureWidth / 2));
                            Bordure1.transform.parent = Building.transform;
                            //RightBordure
                            GameObject Bordure2 = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure2.name = "Bordure";
                            Bordure2.transform.localScale = new Vector3(BordureWidth, BordureHeight, Building.transform.localScale.z);
                            Bordure2.transform.position = Building.transform.position + Building.transform.TransformDirection(new Vector3(Building.transform.localScale.x / 2 - BordureWidth / 2, Building.transform.localScale.y / 2 + BordureHeight / 2, 0));
                            Bordure2.transform.parent = Building.transform;
                            //LeftBordure
                            GameObject Bordure3 = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure3.name = "Bordure";
                            Bordure3.transform.localScale = new Vector3(BordureWidth, BordureHeight, Building.transform.localScale.z);
                            Bordure3.transform.position = Building.transform.position + Building.transform.TransformDirection(new Vector3(-Building.transform.localScale.x / 2 + BordureWidth / 2, Building.transform.localScale.y / 2 + BordureHeight / 2, 0));
                            Bordure3.transform.parent = Building.transform;
                        }
                    }
                }
                //Columns--------------------------------------------------------------------------------------------------
                if (model.NordColumn)
                {
                    GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                    column.name = "Column";
                    column.transform.position = Building.position + new Vector3(Building.transform.localScale.x / 2, 0, Building.transform.localScale.z / 2);
                    column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                    column.transform.parent = Building;
                    if (CentralColumn != null)
                    {
                        for (int i = 1; i < WindowsX; i++)
                        {
                            column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                            column.name = "Column";
                            column.transform.position = Building.position + new Vector3(Building.transform.localScale.x / 2, 0, Building.transform.localScale.z / 2) - new Vector3((Width + CentralColumn.transform.lossyScale.x * 0.5f) * i - 0.015f * i, 0, 0);
                            column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                            column.transform.parent = Building;
                        }
                    }
                }
                if (model.SouthColumn)
                {
                    GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                    column.name = "Column";
                    column.transform.position = Building.position + new Vector3(Building.transform.localScale.x / 2, 0, -Building.transform.localScale.z / 2);
                    column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                    column.transform.parent = Building;
                    if (CentralColumn != null)
                    {
                        for (int i = 1; i < WindowsX; i++)
                        {
                            column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                            column.name = "Column";
                            column.transform.position = Building.position + new Vector3(Building.transform.localScale.x / 2, 0, -Building.transform.localScale.z / 2) - new Vector3((Width + CentralColumn.transform.lossyScale.x * 0.5f) * i - 0.015f * i, 0, 0);
                            column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                            column.transform.parent = Building;
                        }
                    }
                }
                if (model.EastColumn)
                {
                    GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                    column.name = "Column";
                    column.transform.position = Building.position + new Vector3(-Building.transform.localScale.x / 2, 0, Building.transform.localScale.z / 2);
                    column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                    column.transform.parent = Building;
                    if (CentralColumn != null)
                    {
                        for (int i = 1; i < WindowsZ; i++)
                        {
                            column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                            column.name = "Column";
                            column.transform.position = Building.position + new Vector3(Building.transform.localScale.x / 2, 0, Building.transform.localScale.z / 2) - new Vector3(0, 0, (Lenght + CentralColumn.transform.lossyScale.z * 0.5f) * i - 0.015f * i);
                            column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                            column.transform.parent = Building;
                        }
                    }
                }
                if (model.WestColumn)
                {
                    GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                    column.name = "Column";
                    column.transform.position = Building.position + new Vector3(-Building.transform.localScale.x / 2, 0, -Building.transform.localScale.z / 2);
                    column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                    column.transform.parent = Building;
                    if (CentralColumn != null)
                    {
                        for (int i = 1; i < WindowsZ; i++)
                        {
                            column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                            column.name = "Column";
                            column.transform.position = Building.position + new Vector3(-Building.transform.localScale.x / 2, 0, -Building.transform.localScale.z / 2) + new Vector3(0, 0, (Lenght + CentralColumn.transform.lossyScale.z * 0.5f) * i - 0.015f * i);
                            column.transform.localScale = new Vector3(column.transform.localScale.x, Building.transform.localScale.y + 0.001f, column.transform.localScale.z);
                            column.transform.parent = Building;
                        }
                    }
                }
                //Bordure--------------------------------------------------------------------------------------------------
                if (BordureBetweenFloors != null && floor != FloorModels.Count - 1)
                {
                    GameObject bordure = Instantiate(BordureBetweenFloors, Vector3.zero, Quaternion.identity);
                    bordure.name = "Bordure Between Floors";
                    bordure.transform.localScale = new Vector3(model.Model.lossyScale.x + FloorBordureScale.x, bordure.transform.lossyScale.y, model.Model.lossyScale.z + FloorBordureScale.y);
                    bordure.transform.position = model.Model.position + new Vector3(0, model.Model.lossyScale.y / 2, 0);
                    bordure.transform.parent = model.Model;
                }
                //Doors----------------------------------------------------------------------------------------------------
                StartGeneratorCoroutine(PlaceDoors(model));
                //Wall Mods------------------------------------------------------------------------------------------------
                StartGeneratorCoroutine(PlaceMods(model));
                //Extenctions----------------------------------------------------------------------------------------------
                foreach (Extenction x in exts)
                {
                    Width = x.WindowWidth;
                    Height = x.WindowHeight;
                    Lenght = x.WindowLenght;
                    Building = x.Model;
                    WindowsX = x.WindowsX;
                    WindowsY = x.WindowsY;
                    WindowsZ = x.WindowsZ;
                    WindowsOffset = x.WindowsOffset;
                    if (AutoSize)
                    {
                        Width = Building.transform.lossyScale.x / (WindowsX * 1.3f);
                        Height = Building.transform.lossyScale.y / (WindowsY * 1.3f);
                        Lenght = Building.transform.lossyScale.z / (WindowsZ * 1.3f);
                    }
                    for (int currentfloor = 0; currentfloor < WindowsY; currentfloor++)
                    {
                        for (int currentwindow = 0; currentwindow < WindowsX; currentwindow++)
                        {
                            float currentheight = (Building.position.y - Building.lossyScale.y / 2) + (Building.lossyScale.y / WindowsY) * currentfloor + Height * 0.75f;
                            //FrontWindows
                            if (x.NordWindow)
                            {
                                Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z + Building.transform.lossyScale.z * 0.5f - Lenght * 0.001f - WindowsOffset);
                                Vector3 scale = new Vector3(Width * 0.99f, Height, Lenght * 0.01f);
                                Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                                if (cols.Length < 2)
                                {
                                    GameObject Window = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                    Window.name = "Window";
                                    Window.transform.localScale = scale;
                                    Window.transform.position = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z + Building.transform.lossyScale.z * 0.5f - Lenght * 0.001f - WindowsOffset);
                                    Window.transform.eulerAngles = new Vector3(0, 0, 0);
                                    GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    Vector3 WallPos = new Vector3(Window.transform.position.x, Window.transform.position.y, Building.transform.position.z + Building.transform.lossyScale.z / 2);
                                    WindowBox.transform.localScale = new Vector3(Window.transform.lossyScale.x, Window.transform.lossyScale.y, Window.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window.transform.position.x, Window.transform.position.y, Window.transform.position.z + Window.transform.lossyScale.z / 2), WallPos));
                                    WindowBox.transform.position = (Window.transform.position + WallPos) * 0.5f;
                                    WindowBox.name = "WindowBox";
                                    WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                    WindowBox.transform.parent = Building.transform;
                                    Window.transform.parent = WindowBox.transform;
                                    Window.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                    if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                    {
                                        Windows.Add(Window.transform);
                                    }
                                }
                            }
                            //BackWindows
                            if (x.SouthWindow)
                            {
                                Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z - Building.transform.lossyScale.z * 0.5f + Lenght * 0.001f + WindowsOffset);
                                Vector3 scale = new Vector3(Width * 0.99f, Height, Lenght * 0.01f);
                                Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                                if (cols.Length < 2)
                                {
                                    GameObject Window2 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                    Window2.name = "Window";
                                    Window2.transform.localScale = scale;
                                    Window2.transform.position = new Vector3(Building.transform.position.x + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.x * 0.5f) - currentwindow * (Building.transform.lossyScale.x / WindowsX), 0, 0)).x - (Width * 0.625f), currentheight, Building.transform.position.z - Building.transform.lossyScale.z * 0.5f + Lenght * 0.001f + WindowsOffset);
                                    Window2.transform.eulerAngles = new Vector3(0, 180, 0);
                                    GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    Vector3 WallPos = new Vector3(Window2.transform.position.x, Window2.transform.position.y, Building.transform.position.z - Building.transform.lossyScale.z / 2);
                                    WindowBox.transform.localScale = new Vector3(Window2.transform.lossyScale.x, Window2.transform.lossyScale.y, Window2.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window2.transform.position.x, Window2.transform.position.y, Window2.transform.position.z + Window2.transform.lossyScale.z / 2), WallPos));
                                    WindowBox.transform.position = (Window2.transform.position + WallPos) * 0.5f;
                                    WindowBox.name = "WindowBox";
                                    WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                    WindowBox.transform.parent = Building.transform;
                                    Window2.transform.parent = WindowBox.transform;
                                    Window2.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                    if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                    {
                                        Windows.Add(Window2.transform);
                                    }
                                }
                            }
                        }
                        for (int currentwindow = 0; currentwindow < WindowsZ; currentwindow++)
                        {
                            float currentheight = (Building.position.y - Building.lossyScale.y / 2) + (Building.lossyScale.y / WindowsY) * currentfloor + Height * 0.75f;
                            //LeftWindows
                            if (x.EastWindow)
                            {
                                Vector3 pos = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x * 0.5f + Width * 0.001f + WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                Vector3 scale = new Vector3(Lenght * 0.99f, Height, Width * 0.01f);
                                Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                                if (cols.Length < 2)
                                {
                                    GameObject Window3 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                    Window3.name = "Window";
                                    Window3.transform.localScale = scale;
                                    Window3.transform.position = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x * 0.5f + Width * 0.001f + WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                    Window3.transform.eulerAngles = new Vector3(0, 270, 0);
                                    GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    Vector3 WallPos = new Vector3(Building.transform.position.x - Building.transform.lossyScale.x / 2, Window3.transform.position.y, Window3.transform.position.z);
                                    WindowBox.transform.localScale = new Vector3(Window3.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window3.transform.position.x + Window3.transform.lossyScale.z / 2, Window3.transform.position.y, Window3.transform.position.z), WallPos), Window3.transform.lossyScale.y, Window3.transform.lossyScale.x);
                                    WindowBox.transform.position = (Window3.transform.position + WallPos) * 0.5f;
                                    WindowBox.name = "WindowBox";
                                    WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                    WindowBox.transform.parent = Building.transform;
                                    Window3.transform.parent = WindowBox.transform;
                                    Window3.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                    if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                    {
                                        Windows.Add(Window3.transform);
                                    }
                                }
                            }
                            //RightWindows
                            if (x.WestWindow)
                            {
                                Vector3 pos = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x * 0.5f - Width * 0.001f - WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                Vector3 scale = new Vector3(Lenght * 0.99f, Height, Width * 0.01f);
                                Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                                if (cols.Length < 2)
                                {
                                    GameObject Window4 = Instantiate(WindowModel, Vector3.zero, Quaternion.identity);
                                    Window4.name = "Window";
                                    Window4.transform.localScale = scale;
                                    Window4.transform.position = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x * 0.5f - Width * 0.001f - WindowsOffset, currentheight, Building.transform.position.z + Building.transform.TransformDirection(new Vector3((Building.transform.lossyScale.z * 0.5f) - currentwindow * (Building.transform.lossyScale.z / WindowsZ), 0, 0)).x - (Lenght * 0.625f));
                                    Window4.transform.eulerAngles = new Vector3(0, 90, 0);
                                    GameObject WindowBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    Vector3 WallPos = new Vector3(Building.transform.position.x + Building.transform.lossyScale.x / 2, Window4.transform.position.y, Window4.transform.position.z);
                                    WindowBox.transform.localScale = new Vector3(Window4.transform.lossyScale.z * 0.5f + Vector3.Distance(new Vector3(Window4.transform.position.x + Window4.transform.lossyScale.z / 2, Window4.transform.position.y, Window4.transform.position.z), WallPos), Window4.transform.lossyScale.y, Window4.transform.lossyScale.x);
                                    WindowBox.transform.position = (Window4.transform.position + WallPos) * 0.5f;
                                    WindowBox.name = "WindowBox";
                                    WindowBox.GetComponent<MeshRenderer>().enabled = false;
                                    WindowBox.transform.parent = Building.transform;
                                    Window4.transform.parent = WindowBox.transform;
                                    Window4.transform.GetComponent<Builder.ChangeRoom>().AssignRoom(materials.ToArray());
                                    if (currentwindow != 0 && currentwindow != WindowsX - 1)
                                    {
                                        Windows.Add(Window4.transform);
                                    }
                                }
                            }
                        }
                    }
                    if (Building.position.y + Building.lossyScale.y / 2 > 10 && RoofBordure != null)
                    {
                        //FrontBordure
                        Vector3 scale = new Vector3(Building.transform.lossyScale.x, BordureHeight, BordureWidth);
                        Vector3 pos = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.lossyScale.y / 2 + BordureHeight / 2, Building.transform.lossyScale.z / 2 - BordureWidth / 2));
                        bool hasobstacles = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f);
                        foreach (Collider col in cols)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles = true;
                            }
                        }
                        if (!hasobstacles)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale;
                            Bordure.transform.position = pos;
                            Bordure.transform.parent = Building.transform;
                        }
                        //BackBordure
                        Vector3 scale1 = new Vector3(Building.transform.lossyScale.x, BordureHeight, BordureWidth);
                        Vector3 pos1 = Building.transform.position + Building.transform.TransformDirection(new Vector3(0, Building.transform.lossyScale.y / 2 + BordureHeight / 2, -Building.transform.lossyScale.z / 2 + BordureWidth / 2));
                        bool hasobstacles1 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols1 = Physics.OverlapBox(pos1, scale1 * 0.5f);
                        foreach (Collider col in cols1)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles1 = true;
                            }
                        }
                        if (!hasobstacles1)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale1;
                            Bordure.transform.position = pos1;
                            Bordure.transform.parent = Building.transform;
                        }
                        //RightBordure
                        Vector3 scale2 = new Vector3(BordureWidth, BordureHeight, Building.transform.lossyScale.z);
                        Vector3 pos2 = Building.transform.position + Building.transform.TransformDirection(new Vector3(Building.transform.lossyScale.x / 2 - BordureWidth / 2, Building.transform.lossyScale.y / 2 + BordureHeight / 2, 0));
                        bool hasobstacles2 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols2 = Physics.OverlapBox(pos2, scale2 * 0.5f);
                        foreach (Collider col in cols2)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles2 = true;
                            }
                        }
                        if (!hasobstacles2)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale2;
                            Bordure.transform.position = pos2;
                            Bordure.transform.parent = Building.transform;
                        }
                        //LeftBordure
                        Vector3 scale3 = new Vector3(BordureWidth, BordureHeight, Building.transform.lossyScale.z);
                        Vector3 pos3 = Building.transform.position + Building.transform.TransformDirection(new Vector3(-Building.transform.lossyScale.x / 2 + BordureWidth / 2, Building.transform.lossyScale.y / 2 + BordureHeight / 2, 0));
                        bool hasobstacles3 = false;
                        yield return new WaitForSeconds(0.2f);
                        Collider[] cols3 = Physics.OverlapBox(pos3, scale3 * 0.5f);
                        foreach (Collider col in cols3)
                        {
                            if (col.transform != Building.transform && col.transform.name != "Bordure" && col.transform.position.y - col.transform.lossyScale.y * 0.5f >= Building.position.y + Building.lossyScale.y * 0.5f)
                            {
                                hasobstacles3 = true;
                            }
                        }
                        if (!hasobstacles3)
                        {
                            GameObject Bordure = Instantiate(RoofBordure, Vector3.zero, Quaternion.identity);
                            Bordure.name = "Bordure";
                            Bordure.transform.localScale = scale3;
                            Bordure.transform.position = pos3;
                            Bordure.transform.parent = Building.transform;
                        }
                    }
                    //Columns--------------------------------------------------------------------------------------------------
                    if (x.NordColumn)
                    {
                        GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                        column.name = "Column";
                        column.transform.position = Building.position + new Vector3(Building.transform.lossyScale.x / 2, 0, Building.transform.lossyScale.z / 2);
                        column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                        column.transform.parent = Building;
                        if (CentralColumn != null)
                        {
                            for (int i = 1; i < WindowsX; i++)
                            {
                                column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                                column.name = "Column";
                                column.transform.position = Building.position + new Vector3(Building.transform.lossyScale.x / 2, 0, Building.transform.lossyScale.z / 2) - new Vector3((Width + CentralColumn.transform.lossyScale.x * 0.5f) * i - 0.015f * i, 0, 0);
                                column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                                column.transform.parent = Building;
                            }
                        }
                    }
                    if (x.SouthColumn)
                    {
                        GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                        column.name = "Column";
                        column.transform.position = Building.position + new Vector3(Building.transform.lossyScale.x / 2, 0, -Building.transform.lossyScale.z / 2);
                        column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                        column.transform.parent = Building;
                        if (CentralColumn != null)
                        {
                            for (int i = 1; i < WindowsX; i++)
                            {
                                column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                                column.name = "Column";
                                column.transform.position = Building.position + new Vector3(Building.transform.lossyScale.x / 2, 0, -Building.transform.lossyScale.z / 2) - new Vector3((Width + CentralColumn.transform.lossyScale.x * 0.5f) * i - 0.015f * i, 0, 0);
                                column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                                column.transform.parent = Building;
                            }
                        }
                    }
                    if (x.EastColumn)
                    {
                        GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                        column.name = "Column";
                        column.transform.position = Building.position + new Vector3(-Building.transform.lossyScale.x / 2, 0, Building.transform.lossyScale.z / 2);
                        column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                        column.transform.parent = Building;
                        if (CentralColumn != null)
                        {
                            for (int i = 1; i < WindowsZ; i++)
                            {
                                column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                                column.name = "Column";
                                column.transform.position = Building.position + new Vector3(Building.transform.lossyScale.x / 2, 0, Building.transform.lossyScale.z / 2) - new Vector3(0, 0, (Lenght + CentralColumn.transform.lossyScale.z * 0.5f) * i - 0.015f * i);
                                column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                                column.transform.parent = Building;
                            }
                        }
                    }
                    if (x.WestColumn)
                    {
                        GameObject column = Instantiate(Column, Vector3.zero, Quaternion.identity);
                        column.name = "Column";
                        column.transform.position = Building.position + new Vector3(-Building.transform.lossyScale.x / 2, 0, -Building.transform.lossyScale.z / 2);
                        column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                        column.transform.parent = Building;
                        if (CentralColumn != null)
                        {
                            for (int i = 1; i < WindowsZ; i++)
                            {
                                column = Instantiate(CentralColumn, Vector3.zero, Quaternion.identity);
                                column.name = "Column";
                                column.transform.position = Building.position + new Vector3(-Building.transform.lossyScale.x / 2, 0, -Building.transform.lossyScale.z / 2) + new Vector3(0, 0, (Lenght + CentralColumn.transform.lossyScale.z * 0.5f) * i - 0.015f * i);
                                column.transform.localScale = new Vector3(column.transform.lossyScale.x, Building.transform.lossyScale.y + 0.001f, column.transform.lossyScale.z);
                                column.transform.parent = Building;
                            }
                        }
                    }
                    StartGeneratorCoroutine(PlaceDoorsExtention(x));
                    StartGeneratorCoroutine(PlaceModsExtenction(x));
                }
            }
            Debug.Log("More details generated on floors!");
            //Balconies------------------------------------------------------------------------------------------------
            if (WindowsPerBalcony > 0 && Balcony != null)
            {
                int curid = WindowsPerBalcony;
                for (int window = 0; window < Windows.Count; window++)
                {
                    if (curid == WindowsPerBalcony)
                    {
                        if (Windows[window].position.y > 2)
                        {
                            GameObject balc = Instantiate(Balcony, Vector3.zero, Quaternion.identity);
                            balc.name = "Balcony";
                            balc.transform.position = Windows[window].position + Windows[window].TransformDirection(new Vector3(0, -(balc.transform.localScale.y / 2 + Door.transform.lossyScale.y * 0.5f), balc.transform.localScale.z / 2));
                            balc.transform.eulerAngles = Windows[window].eulerAngles;
                            balc.transform.parent = Windows[window].parent.transform.parent.transform;
                            balc.GetComponent<Builder.Balcony>().GenerateDetails();
                            Windows[window].gameObject.SetActive(false);
                            GameObject door = Instantiate(Door, Windows[window].position, Windows[window].rotation);
                            door.transform.parent = balc.transform;
                            door.name = "Door";
                            door.layer = 7;
                            if (door.GetComponent<Builder.ChangeRoom>())
                            {
                                Material[] mats = new Material[1] { Windows[window].GetComponent<Builder.ChangeRoom>().Mat.sharedMaterial };
                                door.GetComponent<Builder.ChangeRoom>().AssignRoom(mats);
                            }
                            if (Windows[window].parent.transform != null)
                            {
                                DestroyImmediate(Windows[window].parent.transform.gameObject);
                            }
                            curid = 0;
                        }
                    }
                    else
                    {
                        curid += 1;
                    }
                }
            }
            if (Balcony != null)
            {
                Debug.Log("Balconies generated!");
            }
            //Roof-----------------------------------------------------------------------------------------------------
            Debug.Log("Generating roof!");
            if (Roof != null)
            {
                GameObject roof = Instantiate(Roof, Vector3.zero, Quaternion.identity);
                roof.name = "Roof";
                roof.transform.parent = Construction.transform;
                roof.transform.localScale = new Vector3(FloorModels[FloorModels.Count - 1].Model.localScale.x, (FloorModels[FloorModels.Count - 1].Model.localScale.x + FloorModels[FloorModels.Count - 1].Model.localScale.z) / 8, FloorModels[FloorModels.Count - 1].Model.localScale.z);
                roof.transform.position = FloorModels[FloorModels.Count - 1].Model.position + new Vector3(0, FloorModels[FloorModels.Count - 1].Model.localScale.y / 2 + roof.transform.localScale.y / 2, 0);
            }
            //Roof Mods------------------------------------------------------------------------------------------------
            for (int i5 = 0; i5 < FloorModels.Count; i5++)
            {
                var fl = FloorModels[i5];
                Transform lastfloor = fl.Model;
                if (lastfloor.lossyScale.x > 3 && lastfloor.lossyScale.z > 3)
                {
                    if (lastfloor == FloorModels[FloorModels.Count - 1].Model)
                    {
                        if (Roof == null)
                        {
                            List<RoofSection> RoofSections = new List<RoofSection>();
                            for (int i = 0; i < RoofMods.Length; i++)
                            {
                                if (UnityEngine.Random.Range(0, 2) == 1)
                                {
                                    Vector3 pos = new Vector3(lastfloor.position.x + UnityEngine.Random.Range(-lastfloor.lossyScale.x + 2, lastfloor.lossyScale.x - 2), lastfloor.position.y + lastfloor.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, lastfloor.position.z + UnityEngine.Random.Range(-lastfloor.lossyScale.z + 2, lastfloor.lossyScale.z - 2));
                                    pos.x = Mathf.Clamp(pos.x, lastfloor.position.x - lastfloor.lossyScale.x / 2 + 1, lastfloor.position.x + lastfloor.lossyScale.x / 2 - 1);
                                    pos.z = Mathf.Clamp(pos.z, lastfloor.position.z - lastfloor.lossyScale.z / 2 + 1, lastfloor.position.z + lastfloor.lossyScale.z / 2 - 1);
                                    RoofSection rs = new RoofSection()
                                    {
                                        Position = pos,
                                        Obj = RoofMods[i],
                                        Rotation = UnityEngine.Random.Range(0, 2) * 90,
                                        Radius = UnityEngine.Random.Range(1, 5),
                                        Num = UnityEngine.Random.Range(1, 4),
                                    };
                                    RoofSections.Add(rs);
                                }
                                foreach (RoofSection rs in RoofSections)
                                {
                                    for (int n = 0; n < rs.Num; n++)
                                    {
                                        Vector3 pos = new Vector3(rs.Position.x + UnityEngine.Random.Range(-rs.Radius, rs.Radius), lastfloor.position.y + lastfloor.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, rs.Position.z + UnityEngine.Random.Range(-rs.Radius, rs.Radius));
                                        pos.x = Mathf.Clamp(pos.x, lastfloor.position.x - lastfloor.lossyScale.x / 2 + 1, lastfloor.position.x + lastfloor.lossyScale.x / 2 - 1);
                                        pos.z = Mathf.Clamp(pos.z, lastfloor.position.z - lastfloor.lossyScale.z / 2 + 1, lastfloor.position.z + lastfloor.lossyScale.z / 2 - 1);
                                        Quaternion rot = Quaternion.Euler(0, rs.Rotation, 0);
                                        bool Hasobstacles = false;
                                        yield return new WaitForSecondsRealtime(TimeToGenerate);
                                        Collider[] cols = Physics.OverlapBox(pos, RoofMods[i].transform.lossyScale / 2, rot);
                                        foreach (Collider col in cols)
                                        {
                                            if (col.transform != lastfloor)
                                            {
                                                Hasobstacles = true;
                                            }
                                        }
                                        if (!Hasobstacles)
                                        {
                                            GameObject obj = Instantiate(RoofMods[i], Vector3.zero, rot, Construction.transform);
                                            for (int x = 70; x < 100; x++)
                                            {
                                                obj.transform.position = new Vector3(pos.x, pos.y * x * 0.01f, pos.z);
                                                yield return new WaitForSecondsRealtime(0.01f);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        List<RoofSection> RoofSections = new List<RoofSection>();
                        for (int i = 0; i < RoofMods.Length; i++)
                        {
                            if (UnityEngine.Random.Range(0, 2) == 1)
                            {
                                Vector3 pos = new Vector3(lastfloor.position.x + UnityEngine.Random.Range(-lastfloor.lossyScale.x + 2, lastfloor.lossyScale.x - 2), lastfloor.position.y + lastfloor.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, lastfloor.position.z + UnityEngine.Random.Range(-lastfloor.lossyScale.z + 2, lastfloor.lossyScale.z - 2));
                                pos.x = Mathf.Clamp(pos.x, lastfloor.position.x - lastfloor.lossyScale.x / 2 + 1, lastfloor.position.x + lastfloor.lossyScale.x / 2 - 1);
                                pos.z = Mathf.Clamp(pos.z, lastfloor.position.z - lastfloor.lossyScale.z / 2 + 1, lastfloor.position.z + lastfloor.lossyScale.z / 2 - 1);
                                RoofSection rs = new RoofSection()
                                {
                                    Position = pos,
                                    Obj = RoofMods[i],
                                    Rotation = UnityEngine.Random.Range(0, 2) * 90,
                                    Radius = UnityEngine.Random.Range(1, 5),
                                    Num = UnityEngine.Random.Range(1, 4),
                                };
                                RoofSections.Add(rs);
                            }
                            foreach (RoofSection rs in RoofSections)
                            {
                                for (int n = 0; n < rs.Num; n++)
                                {
                                    Vector3 pos = new Vector3(rs.Position.x + UnityEngine.Random.Range(-rs.Radius, rs.Radius), lastfloor.position.y + lastfloor.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, rs.Position.z + UnityEngine.Random.Range(-rs.Radius, rs.Radius));
                                    pos.x = Mathf.Clamp(pos.x, lastfloor.position.x - lastfloor.lossyScale.x / 2 + 1, lastfloor.position.x + lastfloor.lossyScale.x / 2 - 1);
                                    pos.z = Mathf.Clamp(pos.z, lastfloor.position.z - lastfloor.lossyScale.z / 2 + 1, lastfloor.position.z + lastfloor.lossyScale.z / 2 - 1);
                                    Quaternion rot = Quaternion.Euler(0, rs.Rotation, 0);
                                    bool Hasobstacles = false;
                                    yield return new WaitForSecondsRealtime(TimeToGenerate);
                                    Collider[] cols = Physics.OverlapBox(pos, RoofMods[i].transform.lossyScale / 2, rot);
                                    foreach (Collider col in cols)
                                    {
                                        if (col.transform != lastfloor)
                                        {
                                            Hasobstacles = true;
                                        }
                                    }
                                    if (!Hasobstacles)
                                    {
                                        GameObject obj = Instantiate(RoofMods[i], Vector3.zero, rot, Construction.transform);
                                        for (int x = 70; x < 100; x++)
                                        {
                                            obj.transform.position = new Vector3(pos.x, pos.y * x * 0.01f, pos.z);
                                            yield return new WaitForSecondsRealtime(0.01f);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (Extenction ex in fl.Extenctions)
                {
                    Transform lastfloorex = ex.Model;
                    if (lastfloorex.lossyScale.x > 10 && lastfloorex.lossyScale.z > 10)
                    {
                        List<RoofSection> RoofSections = new List<RoofSection>();
                        for (int i = 0; i < RoofMods.Length; i++)
                        {
                            if (UnityEngine.Random.Range(0, 2) == 1)
                            {
                                Vector3 pos = new Vector3(lastfloorex.position.x + UnityEngine.Random.Range(-lastfloorex.lossyScale.x + 2, lastfloorex.lossyScale.x - 2), lastfloorex.position.y + lastfloorex.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, lastfloorex.position.z + UnityEngine.Random.Range(-lastfloorex.lossyScale.z + 2, lastfloorex.lossyScale.z - 2));
                                pos.x = Mathf.Clamp(pos.x, lastfloorex.position.x - lastfloorex.lossyScale.x / 2 + 1, lastfloorex.position.x + lastfloorex.lossyScale.x / 2 - 1);
                                pos.z = Mathf.Clamp(pos.z, lastfloorex.position.z - lastfloorex.lossyScale.z / 2 + 1, lastfloorex.position.z + lastfloorex.lossyScale.z / 2 - 1);
                                RoofSection rs = new RoofSection()
                                {
                                    Position = pos,
                                    Obj = RoofMods[i],
                                    Rotation = UnityEngine.Random.Range(0, 2) * 90,
                                    Radius = UnityEngine.Random.Range(1, 5),
                                    Num = UnityEngine.Random.Range(1, 4),
                                };
                                RoofSections.Add(rs);
                            }
                            foreach (RoofSection rs in RoofSections)
                            {
                                for (int n = 0; n < rs.Num; n++)
                                {
                                    Vector3 pos = new Vector3(rs.Position.x + UnityEngine.Random.Range(-rs.Radius, rs.Radius), lastfloorex.position.y + lastfloorex.lossyScale.y / 2 + RoofMods[i].transform.lossyScale.y / 2, rs.Position.z + UnityEngine.Random.Range(-rs.Radius, rs.Radius));
                                    pos.x = Mathf.Clamp(pos.x, lastfloorex.position.x - lastfloorex.lossyScale.x / 2 + 1, lastfloorex.position.x + lastfloorex.lossyScale.x / 2 - 1);
                                    pos.z = Mathf.Clamp(pos.z, lastfloorex.position.z - lastfloorex.lossyScale.z / 2 + 1, lastfloorex.position.z + lastfloorex.lossyScale.z / 2 - 1);
                                    Quaternion rot = Quaternion.Euler(0, rs.Rotation, 0);
                                    bool Hasobstacles = false;
                                    yield return new WaitForSecondsRealtime(TimeToGenerate);
                                    Collider[] cols = Physics.OverlapBox(pos, RoofMods[i].transform.lossyScale / 2, rot);
                                    foreach (Collider col in cols)
                                    {
                                        if (col.transform != lastfloorex)
                                        {
                                            Hasobstacles = true;
                                        }
                                    }
                                    if (!Hasobstacles)
                                    {
                                        GameObject obj = Instantiate(RoofMods[i], Vector3.zero, rot, Construction.transform);
                                        for (int x = 70; x < 100; x++)
                                        {
                                            obj.transform.position = new Vector3(pos.x, pos.y * x * 0.01f, pos.z);
                                            yield return new WaitForSecondsRealtime(0.01f);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Debug.Log(i5 + " floor's roof generated!");
            }
            Debug.Log("Roof generated!");
            //LOD------------------------------------------------------------------------------------------------------
            Transform[] Parts = Construction.transform.GetComponentsInChildren<Transform>();
            if (CreateLOD == lod.Full)
            {
                LODGroup lod = Construction.AddComponent<LODGroup>();
                LOD[] lods = new LOD[2];
                //LOD1
                List<Renderer> r1 = new List<Renderer>();
                foreach (Transform part in Parts)
                {
                    if (part.GetComponent<Renderer>())
                    {
                        r1.Add(part.GetComponent<Renderer>());
                    }
                }
                //LOD2
                GameObject fakebuilding = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float width = 0;
                float height = 0;
                float lenght = 0;
                Vector3 Buildingpos = Vector3.zero;
                foreach (var part in FloorModels)
                {
                    if (part.Model.lossyScale.x > width)
                    {
                        width = part.Model.lossyScale.x;
                    }
                    height += part.Model.lossyScale.y;
                    if (part.Model.lossyScale.z > lenght)
                    {
                        lenght = part.Model.lossyScale.z;
                    }
                    Buildingpos += part.Model.position;
                }
                Buildingpos = new Vector3(Buildingpos.x / FloorModels.Count, height * 0.5f, Buildingpos.z / FloorModels.Count);
                fakebuilding.transform.position = Buildingpos;
                fakebuilding.transform.localScale = new Vector3(width, height, lenght);
                fakebuilding.transform.parent = Construction.transform;
                fakebuilding.GetComponent<Renderer>().sharedMaterial = BuildingMaterial;
                List<Renderer> r2 = new List<Renderer>();
                r2.Add(fakebuilding.GetComponent<Renderer>());
                //LODS
                lods[0] = new LOD(0.4f, r1.ToArray());
                lods[1] = new LOD(0.1f, r2.ToArray());
                lod.SetLODs(lods);
                lod.RecalculateBounds();
            }
            if (CreateLOD == lod.Partial)
            {
                LODGroup lod = Construction.AddComponent<LODGroup>();
                LOD[] lods = new LOD[2];
                //LOD1
                List<Renderer> r1 = new List<Renderer>();
                foreach (var part in FloorModels)
                {
                    if (part.Model.GetComponent<Renderer>())
                    {
                        r1.Add(part.Model.GetComponent<Renderer>());
                    }
                }
                //LOD2
                GameObject fakebuilding = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float width = 0;
                float height = 0;
                float lenght = 0;
                Vector3 Buildingpos = Vector3.zero;
                foreach (var part in FloorModels)
                {
                    if (part.Model.lossyScale.x > width)
                    {
                        width = part.Model.lossyScale.x;
                    }
                    height += part.Model.lossyScale.y;
                    if (part.Model.lossyScale.z > lenght)
                    {
                        lenght = part.Model.lossyScale.z;
                    }
                    Buildingpos += part.Model.position;
                }
                Buildingpos = new Vector3(Buildingpos.x / FloorModels.Count, height * 0.5f, Buildingpos.z / FloorModels.Count);
                fakebuilding.transform.position = Buildingpos;
                fakebuilding.transform.localScale = new Vector3(width, height, lenght);
                fakebuilding.transform.parent = Construction.transform;
                fakebuilding.GetComponent<Renderer>().sharedMaterial = BuildingMaterial;
                List<Renderer> r2 = new List<Renderer>();
                r2.Add(fakebuilding.GetComponent<Renderer>());
                //LODS
                lods[0] = new LOD(0.4f, r1.ToArray());
                lods[1] = new LOD(0.1f, r2.ToArray());
                lod.SetLODs(lods);
                lod.RecalculateBounds();
            }
            Debug.Log("LOD generated!");
            //Deform Building------------------------------------------------------------------------------------------
            Debug.Log("Deforming building!");
            if (Interior)
            {
                foreach (var fl in FloorModels)
                {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.localScale = fl.Model.lossyScale * 0.99f;
                    obj.transform.position = fl.Model.position;
                    obj.transform.parent = fl.Model.transform;
                    obj.transform.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    InteriorsDictionnary.Add(fl.Model, obj.transform);
                    foreach (var ex in fl.Extenctions)
                    {
                        GameObject obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obj1.transform.localScale = ex.Model.lossyScale * 0.99f;
                        obj1.transform.position = ex.Model.position;
                        obj1.transform.parent = ex.Model.transform;
                        obj1.transform.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                        InteriorsDictionnary.Add(ex.Model, obj1.transform);
                    }
                }
            }
            yield return new WaitForSeconds(1f);
            StartGeneratorCoroutine(DeformBuilding(FloorModels.ToArray()));
            yield return new WaitForSeconds(1f);
            //Static---------------------------------------------------------------------------------------------------
            Transform[] Parts2 = Construction.transform.GetComponentsInChildren<Transform>();
            foreach (Transform part in Parts2)
            {
                part.gameObject.isStatic = true;
            }
            Debug.Log("DONE! :)");
            Console.Clear();
        }
        IEnumerator PlaceDoors(FloorModel floor)
        {
            yield return new WaitForSeconds(1);
            for (int door = 0; door < floor.Doors.Count; door++)
            {
                var Door = floor.Doors[door];
                Transform floormodel = floor.Model;
                GameObject doorobject = Instantiate(Door.Model, Vector3.zero, Quaternion.identity);
                doorobject.name = "Door";
                doorobject.layer = 7;
                if (doorobject.GetComponent<Builder.ChangeRoom>())
                {
                    doorobject.GetComponent<Builder.ChangeRoom>().AssignRoom(Rooms);
                }
                if (Door.Direction == DoorDirection.Nord)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 180, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(0, -floormodel.lossyScale.y / 2f + doorobject.transform.lossyScale.y / 2f, floormodel.lossyScale.z / 2f + doorobject.transform.lossyScale.z / 4f)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.South)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 0, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(0, floormodel.lossyScale.y / 2f - doorobject.transform.lossyScale.y / 2f, floormodel.lossyScale.z / 2f + doorobject.transform.lossyScale.z / 4f)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.East)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, -90, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2f + doorobject.transform.lossyScale.z / 4f, -floormodel.lossyScale.y / 2f + doorobject.transform.lossyScale.y / 2f, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.West)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 90, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2f + doorobject.transform.lossyScale.z / 4f, floormodel.lossyScale.y / 2f - doorobject.transform.lossyScale.y / 2f, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                doorobject.transform.parent = floormodel;
                Collider[] cols = Physics.OverlapBox(doorobject.transform.position, doorobject.transform.lossyScale * 0.5f, doorobject.transform.rotation);
                for (int c = 0; c < cols.Length; c++)
                {
                    Collider col = cols[c];
                    if (col != null)
                    {
                        if (col.transform.name == "WindowBox")
                        {
                            DestroyImmediate(col.transform.gameObject);
                        }
                    }
                }
            }
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                t.gameObject.isStatic = true;
            }
        }
        IEnumerator PlaceDoorsExtention(Extenction floor)
        {
            yield return new WaitForSeconds(1);
            for (int door = 0; door < floor.Doors.Count; door++)
            {
                var Door = floor.Doors[door];
                Transform floormodel = floor.Model;
                GameObject doorobject = Instantiate(Door.Model, Vector3.zero, Quaternion.identity);
                doorobject.name = "Door";
                doorobject.layer = 7;
                if (doorobject.GetComponent<Builder.ChangeRoom>())
                {
                    doorobject.GetComponent<Builder.ChangeRoom>().AssignRoom(Rooms);
                }
                if (Door.Direction == DoorDirection.Nord)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 180, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(0, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 4)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.South)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 0, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(0, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 4)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.East)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, -90, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 4, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.West)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 90, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 4, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                doorobject.transform.parent = floormodel;
                Collider[] cols = Physics.OverlapBox(doorobject.transform.position, doorobject.transform.lossyScale * 0.5f, doorobject.transform.rotation);
                for (int c = 0; c < cols.Length; c++)
                {
                    Collider col = cols[c];
                    if (col != null)
                    {
                        if (col.transform.name == "WindowBox")
                        {
                            DestroyImmediate(col.transform.gameObject);
                        }
                    }
                }
            }
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                t.gameObject.isStatic = true;
            }
        }
        IEnumerator PlaceMods(FloorModel floor)
        {
            yield return new WaitForSeconds(1);
            for (int door = 0; door < floor.WallMods.Count; door++)
            {
                var Door = floor.WallMods[door];
                Transform floormodel = floor.Model;
                GameObject doorobject = Instantiate(Door.Model, Vector3.zero, Quaternion.identity);
                doorobject.name = "WallMod";
                if (Door.Direction == DoorDirection.Nord)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 180, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(0, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 2)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.South)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 0, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(0, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 2)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.East)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, -90, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 2, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.West)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 90, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 2, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                doorobject.transform.parent = floormodel;
                if (Door.Obstacle)
                {
                    Collider[] cols = Physics.OverlapBox(doorobject.transform.position, doorobject.transform.lossyScale * 0.5f, doorobject.transform.rotation);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        Collider col = cols[c];
                        if (col != null)
                        {
                            if (col.transform.name == "WindowBox")
                            {
                                DestroyImmediate(col.transform.gameObject);
                            }
                        }
                    }
                }
            }
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                t.gameObject.isStatic = true;
            }
        }
        IEnumerator PlaceModsExtenction(Extenction floor)
        {
            yield return new WaitForSeconds(1);
            for (int door = 0; door < floor.WallMods.Count; door++)
            {
                var Door = floor.WallMods[door];
                Transform floormodel = floor.Model;
                GameObject doorobject = Instantiate(Door.Model, Vector3.zero, Quaternion.identity);
                doorobject.name = "WallMod";
                if (Door.Direction == DoorDirection.Nord)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 180, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(0, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 2)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.South)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.x * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.x * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 0, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(0, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, floormodel.lossyScale.z / 2 + doorobject.transform.lossyScale.z / 2)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.East)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, -90, 0);
                    doorobject.transform.position = floormodel.position + floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 2, -floormodel.lossyScale.y / 2 + doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                if (Door.Direction == DoorDirection.West)
                {
                    Door.Position.x = Mathf.Clamp(Door.Position.x, floormodel.lossyScale.z * -0.5f + doorobject.transform.lossyScale.x * 0.5f, floormodel.lossyScale.z * 0.5f - doorobject.transform.lossyScale.x * 0.5f);
                    Door.Position.y = Mathf.Clamp(Door.Position.y, floormodel.lossyScale.y * -0.5f + doorobject.transform.lossyScale.y * 0.5f, floormodel.lossyScale.y * 0.5f - doorobject.transform.lossyScale.y * 0.5f);
                    doorobject.transform.eulerAngles = new Vector3(0, 90, 0);
                    doorobject.transform.position = floormodel.position - floormodel.TransformDirection(new Vector3(floormodel.lossyScale.x / 2 + doorobject.transform.lossyScale.z / 2, floormodel.lossyScale.y / 2 - doorobject.transform.lossyScale.y / 2, 0)) + doorobject.transform.TransformDirection(Door.Position);
                }
                doorobject.transform.parent = floormodel;
                if (Door.Obstacle)
                {
                    Collider[] cols = Physics.OverlapBox(doorobject.transform.position, doorobject.transform.lossyScale * 0.5f, doorobject.transform.rotation);
                    for (int c = 0; c < cols.Length; c++)
                    {
                        Collider col = cols[c];
                        if (col != null)
                        {
                            if (col.transform.name == "WindowBox")
                            {
                                DestroyImmediate(col.transform.gameObject);
                            }
                        }
                    }
                }
            }
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                t.gameObject.isStatic = true;
            }
        }
        IEnumerator DeformBuilding(FloorModel[] FloorModels)
        {
            List<Transform> Difformedcolumns = new List<Transform>();
            Transform[] ts5 = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts5)
            {
                if (t != null)
                {
                    if (t.name == "Column" && !Difformedcolumns.Contains(t))
                    {
                        Collider[] cols = Physics.OverlapBox(t.position, t.lossyScale * 0.5f, t.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col != null)
                            {
                                if (col.transform.name == "Door")
                                {
                                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    obj.transform.position = col.transform.position;
                                    obj.transform.rotation = col.transform.rotation;
                                    obj.transform.localScale = col.transform.lossyScale * 2f;
                                    try
                                    {
                                        Model result = CSG.Subtract(t.gameObject, obj.gameObject);
                                        Mesh mesh = result.mesh;
                                        var composite = new GameObject();
                                        composite.name = "Column";
                                        composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                                        composite.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { t.GetComponent<Renderer>().sharedMaterial, t.GetComponent<Renderer>().sharedMaterial };
                                        composite.AddComponent<MeshCollider>();
                                        composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                                        composite.transform.parent = t.parent.transform;
                                        DestroyImmediate(t.gameObject);
                                    }
                                    catch
                                    {
                                        Debug.Log("Couldn't deform a column");
                                    }
                                    DestroyImmediate(obj);
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < FloorModels.Length; i++)
            {
                FloorModel part = FloorModels[i];
                Vector3 scale = part.Model.lossyScale;
                Vector3 pos = part.Model.position;
                GameObject subtractfrom = null;
                List<Transform> Partsinside = part.Model.transform.GetComponentsInChildren<Transform>().ToList();
                List<Transform> Partstomove = new List<Transform>();
                foreach (var t in Partsinside)
                {
                    if (t.transform.parent == part.Model.transform)
                    {
                        Partstomove.Add(t);
                    }
                }
                Material mat = part.Model.GetComponent<Renderer>().sharedMaterial;
                if (Interior && part.Deform)
                {
                    Transform obj = null;
                    InteriorsDictionnary.TryGetValue(part.Model.transform, out obj);
                    Model result1 = CSG.Subtract(part.Model.gameObject, obj.gameObject);
                    Mesh mesh = result1.mesh;
                    var composite = new GameObject();
                    composite.name = "Floor";
                    composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                    composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                    composite.AddComponent<MeshCollider>();
                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                    subtractfrom = composite;
                    if (subtractfrom != null)
                    {
                        subtractfrom.transform.parent = part.Model.parent.transform;
                        foreach (Transform t in Partsinside)
                        {
                            if (t.parent.transform == part.Model)
                            {
                                t.parent = subtractfrom.transform;
                            }
                        }
                        DestroyImmediate(part.Model.gameObject);
                        Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f, Quaternion.identity);
                        foreach (Collider col in cols)
                        {
                            Transform t = col.transform;
                            if (t.name == "Column")
                            {
                                Model m = CSG.Subtract(t.gameObject, obj.gameObject);
                                var colm = new GameObject();
                                colm.name = t.name + "Modified";
                                colm.AddComponent<MeshFilter>().sharedMesh = m.mesh;
                                colm.AddComponent<MeshRenderer>().sharedMaterials = m.materials.ToArray();
                                colm.AddComponent<MeshCollider>();
                                colm.GetComponent<MeshCollider>().sharedMesh = m.mesh;
                                if (t.parent.transform != subtractfrom.transform)
                                {
                                    colm.transform.parent = t.parent.transform;
                                }
                                else
                                {
                                    colm.transform.parent = subtractfrom.transform;
                                    Partstomove.Add(colm.transform);
                                }
                                DestroyImmediate(t.gameObject);
                            }
                            if (t != null)
                            {
                                if (t.name == "Bordure Between Floors")
                                {
                                    Model m = CSG.Subtract(t.gameObject, obj.gameObject);
                                    var colm = new GameObject();
                                    colm.name = t.name;
                                    colm.AddComponent<MeshFilter>().sharedMesh = m.mesh;
                                    colm.AddComponent<MeshRenderer>().sharedMaterials = m.materials.ToArray();
                                    colm.AddComponent<MeshCollider>();
                                    colm.GetComponent<MeshCollider>().sharedMesh = m.mesh;
                                    if (t.parent.transform != subtractfrom.transform)
                                    {
                                        colm.transform.parent = t.parent.transform;
                                    }
                                    else
                                    {
                                        colm.transform.parent = subtractfrom.transform;
                                        Partstomove.Add(colm.transform);
                                    }
                                    DestroyImmediate(t.gameObject);
                                }
                            }
                        }
                    }
                    DestroyImmediate(obj.gameObject);
                }
                yield return new WaitForSecondsRealtime(1f);
                if (part.Deform)
                {
                    foreach (Transform t in Partstomove)
                    {
                        if (t != null)
                        {
                            t.parent = Building;
                        }
                    }
                    foreach (Transform t in Partsinside)
                    {
                        if (t != null)
                        {
                            if (t.name != "Floor" && t.name != "Extenction" && t.name != "Wall" && t.name != "FloorModified" && t.name != "WallModified" && t.GetComponent<Collider>() && t != part.Model && t.GetComponent<Renderer>() && t.GetComponent<MeshFilter>())
                            {
                                if (t.GetComponent<Renderer>().sharedMaterial != null)
                                {
                                    if (t.parent.transform == Building)
                                    {
                                        int numberoftries = 0;
                                        for (int i2 = 0; i2 < 1; i2++)
                                        {
                                            try
                                            {
                                                Model result = null;
                                                if (subtractfrom == null)
                                                {
                                                    result = CSG.Subtract(part.Model.gameObject, t.gameObject);
                                                }
                                                else
                                                {
                                                    if (subtractfrom != null && t.gameObject != null)
                                                    {
                                                        Debug.Log("Deforming: " + subtractfrom.name + " with: " + t.gameObject.name);
                                                        result = CSG.Subtract(subtractfrom, t.gameObject);
                                                    }
                                                }
                                                Mesh mesh = result.mesh;
                                                var composite = new GameObject();
                                                composite.name = "Floor";
                                                composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                                                //Material[] mats = new Material[] { result.materials.ToArray()[0], mat };
                                                composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                                                if (!Interior)
                                                {
                                                    composite.AddComponent<BoxCollider>();
                                                }
                                                else
                                                {
                                                    MeshCollider col = composite.AddComponent<MeshCollider>();
                                                    col.sharedMesh = result.mesh;
                                                    col.convex = false;
                                                }
                                                DestroyImmediate(subtractfrom);
                                                subtractfrom = composite;
                                            }
                                            catch
                                            {
                                                Debug.Log("Couldn't deform the building");
                                                if (numberoftries < 5)
                                                {
                                                    i2 -= 1;
                                                    numberoftries += 1;
                                                }
                                            }
                                        }
                                    }
                                    if (t.parent.transform.name == "Balcony")
                                    {
                                        if (t.parent.transform.parent.transform == Building)
                                        {
                                            int numberoftries = 0;
                                            for (int i2 = 0; i2 < 1; i2++)
                                            {
                                                try
                                                {
                                                    Model result = null;
                                                    if (subtractfrom == null)
                                                    {
                                                        result = CSG.Subtract(part.Model.gameObject, t.gameObject);
                                                    }
                                                    else
                                                    {
                                                        if (subtractfrom != null && t.gameObject != null)
                                                        {
                                                            Debug.Log("Deforming: " + subtractfrom.name + " with: " + t.gameObject.name);
                                                            result = CSG.Subtract(subtractfrom, t.gameObject);
                                                        }
                                                    }
                                                    Mesh mesh = result.mesh;
                                                    var composite = new GameObject();
                                                    composite.name = "Floor";
                                                    composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                                                    //Material[] mats = new Material[] { result.materials.ToArray()[0], mat };
                                                    composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                                                    if (!Interior)
                                                    {
                                                        composite.AddComponent<BoxCollider>();
                                                    }
                                                    else
                                                    {
                                                        MeshCollider col = composite.AddComponent<MeshCollider>();
                                                        col.sharedMesh = result.mesh;
                                                        col.convex = false;
                                                    }
                                                    DestroyImmediate(subtractfrom);
                                                    subtractfrom = composite;
                                                }
                                                catch
                                                {
                                                    Debug.Log("Couldn't deform the building");
                                                    if (numberoftries < 5)
                                                    {
                                                        i2 -= 1;
                                                        numberoftries += 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        yield return new WaitForSecondsRealtime(0);
                    }
                }
                if (subtractfrom != null)
                {
                    subtractfrom.transform.parent = Building;
                    foreach (Transform t in Partstomove)
                    {
                        if (t != null)
                        {
                            t.parent = subtractfrom.transform;
                        }
                    }
                    fl F = new fl()
                    {
                        Model = subtractfrom.transform,
                        Position = pos,
                        Scale = scale,
                        Deform = part.Deform,
                    };
                    fls.Add(F);
                    if (part.Model != null)
                    {
                        DestroyImmediate(part.Model.gameObject);
                    }
                }
                foreach (Extenction x in part.Extenctions)
                {
                    subtractfrom = null;
                    scale = x.Model.lossyScale;
                    pos = x.Model.position;
                    Transform parent = x.Model.parent.transform;
                    Partsinside = x.Model.transform.GetComponentsInChildren<Transform>().ToList();
                    Partstomove.Clear();
                    foreach (var t in Partsinside)
                    {
                        if (t.transform.parent == x.Model.transform)
                        {
                            Partstomove.Add(t);
                        }
                    }
                    mat = x.Model.GetComponent<Renderer>().sharedMaterial;
                    if (Interior && x.Deform)
                    {
                        Transform obj = null;
                        InteriorsDictionnary.TryGetValue(x.Model.transform, out obj);
                        Model result1 = CSG.Subtract(x.Model.gameObject, obj.gameObject);
                        Mesh mesh = result1.mesh;
                        var composite = new GameObject();
                        composite.name = "Floor";
                        composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                        composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                        composite.AddComponent<MeshCollider>();
                        composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                        subtractfrom = composite;
                        if (subtractfrom != null)
                        {
                            subtractfrom.transform.parent = x.Model.parent.transform;
                            foreach (Transform t in Partsinside)
                            {
                                if (t.parent.transform == x.Model)
                                {
                                    t.parent = subtractfrom.transform;
                                }
                            }
                            DestroyImmediate(x.Model.gameObject);
                            Collider[] cols = Physics.OverlapBox(pos, scale * 0.5f, Quaternion.identity);
                            foreach (Collider col in cols)
                            {
                                Transform t = col.transform;
                                if (t.name == "ColumnModified" || t.name == "Column")
                                {
                                    Model m = CSG.Subtract(t.gameObject, obj.gameObject);
                                    var colm = new GameObject();
                                    colm.name = t.name + "Modified2";
                                    colm.AddComponent<MeshFilter>().sharedMesh = m.mesh;
                                    colm.AddComponent<MeshRenderer>().sharedMaterials = m.materials.ToArray();
                                    colm.AddComponent<MeshCollider>();
                                    colm.GetComponent<MeshCollider>().sharedMesh = m.mesh;
                                    colm.transform.parent = subtractfrom.transform;
                                    Partstomove.Add(colm.transform);
                                    DestroyImmediate(t.gameObject);
                                }
                                if (t != null)
                                {
                                    if (t.name == "Bordure Between Floors")
                                    {
                                        Model m = CSG.Subtract(t.gameObject, obj.gameObject);
                                        var colm = new GameObject();
                                        colm.name = t.name;
                                        colm.AddComponent<MeshFilter>().sharedMesh = m.mesh;
                                        colm.AddComponent<MeshRenderer>().sharedMaterials = m.materials.ToArray();
                                        colm.AddComponent<MeshCollider>();
                                        colm.GetComponent<MeshCollider>().sharedMesh = m.mesh;
                                        colm.transform.parent = subtractfrom.transform;
                                        Partstomove.Add(colm.transform);
                                        DestroyImmediate(t.gameObject);
                                    }
                                }
                            }
                        }
                        DestroyImmediate(obj.gameObject);
                    }
                    yield return new WaitForSecondsRealtime(1f);
                    if (x.Deform)
                    {
                        foreach (Transform t in Partstomove)
                        {
                            if (t != null)
                            {
                                t.parent = Building;
                            }
                        }
                        foreach (Transform t in Partsinside)
                        {
                            if (t != null)
                            {
                                if (t.name != "Floor" && t.name != "Extenction" && t.name != "Wall" && t.name != "FloorModified" && t.name != "WallModified" && t.GetComponent<Collider>() && t != x.Model && t.GetComponent<Renderer>() && t.GetComponent<MeshFilter>())
                                {
                                    if (t.GetComponent<Renderer>().sharedMaterial != null)
                                    {
                                        if (t.parent.transform == Building)
                                        {
                                            int numberoftries = 0;
                                            for (int i2 = 0; i2 < 1; i2++)
                                            {
                                                try
                                                {
                                                    Model result = null;
                                                    if (subtractfrom == null)
                                                    {
                                                        result = CSG.Subtract(x.Model.gameObject, t.gameObject);
                                                    }
                                                    else
                                                    {
                                                        if (subtractfrom != null && t.gameObject != null)
                                                        {
                                                            Debug.Log("Deforming: " + subtractfrom.name + " with: " + t.gameObject.name);

                                                            result = CSG.Subtract(subtractfrom, t.gameObject);
                                                        }
                                                    }
                                                    Mesh mesh = result.mesh;
                                                    var composite = new GameObject();
                                                    composite.name = "Floor";
                                                    composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
                                                    Material[] mats = new Material[] { result.materials.ToArray()[0], mat };
                                                    composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
                                                    if (!Interior)
                                                    {
                                                        composite.AddComponent<BoxCollider>();
                                                    }
                                                    else
                                                    {
                                                        MeshCollider col = composite.AddComponent<MeshCollider>();
                                                        col.sharedMesh = result.mesh;
                                                        col.convex = false;
                                                    }
                                                    DestroyImmediate(subtractfrom);
                                                    subtractfrom = composite;
                                                }
                                                catch
                                                {
                                                    Debug.Log("Couldn't deform the building");
                                                    if (numberoftries < 5)
                                                    {
                                                        i2 -= 1;
                                                        numberoftries += 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            yield return new WaitForSecondsRealtime(0);
                        }
                    }
                    if (subtractfrom != null)
                    {
                        subtractfrom.transform.parent = parent;
                        foreach (Transform t in Partstomove)
                        {
                            if (t != null)
                            {
                                t.parent = subtractfrom.transform;
                            }
                        }
                        fl F = new fl()
                        {
                            Model = subtractfrom.transform,
                            Position = pos,
                            Scale = scale,
                            Deform = x.Deform,
                        };
                        fls.Add(F);
                        if (x.Model != null)
                        {
                            DestroyImmediate(x.Model.gameObject);
                        }
                    }
                }
            }
            Debug.Log("Building deformed!");
            Transform[] ts2 = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts2)
            {
                t.gameObject.isStatic = true;
            }
            if (Interior)
            {
                StartGeneratorCoroutine(CreateRooms());
            }
            Console.Clear();
        }
        IEnumerator CreateRooms()
        {
            FloorLevels.Clear();
            FloorLevelsDone = false;
            for (int i = 0; i < fls.Count; i++)
            {
                fl fl = fls[i];
                if (fl.Model != null && fl.Deform)
                {
                    List<Transform> Rooms = new List<Transform>();
                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.name = "Room";
                    block.transform.position = fl.Position;
                    block.transform.localScale = fl.Scale * 0.99f;
                    block.transform.parent = fl.Model;
                    block.layer = 6;
                    block.GetComponent<Renderer>().enabled = false;
                    DestroyImmediate(block.GetComponent<Collider>());
                    Rooms.Add(block.transform);
                    if (fl.Model != null)
                    {
                        StartGeneratorCoroutine(SplitRooms(Rooms, fl.Model, fl.Position, fl.Scale));
                    }
                }
            }
            yield return new WaitForSecondsRealtime(5);
            StartGeneratorCoroutine(CreateStairWayAndShaft());
        }
        float CalculateStairWayAndElevatorShaftHeight(bool InMiddle)
        {
            float height = 0;
            if (Interior)
            {
                Floor PreviousFloor = null;
                foreach (Floor fl in Floors)
                {
                    if (fl.Deform)
                    {
                        if (PreviousFloor == null)
                        {
                            height += fl.height;
                            PreviousFloor = fl;
                        }
                        else
                        {
                            if (InMiddle)
                            {
                                height += fl.height;
                                PreviousFloor = fl;
                            }
                            else
                            {
                                if (PreviousFloor.width == fl.width && PreviousFloor.lenght == fl.lenght)
                                {
                                    height += fl.height;
                                    PreviousFloor = fl;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return height;
        }
        Vector3 FindFirstFloorScale()
        {
            Vector3 Scale = Vector3.zero;
            foreach (Floor fl in Floors)
            {
                if (fl.width > 0 && fl.height > 0 && fl.lenght > 0)
                {
                    Scale = new Vector3(fl.width, fl.height, fl.lenght);
                    break;
                }
            }
            return Scale;
        }
        Vector3 FindElevatorPosition(Vector3 stairspos)
        {
            Vector3 pos = Vector3.zero;
            float height = CalculateStairWayAndElevatorShaftHeight(true);
            Vector3 FirstFloorScale = FindFirstFloorScale();
            for (int i = 0; i < 1; i++)
            {
                pos = FirstFloorPosition + new Vector3(UnityEngine.Random.Range(-FirstFloorScale.x * 0.5f + Elevator.transform.lossyScale.x * 0.5f, FirstFloorScale.x * 0.5f - Elevator.transform.lossyScale.x * 0.5f), height * 0.5f, UnityEngine.Random.Range(-FirstFloorScale.z * 0.5f + Elevator.transform.lossyScale.z * 0.5f, FirstFloorScale.z * 0.5f - Elevator.transform.lossyScale.z * 0.5f));
                if (Vector3.Distance(pos, stairspos) > ((StairWay.transform.lossyScale.x * StairWay.transform.lossyScale.z) + (Elevator.transform.lossyScale.x * Elevator.transform.lossyScale.z)) * 0.5f)
                {
                    i = -1;
                }
                Collider[] cols = Physics.OverlapBox(pos, Elevator.transform.lossyScale * 0.5f, Elevator.transform.rotation);
                foreach (Collider col in cols)
                {
                    if (col.gameObject.layer == 8)
                    {
                        i = -1;
                    }
                    if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform.name == "Door")
                    {
                        if (col.gameObject.layer != 8)
                        {
                            i -= 1;
                        }
                    }
                }
            }
            return pos;
        }
        IEnumerator CreateStairWayAndShaft()
        {
            yield return new WaitForSecondsRealtime(10f);
            bool Middle = false;
            GameObject block = null;
            Vector3 FirstFloorScale = FindFirstFloorScale();
            //Create Stairway-----------------------------------------------------------------
            float numberoffloors = CalculateNumberOfFloors();
            Vector3 blockscale = Vector3.zero;
            if (StairWay != null && numberoffloors != 0)
            {
                Debug.Log("STAIRWAY!!!!");
                //Center
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    Middle = true;
                    float height = CalculateStairWayAndElevatorShaftHeight(true) - 0.01f;
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = "StairWay";
                    block.transform.position = FirstFloorPosition + new Vector3(0, height * 0.5f, 0);
                    block.transform.localScale = new Vector3(StairWay.transform.lossyScale.x, height, StairWay.transform.lossyScale.z);
                    blockscale = block.transform.lossyScale;
                    block.transform.parent = Building;
                    block.layer = 6;
                    block.GetComponent<Renderer>().enabled = false;
                }
                //Walls
                else
                {
                    Middle = false;
                    float height = CalculateStairWayAndElevatorShaftHeight(true) - 0.01f;
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = "StairWay";
                    block.transform.localScale = new Vector3(StairWay.transform.lossyScale.x, height, StairWay.transform.lossyScale.z);
                    blockscale = block.transform.lossyScale;
                    int side = UnityEngine.Random.Range(0, 4);
                    if (side == 0)
                    {
                        bool touchesdoors = false;
                        Vector3 blockpos = FirstFloorPosition + new Vector3(UnityEngine.Random.Range(-FirstFloorScale.x * 0.5f + block.transform.lossyScale.x * 0.5f, FirstFloorScale.x * 0.5f - block.transform.lossyScale.x * 0.5f), height * 0.5f, FirstFloorScale.z * 0.5f - block.transform.lossyScale.z * 0.5f);
                        Collider[] cols = Physics.OverlapBox(blockpos, block.transform.lossyScale * 0.5f, block.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col.gameObject.layer == 7)
                            {
                                Debug.Log("COLLIDED: " + col.transform.name, col.transform);
                                touchesdoors = true;
                            }
                        }
                        if (!touchesdoors)
                        {
                            block.transform.position = blockpos;
                        }
                        else
                        {
                            Middle = true;
                            block.transform.position = FirstFloorPosition + new Vector3(0, height * 0.5f, 0);
                        }
                    }
                    if (side == 1)
                    {
                        bool touchesdoors = false;
                        Vector3 blockpos = FirstFloorPosition + new Vector3(UnityEngine.Random.Range(-FirstFloorScale.x * 0.5f + block.transform.lossyScale.x * 0.5f, FirstFloorScale.x * 0.5f - block.transform.lossyScale.x * 0.5f), height * 0.5f, -FirstFloorScale.z * 0.5f + block.transform.lossyScale.z * 0.5f);
                        Collider[] cols = Physics.OverlapBox(blockpos, block.transform.lossyScale * 0.5f, block.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col.gameObject.layer == 7)
                            {
                                Debug.Log("COLLIDED: " + col.transform.name, col.transform);
                                touchesdoors = true;
                            }
                        }
                        if (!touchesdoors)
                        {
                            block.transform.position = blockpos;
                        }
                        else
                        {
                            Middle = true;
                            block.transform.position = FirstFloorPosition + new Vector3(0, height * 0.5f, 0);
                        }
                    }
                    if (side == 2)
                    {
                        bool touchesdoors = false;
                        Vector3 blockpos = FirstFloorPosition + new Vector3(FirstFloorScale.x * 0.5f - block.transform.lossyScale.x * 0.5f, height * 0.5f, UnityEngine.Random.Range(-FirstFloorScale.z * 0.5f + block.transform.lossyScale.z * 0.5f, FirstFloorScale.z * 0.5f - block.transform.lossyScale.z * 0.5f));
                        Collider[] cols = Physics.OverlapBox(blockpos, block.transform.lossyScale * 0.5f, block.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col.gameObject.layer == 7)
                            {
                                Debug.Log("COLLIDED: " + col.transform.name, col.transform);
                                touchesdoors = true;
                            }
                        }
                        if (!touchesdoors)
                        {
                            block.transform.position = blockpos;
                        }
                        else
                        {
                            Middle = true;
                            block.transform.position = FirstFloorPosition + new Vector3(0, height * 0.5f, 0);
                        }
                    }
                    if (side == 3)
                    {
                        bool touchesdoors = false;
                        Vector3 blockpos = FirstFloorPosition + new Vector3(-FirstFloorScale.x * 0.5f + block.transform.lossyScale.x * 0.5f, height * 0.5f, UnityEngine.Random.Range(-FirstFloorScale.z * 0.5f + block.transform.lossyScale.z * 0.5f, FirstFloorScale.z * 0.5f - block.transform.lossyScale.z * 0.5f));
                        Collider[] cols = Physics.OverlapBox(blockpos, block.transform.lossyScale * 0.5f, block.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col.gameObject.layer == 7)
                            {
                                Debug.Log("COLLIDED: " + col.transform.name, col.transform);
                                touchesdoors = true;
                            }
                        }
                        if (!touchesdoors)
                        {
                            block.transform.position = blockpos;
                        }
                        else
                        {
                            Middle = true;
                            block.transform.position = FirstFloorPosition + new Vector3(0, height * 0.5f, 0);
                        }
                    }
                    block.transform.parent = Building;
                    block.layer = 6;
                    block.GetComponent<Renderer>().enabled = false;
                }
            }
            //Create Holes--------------------------------------------------------------------
            yield return new WaitForSecondsRealtime(5f);
            Collider[] cols1 = new Collider[0];
            if (block != null)
            {
                cols1 = Physics.OverlapBox(block.transform.position, block.transform.lossyScale * 0.5f, block.transform.rotation);
            }
            List<Transform> alreadydone = new List<Transform>();
            foreach (Collider col in cols1)
            {
                if (col != null)
                {
                    if (col.transform.name == "RoomFloor" || col.transform.name == "Ceiling" || col.transform.name == "Floor")
                    {
                        if (!alreadydone.Contains(col.transform) && Vector3.Distance(new Vector3(0, block.transform.position.y + block.transform.lossyScale.y * 0.5f, 0), new Vector3(0, col.transform.position.y - col.transform.lossyScale.y * 0.5f, 0)) > 0.5f && Vector3.Distance(new Vector3(0, block.transform.position.y - block.transform.lossyScale.y * 0.5f, 0), new Vector3(0, col.transform.position.y + col.transform.lossyScale.y * 0.5f, 0)) > 0.5f)
                        {
                            try
                            {
                                if (col != null)
                                {
                                    if (col.transform.name == "Floor")
                                    {
                                        block.transform.localScale = new Vector3(blockscale.x - 0.1f, blockscale.y, blockscale.z - 0.1f);
                                    }
                                }
                                Model result1 = CSG.Subtract(col.transform.gameObject, block);
                                Mesh mesh = result1.mesh;
                                var composite = new GameObject();
                                composite.name = col.transform.name + "Modified";
                                composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                                composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                                composite.AddComponent<MeshCollider>();
                                composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                                composite.transform.parent = col.transform.parent.transform;
                                if (col.transform.name == "Floor")
                                {
                                    block.transform.localScale = blockscale;
                                    alreadydone.Add(composite.transform);
                                    composite.name = "Floor";
                                    Transform[] ts = col.transform.GetComponentsInChildren<Transform>();
                                    foreach (Transform t in ts)
                                    {
                                        if (t.parent.transform == col.transform)
                                        {
                                            t.parent = composite.transform;
                                        }
                                    }
                                }
                                DestroyImmediate(col.transform.gameObject);
                            }
                            catch
                            {
                                Debug.Log("Couldn't create the hole");
                            }
                        }
                    }
                    if (col != null)
                    {
                        if (col.transform.name == "CeilingLights" || col.transform.name == "Wall" || col.transform.name == "WallModified")
                        {
                            DestroyImmediate(col.transform.gameObject);
                        }
                    }
                }
            }
            if (block != null)
            {
                if (Elevator == null || numberoffloors < 5)
                {
                    DestroyImmediate(block.GetComponent<Collider>());
                }
            }
            //Create Walls--------------------------------------------------------------------
            Quaternion chosenwallrotation = Quaternion.identity;
            if (StairWayWalls)
            {
                List<Transform> Walls = new List<Transform>();
                //Front Wall----------------------------------------------------------------------
                Vector3 pos = block.transform.position + new Vector3(0, 0, block.transform.lossyScale.z * 0.5f);
                Vector3 pos2 = Building.position + new Vector3(0, 0, FirstFloorScale.z * 0.5f);
                if (pos2.z - pos.z > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = block.transform.position + new Vector3(0, 0, block.transform.lossyScale.z * 0.5f - 0.01f);
                    wall.transform.localScale = new Vector3(block.transform.lossyScale.x - 0.01f, block.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 0, 0);
                    wall.transform.parent = block.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    Walls.Add(wall.transform);
                }
                //Back Wall-----------------------------------------------------------------------
                pos = block.transform.position - new Vector3(0, 0, block.transform.lossyScale.z * 0.5f);
                pos2 = Building.position - new Vector3(0, 0, FirstFloorScale.z * 0.5f);
                if (pos.z - pos2.z > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = block.transform.position - new Vector3(0, 0, block.transform.lossyScale.z * 0.5f - 0.01f);
                    wall.transform.localScale = new Vector3(block.transform.lossyScale.x - 0.01f, block.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 180, 0);
                    wall.transform.parent = block.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    Walls.Add(wall.transform);
                }
                //Right Wall----------------------------------------------------------------------
                pos = block.transform.position + new Vector3(block.transform.lossyScale.x * 0.5f, 0, 0);
                pos2 = Building.position + new Vector3(FirstFloorScale.x * 0.5f, 0, 0);
                if (pos2.x - pos.x > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = block.transform.position + new Vector3(block.transform.lossyScale.x * 0.5f - 0.01f, 0, 0);
                    wall.transform.localScale = new Vector3(block.transform.lossyScale.z - 0.01f, block.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 90, 0);
                    wall.transform.parent = block.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    Walls.Add(wall.transform);
                }
                //Left Wall-----------------------------------------------------------------------
                pos = block.transform.position - new Vector3(block.transform.lossyScale.x * 0.5f, 0, 0);
                pos2 = Building.position - new Vector3(FirstFloorScale.x * 0.5f, 0, 0);
                if (pos.x - pos2.x > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = block.transform.position - new Vector3(block.transform.lossyScale.x * 0.5f - 0.01f, 0, 0);
                    wall.transform.localScale = new Vector3(block.transform.lossyScale.z - 0.01f, block.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, -90, 0);
                    wall.transform.parent = block.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    Walls.Add(wall.transform);
                }
                //Floor---------------------------------------------------------------------------
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "RoomFloor";
                floor.transform.position = block.transform.position - new Vector3(0, block.transform.lossyScale.y * 0.5f - 0.05f, 0);
                floor.transform.localScale = new Vector3(block.transform.lossyScale.x - 0.01f, 0.1f, block.transform.lossyScale.z - 0.01f);
                floor.transform.parent = block.transform;
                floor.layer = 6;
                floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial;
                //Ceiling-------------------------------------------------------------------------
                GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.name = "Ceiling";
                ceiling.transform.position = block.transform.position + new Vector3(0, block.transform.lossyScale.y * 0.5f - 0.1f, 0);
                ceiling.transform.localScale = new Vector3(block.transform.lossyScale.x - 0.01f, 0.1f, block.transform.lossyScale.z - 0.01f);
                ceiling.transform.parent = block.transform;
                ceiling.layer = 6;
                ceiling.GetComponent<Renderer>().sharedMaterial = CeilingMaterial;
                //Create Entrances-----------------------------------------------------------------
                Transform chosenwall = Walls[UnityEngine.Random.Range(0, Walls.Count)];
                Vector3 chosenwallposition = chosenwall.position;
                chosenwallrotation = chosenwall.rotation;
                for (int i = 0; i < FloorLevels.Count; i++)
                {
                    //Place Door
                    Vector3 doorpos = new Vector3(chosenwallposition.x, FloorLevels[i] + InteriorDoor.transform.lossyScale.y * 0.5f, chosenwallposition.z);
                    GameObject door = Instantiate(InteriorDoor, doorpos, chosenwallrotation);
                    door.layer = 7;
                    door.transform.parent = chosenwall;
                    //Create Hole
                    GameObject objecttosubtract = door.transform.parent.transform.gameObject;
                    Model result1 = CSG.Subtract(door.transform.parent.transform.gameObject, door);
                    Mesh mesh = result1.mesh;
                    var composite = new GameObject();
                    composite.name = door.transform.parent.transform.name + "Modified";
                    composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                    composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                    composite.AddComponent<MeshCollider>();
                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                    composite.transform.parent = door.transform.parent.transform.parent.transform;
                    chosenwall = composite.transform;
                    Transform[] ts = door.transform.parent.transform.GetComponentsInChildren<Transform>();
                    foreach (Transform t in ts)
                    {
                        if (t.parent.transform == door.transform.parent.transform)
                        {
                            t.parent = composite.transform;
                        }
                    }
                    DestroyImmediate(objecttosubtract);
                }
            }
            //Put Stairs----------------------------------------------------------------------
            for (int i = 0; i < FloorLevels.Count - 1; i++)
            {
                //Place Door
                Vector3 stairwaypos = new Vector3(block.transform.position.x, FloorLevels[i] + StairWay.transform.lossyScale.y * 0.5f, block.transform.position.z);
                GameObject stairway = Instantiate(StairWay, stairwaypos, chosenwallrotation);
                stairway.transform.parent = block.transform;
                stairway.layer = 3;
            }
            //Change Layer--------------------------------------------------------------------
            Transform[] ts2 = new Transform[0];
            if (block != null)
            {
                ts2 = block.GetComponentsInChildren<Transform>();
            }
            foreach (Transform t in ts2)
            {
                t.gameObject.layer = 8;
            }
            if (block != null)
            {
                block.layer = 8;
            }
            //Create Elevator-----------------------------------------------------------------
            yield return new WaitForSecondsRealtime(5f);
            if (Elevator != null && numberoffloors > 5)
            {
                Debug.Log("ELEVATOR!!!!");
                float height = CalculateStairWayAndElevatorShaftHeight(true);
                GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shaft.name = "ElevatorShaft";
                shaft.transform.position = FindElevatorPosition(block.transform.position);
                shaft.transform.localScale = new Vector3(Elevator.transform.lossyScale.x, height, Elevator.transform.lossyScale.z);
                shaft.transform.parent = Building;
                shaft.layer = 6;
                shaft.GetComponent<Renderer>().enabled = false;
                List<Transform> Walls = new List<Transform>();
                //Front Wall----------------------------------------------------------------------
                Vector3 pos = shaft.transform.position + new Vector3(0, 0, shaft.transform.lossyScale.z * 0.5f);
                Vector3 pos2 = Building.position + new Vector3(0, 0, FirstFloorScale.z * 0.5f);
                if (pos2.z - pos.z > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = shaft.transform.position + new Vector3(0, 0, shaft.transform.lossyScale.z * 0.5f - 0.01f);
                    wall.transform.localScale = new Vector3(shaft.transform.lossyScale.x - 0.01f, shaft.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 0, 0);
                    wall.transform.parent = shaft.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    if (wall != null)
                    {
                        bool canbeentrance = true;
                        Collider[] cols10 = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f + wall.transform.TransformDirection(new Vector3(0, 0, 2f)), wall.transform.rotation);
                        foreach (Collider col in cols10)
                        {
                            if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform == block.transform)
                            {
                                if (col.transform != wall.transform)
                                {
                                    canbeentrance = false;
                                }
                            }
                        }
                        if (canbeentrance)
                        {
                            Walls.Add(wall.transform);
                        }
                    }
                }
                //Back Wall-----------------------------------------------------------------------
                pos = shaft.transform.position - new Vector3(0, 0, shaft.transform.lossyScale.z * 0.5f);
                pos2 = Building.position - new Vector3(0, 0, FirstFloorScale.z * 0.5f);
                if (pos.z - pos2.z > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = shaft.transform.position - new Vector3(0, 0, shaft.transform.lossyScale.z * 0.5f - 0.01f);
                    wall.transform.localScale = new Vector3(shaft.transform.lossyScale.x - 0.01f, shaft.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 180, 0);
                    wall.transform.parent = shaft.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    if (wall != null)
                    {
                        bool canbeentrance = true;
                        Collider[] cols10 = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f + wall.transform.TransformDirection(new Vector3(0, 0, 2f)), wall.transform.rotation);
                        foreach (Collider col in cols10)
                        {
                            if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform == block.transform)
                            {
                                if (col.transform != wall.transform)
                                {
                                    canbeentrance = false;
                                }
                            }
                        }
                        if (canbeentrance)
                        {
                            Walls.Add(wall.transform);
                        }
                    }
                }
                //Right Wall----------------------------------------------------------------------
                pos = shaft.transform.position + new Vector3(shaft.transform.lossyScale.x * 0.5f, 0, 0);
                pos2 = Building.position + new Vector3(FirstFloorScale.x * 0.5f, 0, 0);
                if (pos2.x - pos.x > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = shaft.transform.position + new Vector3(shaft.transform.lossyScale.x * 0.5f - 0.01f, 0, 0);
                    wall.transform.localScale = new Vector3(shaft.transform.lossyScale.z - 0.01f, shaft.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, 90, 0);
                    wall.transform.parent = shaft.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    if (wall != null)
                    {
                        bool canbeentrance = true;
                        Collider[] cols10 = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f + wall.transform.TransformDirection(new Vector3(0, 0, 2f)), wall.transform.rotation);
                        foreach (Collider col in cols10)
                        {
                            if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform == block.transform)
                            {
                                if (col.transform != wall.transform)
                                {
                                    canbeentrance = false;
                                }
                            }
                        }
                        if (canbeentrance)
                        {
                            Walls.Add(wall.transform);
                        }
                    }
                }
                //Left Wall-----------------------------------------------------------------------
                pos = shaft.transform.position - new Vector3(shaft.transform.lossyScale.x * 0.5f, 0, 0);
                pos2 = Building.position - new Vector3(FirstFloorScale.x * 0.5f, 0, 0);
                if (pos.x - pos2.x > 0.1f || Middle)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Wall";
                    wall.transform.position = shaft.transform.position - new Vector3(shaft.transform.lossyScale.x * 0.5f - 0.01f, 0, 0);
                    wall.transform.localScale = new Vector3(shaft.transform.lossyScale.z - 0.01f, shaft.transform.lossyScale.y - 0.01f, 0.1f);
                    wall.transform.eulerAngles = new Vector3(0, -90, 0);
                    wall.transform.parent = shaft.transform;
                    wall.layer = 6;
                    wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                    Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                    foreach (Collider col in cols)
                    {
                        if (col != null)
                        {
                            if (col.transform.name == "Door")
                            {
                                DestroyImmediate(col.gameObject);
                            }
                        }
                    }
                    if (wall != null)
                    {
                        bool canbeentrance = true;
                        Collider[] cols10 = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f + wall.transform.TransformDirection(new Vector3(0, 0, 2f)), wall.transform.rotation);
                        foreach (Collider col in cols10)
                        {
                            if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform == block.transform)
                            {
                                if (col.transform != wall.transform)
                                {
                                    canbeentrance = false;
                                }
                            }
                        }
                        if (canbeentrance)
                        {
                            Walls.Add(wall.transform);
                        }
                    }
                }
                DestroyImmediate(block.GetComponent<Collider>());
                //Floor---------------------------------------------------------------------------
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "RoomFloor";
                floor.transform.position = shaft.transform.position - new Vector3(0, shaft.transform.lossyScale.y * 0.5f - 0.05f, 0);
                floor.transform.localScale = new Vector3(shaft.transform.lossyScale.x - 0.01f, 0.1f, shaft.transform.lossyScale.z - 0.01f);
                floor.transform.parent = shaft.transform;
                floor.layer = 6;
                floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial;
                //Ceiling-------------------------------------------------------------------------
                GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.name = "Ceiling";
                ceiling.transform.position = shaft.transform.position + new Vector3(0, shaft.transform.lossyScale.y * 0.5f - 0.1f, 0);
                ceiling.transform.localScale = new Vector3(shaft.transform.lossyScale.x - 0.01f, 0.1f, shaft.transform.lossyScale.z - 0.01f);
                ceiling.transform.parent = shaft.transform;
                ceiling.layer = 6;
                ceiling.GetComponent<Renderer>().sharedMaterial = CeilingMaterial;
                //Create Holes--------------------------------------------------------------------
                yield return new WaitForSecondsRealtime(5f);
                Collider[] cols2 = Physics.OverlapBox(shaft.transform.position, shaft.transform.lossyScale * 0.5f, shaft.transform.rotation);
                foreach (Collider col in cols2)
                {
                    if (col != null)
                    {
                        if (col.transform.name == "RoomFloor" || col.transform.name == "RoomFloorModified" || col.transform.name == "Ceiling" || col.transform.name == "CeilingModified")
                        {
                            int numberoftries = 0;
                            for (int i = 0; i < 2; i++)
                            {
                                try
                                {
                                    Model result1 = CSG.Subtract(col.transform.gameObject, shaft);
                                    Mesh mesh = result1.mesh;
                                    var composite = new GameObject();
                                    composite.name = col.transform.name + "Done";
                                    composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                                    composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                                    composite.AddComponent<MeshCollider>();
                                    composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                                    composite.transform.parent = col.transform.parent.transform;
                                    DestroyImmediate(col.transform.gameObject);
                                }
                                catch
                                {
                                    Debug.Log("Couldn't create the hole");
                                    if (numberoftries < 5)
                                    {
                                        i -= 1;
                                        numberoftries += 1;
                                    }
                                }
                            }
                        }
                        if (col != null)
                        {
                            if (col.transform.name == "CeilingLights" || col.transform.name == "Wall" || col.transform.name == "WallModified")
                            {
                                if (col.transform.parent.transform != shaft.transform)
                                {
                                    DestroyImmediate(col.transform.gameObject);
                                }
                            }
                        }
                    }
                    yield return new WaitForSecondsRealtime(1);
                }
                DestroyImmediate(shaft.GetComponent<Collider>());
                //Place Elevator------------------------------------------------------------------
                GameObject el = Instantiate(Elevator, shaft.transform.position - new Vector3(0, shaft.transform.lossyScale.y * 0.5f, 0) + new Vector3(0, Elevator.transform.lossyScale.y * 0.5f, 0), Quaternion.identity);
                el.transform.parent = shaft.transform;
                el.layer = 3;
                //CreateEntrances-----------------------------------------------------------------
                Transform[] ts = el.transform.GetComponentsInChildren<Transform>();
                Transform LeftDoor = null;
                Transform RightDoor = null;
                foreach (Transform t in ts)
                {
                    if (t.name == "LeftDoor")
                    {
                        LeftDoor = t;
                    }
                    if (t.name == "RightDoor")
                    {
                        RightDoor = t;
                    }
                }
                if (el.GetComponent<Builder.Elevator>())
                {
                    el.GetComponent<Builder.Elevator>().LeftDoor = LeftDoor;
                    el.GetComponent<Builder.Elevator>().RightDoor = RightDoor;
                    el.GetComponent<Builder.Elevator>().CurrentChosenFloor = 0;
                }
                else
                {
                    el.AddComponent<Builder.Elevator>();
                    if (el.GetComponent<Builder.Elevator>())
                    {
                        el.GetComponent<Builder.Elevator>().LeftDoor = LeftDoor;
                        el.GetComponent<Builder.Elevator>().RightDoor = RightDoor;
                        el.GetComponent<Builder.Elevator>().CurrentChosenFloor = 0;
                    }
                }
                if (LeftDoor != null && RightDoor != null)
                {
                    Transform chosenwall = Walls[UnityEngine.Random.Range(0, Walls.Count)];
                    chosenwall.parent = null;
                    chosenwall.position = new Vector3(chosenwall.position.x, Building.position.y + (MaximalHeight - 0.2f) * 0.5f, chosenwall.position.z);
                    chosenwall.localScale = new Vector3(chosenwall.lossyScale.x, MaximalHeight - 0.2f, chosenwall.lossyScale.z);
                    chosenwall.parent = shaft.transform;
                    Vector3 chosenwallposition = chosenwall.position;
                    chosenwallrotation = chosenwall.rotation;
                    el.transform.eulerAngles = new Vector3(0, chosenwall.eulerAngles.y, 0);
                    //Place Doors
                    Vector3 leftdoorpos = new Vector3(LeftDoor.position.x, FloorLevels[0] + LeftDoor.transform.lossyScale.y * 0.5f, LeftDoor.position.z) + LeftDoor.TransformDirection(new Vector3(0, 0, LeftDoor.transform.lossyScale.z));
                    Transform leftdoor = Instantiate(LeftDoor, leftdoorpos, chosenwallrotation);
                    leftdoor.gameObject.layer = 7;
                    leftdoor.parent = chosenwall;
                    Vector3 rightdoorpos = new Vector3(RightDoor.position.x, FloorLevels[0] + LeftDoor.transform.lossyScale.y * 0.5f, RightDoor.position.z) + RightDoor.TransformDirection(new Vector3(0, 0, RightDoor.transform.lossyScale.z));
                    Transform rightdoor = Instantiate(RightDoor, rightdoorpos, chosenwallrotation);
                    rightdoor.gameObject.layer = 7;
                    rightdoor.parent = chosenwall;
                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = "Box";
                    Vector3 togetherpos = (leftdoorpos + rightdoorpos) * 0.5f;
                    Vector3 togetherscale = new Vector3(leftdoor.lossyScale.x + rightdoor.lossyScale.x, leftdoor.lossyScale.y, rightdoor.lossyScale.z);
                    Vector3 wallsurfacepos = togetherpos + rightdoor.TransformDirection(new Vector3(0, 0, chosenwall.lossyScale.z * 0.5f)) + new Vector3(0, 0, rightdoor.InverseTransformPoint(chosenwallposition).z * 0.5f);
                    box.transform.localScale = new Vector3(togetherscale.x, togetherscale.y, Vector3.Distance(wallsurfacepos, rightdoor.position + rightdoor.TransformDirection(new Vector3(0, 0, rightdoor.lossyScale.z * 0.5f))) / 16f + 0.2f);
                    box.transform.position = new Vector3((togetherpos.x + chosenwallposition.x) * 0.5f, togetherpos.y, (togetherpos.z + chosenwallposition.z) * 0.5f);
                    box.transform.rotation = chosenwallrotation;
                    int numberoftries = 0;
                    for (int i2 = 0; i2 < 1; i2++)
                    {
                        try
                        {
                            Model result1 = CSG.Subtract(chosenwall.transform.gameObject, box);
                            Mesh mesh = result1.mesh;
                            var composite = new GameObject();
                            composite.name = chosenwall.transform.name;
                            composite.AddComponent<MeshFilter>().sharedMesh = result1.mesh;
                            composite.AddComponent<MeshRenderer>().sharedMaterials = result1.materials.ToArray();
                            composite.AddComponent<MeshCollider>();
                            composite.GetComponent<MeshCollider>().sharedMesh = mesh;
                            composite.transform.parent = chosenwall.transform.parent.transform;
                            Transform[] ts5 = chosenwall.GetComponentsInChildren<Transform>();
                            foreach (Transform t in ts5)
                            {
                                if (t.parent.transform == chosenwall)
                                {
                                    t.parent = composite.transform;
                                }
                            }
                            box.SetActive(false);
                            chosenwall.gameObject.SetActive(false);
                            RaycastHit hit;
                            if (!Physics.Linecast(leftdoorpos, leftdoorpos + leftdoor.TransformDirection(new Vector3(0, 0, 2)), out hit))
                            {
                                box.SetActive(true);
                                chosenwall.gameObject.SetActive(true);
                                DestroyImmediate(chosenwall.transform.gameObject);
                                chosenwall = composite.transform;
                            }
                            else if (numberoftries < 5)
                            {
                                box.SetActive(true);
                                chosenwall.gameObject.SetActive(true);
                                DestroyImmediate(composite);
                                i2 -= 1;
                                numberoftries += 1;
                            }
                        }
                        catch
                        {
                            Debug.Log("Couldn't create the hole");
                            if (numberoftries < 5)
                            {
                                i2 -= 1;
                                numberoftries += 1;
                            }
                        }
                        yield return new WaitForSecondsRealtime(5);
                    }
                    GameObject objtodestroy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    objtodestroy.transform.position = box.transform.position;
                    objtodestroy.transform.rotation = chosenwallrotation;
                    objtodestroy.transform.localScale = new Vector3(box.transform.lossyScale.x * 0.9f, box.transform.lossyScale.y * 0.9f, box.transform.lossyScale.z * 10);
                    yield return new WaitForSecondsRealtime(1);
                    int numberoftries2 = 0;
                    for (int i2 = 0; i2 < 1; i2++)
                    {
                        try
                        {
                            Model result2 = CSG.Subtract(box, objtodestroy);
                            Mesh mesh2 = result2.mesh;
                            var composite2 = new GameObject();
                            composite2.name = "Entrance";
                            composite2.AddComponent<MeshFilter>().sharedMesh = result2.mesh;
                            composite2.AddComponent<MeshRenderer>().sharedMaterials = result2.materials.ToArray();
                            composite2.AddComponent<MeshCollider>();
                            composite2.GetComponent<MeshCollider>().sharedMesh = mesh2;
                            composite2.transform.parent = chosenwall;
                        }
                        catch
                        {
                            Debug.Log("Couldn't create the entrance");
                            if (numberoftries2 < 5)
                            {
                                i2 -= 1;
                                numberoftries2 += 1;
                            }
                        }
                    }
                    DestroyImmediate(box);
                    DestroyImmediate(objtodestroy);
                    if (el.GetComponent<Builder.Elevator>())
                    {
                        Builder.Elevator.Floor fl = new Builder.Elevator.Floor()
                        {
                            Height = FloorLevels[0],
                            LeftDoor = leftdoor,
                            RightDoor = rightdoor,
                        };
                        el.GetComponent<Builder.Elevator>().Floors.Add(fl);
                    }
                    yield return new WaitForSecondsRealtime(5);
                    for (int i = 1; i < FloorLevels.Count; i++)
                    {
                        Transform t = Instantiate(chosenwall, new Vector3(0, FloorLevels[i], 0), Quaternion.identity);
                        t.position = new Vector3(0, FloorLevels[i], 0);
                        t.localScale = Vector3.one;
                        t.parent = shaft.transform;
                        if (el.GetComponent<Builder.Elevator>())
                        {
                            Builder.Elevator.Floor fl = new Builder.Elevator.Floor()
                            {
                                Height = FloorLevels[i],
                                LeftDoor = t.GetChild(0),
                                RightDoor = t.GetChild(1),
                            };
                            el.GetComponent<Builder.Elevator>().Floors.Add(fl);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Couldn't find all doors, please name the elevator's doors LeftDoor and RightDoor");
                }
            }
        }
        IEnumerator SplitRooms(List<Transform> Rooms, Transform Parent, Vector3 Position, Vector3 Scale)
        {
            //Split rooms
            for (int i = 0; i < Rooms.Count; i++)
            {
                FloorLevelsDone = false;
                Debug.Log(i);
                Transform Room = Rooms[i];
                int numberofsplits = UnityEngine.Random.Range(2, 5);
                //Height
                if (Room.lossyScale.y > MaximalHeight)
                {
                    int nsplits = Mathf.CeilToInt(Room.lossyScale.y / MaximalHeight);
                    Debug.Log("Big enough");
                    Rooms.Remove(Room);
                    for (int j = 0; j < nsplits; j++)
                    {
                        Debug.Log("Split: " + j);
                        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        block.name = "Room";
                        block.transform.position = Room.position - new Vector3(0, Room.lossyScale.y * 0.5f, 0) + new Vector3(0, (Room.lossyScale.y / nsplits) * 0.5f + (Room.lossyScale.y / nsplits) * j, 0);
                        block.transform.localScale = new Vector3(Room.lossyScale.x, Room.lossyScale.y / nsplits, Room.lossyScale.z);
                        block.transform.parent = Parent;
                        block.layer = 6;
                        block.GetComponent<Renderer>().enabled = false;
                        DestroyImmediate(block.GetComponent<Collider>());
                        Rooms.Add(block.transform);
                        if (!FloorLevelsDone)
                        {
                            FloorLevels.Add(block.transform.position.y - block.transform.lossyScale.y * 0.5f);
                        }
                        Debug.Log("ADDED!");
                    }
                    FloorLevelsDone = true;
                    DestroyImmediate(Room.gameObject);
                    i = -1;
                }
                if (Room != null)
                {
                    //Vertical
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        Debug.Log("Vertical");
                        if ((Room.lossyScale.x / numberofsplits) > MinimalScale)
                        {
                            Debug.Log("Big enough");
                            Rooms.Remove(Room);
                            for (int j = 0; j < numberofsplits; j++)
                            {
                                Debug.Log("Split: " + j);
                                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                block.name = "Room";
                                block.transform.position = Room.position - new Vector3(Room.lossyScale.x * 0.5f, 0, 0) + new Vector3((Room.lossyScale.x / numberofsplits) * 0.5f + (Room.lossyScale.x / numberofsplits) * j, 0, 0);
                                block.transform.localScale = new Vector3(Room.lossyScale.x / numberofsplits, Room.lossyScale.y, Room.lossyScale.z);
                                block.transform.parent = Parent;
                                block.layer = 6;
                                block.GetComponent<Renderer>().enabled = false;
                                DestroyImmediate(block.GetComponent<Collider>());
                                Rooms.Add(block.transform);
                            }
                            DestroyImmediate(Room.gameObject);
                            i = -1;
                        }
                        else
                        {
                            Debug.Log("Horizontal");
                            if ((Room.lossyScale.z / numberofsplits) > MinimalScale)
                            {
                                Debug.Log("Big enough");
                                Rooms.Remove(Room);
                                for (int j = 0; j < numberofsplits; j++)
                                {
                                    Debug.Log("Split: " + j);
                                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    block.name = "Room";
                                    block.transform.position = Room.position - new Vector3(0, 0, Room.lossyScale.z * 0.5f) + new Vector3(0, 0, (Room.lossyScale.z / numberofsplits) * 0.5f + (Room.lossyScale.z / numberofsplits) * j);
                                    block.transform.localScale = new Vector3(Room.lossyScale.x, Room.lossyScale.y, Room.lossyScale.z / numberofsplits);
                                    block.transform.parent = Parent;
                                    block.layer = 6;
                                    block.GetComponent<Renderer>().enabled = false;
                                    DestroyImmediate(block.GetComponent<Collider>());
                                    Rooms.Add(block.transform);
                                }
                                DestroyImmediate(Room.gameObject);
                                i = -1;
                            }
                        }
                    }
                    //Horizontal
                    else
                    {
                        Debug.Log("Horizontal");
                        if ((Room.lossyScale.z / numberofsplits) > MinimalScale)
                        {
                            Debug.Log("Big enough");
                            Rooms.Remove(Room);
                            for (int j = 0; j < numberofsplits; j++)
                            {
                                Debug.Log("Split: " + j);
                                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                block.name = "Room";
                                block.transform.position = Room.position - new Vector3(0, 0, Room.lossyScale.z * 0.5f) + new Vector3(0, 0, (Room.lossyScale.z / numberofsplits) * 0.5f + (Room.lossyScale.z / numberofsplits) * j);
                                block.transform.localScale = new Vector3(Room.lossyScale.x, Room.lossyScale.y, Room.lossyScale.z / numberofsplits);
                                block.transform.parent = Parent;
                                block.layer = 6;
                                block.GetComponent<Renderer>().enabled = false;
                                DestroyImmediate(block.GetComponent<Collider>());
                                Rooms.Add(block.transform);
                            }
                            DestroyImmediate(Room.gameObject);
                            i = -1;
                        }
                        else
                        {
                            Debug.Log("Vertical");
                            if ((Room.lossyScale.x / numberofsplits) > MinimalScale)
                            {
                                Debug.Log("Big enough");
                                Rooms.Remove(Room);
                                for (int j = 0; j < numberofsplits; j++)
                                {
                                    Debug.Log("Split: " + j);
                                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    block.name = "Room";
                                    block.transform.position = Room.position - new Vector3(Room.lossyScale.x * 0.5f, 0, 0) + new Vector3((Room.lossyScale.x / numberofsplits) * 0.5f + (Room.lossyScale.x / numberofsplits) * j, 0, 0);
                                    block.transform.localScale = new Vector3(Room.lossyScale.x / numberofsplits, Room.lossyScale.y, Room.lossyScale.z);
                                    block.transform.parent = Parent;
                                    block.layer = 6;
                                    block.GetComponent<Renderer>().enabled = false;
                                    DestroyImmediate(block.GetComponent<Collider>());
                                    Rooms.Add(block.transform);
                                }
                                DestroyImmediate(Room.gameObject);
                                i = -1;
                            }
                        }
                    }
                }
            }
            Debug.Log("Zones drawn!");
            //Create walls, ceiling and floor
            Transform[] transforms = Parent.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                if (t.gameObject.layer == 6 && t.name == "Room")
                {
                    //Front Wall----------------------------------------------------------------------
                    Vector3 pos = t.position + new Vector3(0, 0, t.lossyScale.z * 0.5f);
                    Vector3 pos2 = Position + new Vector3(0, 0, Scale.z * 0.5f);
                    bool touchesanotherbuilding = false;
                    Collider[] cols1 = Physics.OverlapBox(pos, Vector3.one * 0.5f, Quaternion.identity);
                    foreach (Collider col in cols1)
                    {
                        if (col.transform.name == "Floor" && col.transform != Parent)
                        {
                            touchesanotherbuilding = true;
                        }
                    }
                    if (pos2.z - pos.z > 1 || touchesanotherbuilding)
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "Wall";
                        wall.transform.position = t.position + new Vector3(0, 0, t.lossyScale.z * 0.5f - 0.01f);
                        wall.transform.localScale = new Vector3(t.lossyScale.x, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 0, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                        Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col != null)
                            {
                                if (col.transform.name == "Door")
                                {
                                    DestroyImmediate(wall);
                                }
                            }
                        }
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    else
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "BuildingWall";
                        wall.transform.position = t.position + new Vector3(0, 0, t.lossyScale.z * 0.5f - 0.01f);
                        wall.transform.localScale = new Vector3(t.lossyScale.x, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 180, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().enabled = false;
                        wall.GetComponent<Collider>().enabled = false;
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    //Back Wall-----------------------------------------------------------------------
                    pos = t.position - new Vector3(0, 0, t.lossyScale.z * 0.5f);
                    pos2 = Position - new Vector3(0, 0, Scale.z * 0.5f);
                    touchesanotherbuilding = false;
                    cols1 = Physics.OverlapBox(pos, Vector3.one * 0.5f, Quaternion.identity);
                    foreach (Collider col in cols1)
                    {
                        if (col.transform.name == "Floor" && col.transform != Parent)
                        {
                            touchesanotherbuilding = true;
                        }
                    }
                    if (pos.z - pos2.z > 1 || touchesanotherbuilding)
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "Wall";
                        wall.transform.position = t.position - new Vector3(0, 0, t.lossyScale.z * 0.5f - 0.01f);
                        wall.transform.localScale = new Vector3(t.lossyScale.x, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 180, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                        Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col != null)
                            {
                                if (col.transform.name == "Door")
                                {
                                    DestroyImmediate(wall);
                                }
                            }
                        }
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    else
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "BuildingWall";
                        wall.transform.position = t.position - new Vector3(0, 0, t.lossyScale.z * 0.5f - 0.01f);
                        wall.transform.localScale = new Vector3(t.lossyScale.x, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 0, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().enabled = false;
                        wall.GetComponent<Collider>().enabled = false;
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    //Right Wall----------------------------------------------------------------------
                    pos = t.position + new Vector3(t.lossyScale.x * 0.5f, 0, 0);
                    pos2 = Position + new Vector3(Scale.x * 0.5f, 0, 0);
                    touchesanotherbuilding = false;
                    cols1 = Physics.OverlapBox(pos, Vector3.one * 0.5f, Quaternion.identity);
                    foreach (Collider col in cols1)
                    {
                        if (col.transform.name == "Floor" && col.transform != Parent)
                        {
                            touchesanotherbuilding = true;
                        }
                    }
                    if (pos2.x - pos.x > 1 || touchesanotherbuilding)
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "Wall";
                        wall.transform.position = t.position + new Vector3(t.lossyScale.x * 0.5f - 0.01f, 0, 0);
                        wall.transform.localScale = new Vector3(t.lossyScale.z, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 90, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                        Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col != null)
                            {
                                if (col.transform.name == "Door")
                                {
                                    DestroyImmediate(wall);
                                }
                            }
                        }
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    else
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "BuildingWall";
                        wall.transform.position = t.position + new Vector3(t.lossyScale.x * 0.5f - 0.01f, 0, 0);
                        wall.transform.localScale = new Vector3(t.lossyScale.z, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, -90, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().enabled = false;
                        wall.GetComponent<Collider>().enabled = false;
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    //Left Wall-----------------------------------------------------------------------
                    pos = t.position - new Vector3(t.lossyScale.x * 0.5f, 0, 0);
                    pos2 = Position - new Vector3(Scale.x * 0.5f, 0, 0);
                    touchesanotherbuilding = false;
                    cols1 = Physics.OverlapBox(pos, Vector3.one * 0.5f, Quaternion.identity);
                    foreach (Collider col in cols1)
                    {
                        if (col.transform.name == "Floor" && col.transform != Parent)
                        {
                            touchesanotherbuilding = true;
                        }
                    }
                    if (pos.x - pos2.x > 1 || touchesanotherbuilding)
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "Wall";
                        wall.transform.position = t.position - new Vector3(t.lossyScale.x * 0.5f - 0.01f, 0, 0);
                        wall.transform.localScale = new Vector3(t.lossyScale.z, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, -90, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().sharedMaterial = WallMaterial;
                        Collider[] cols = Physics.OverlapBox(wall.transform.position, wall.transform.lossyScale * 0.5f, wall.transform.rotation);
                        foreach (Collider col in cols)
                        {
                            if (col != null)
                            {
                                if (col.transform.name == "Door")
                                {
                                    DestroyImmediate(wall);
                                }
                            }
                        }
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    else
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = "BuildingWall";
                        wall.transform.position = t.position - new Vector3(t.lossyScale.x * 0.5f - 0.01f, 0, 0);
                        wall.transform.localScale = new Vector3(t.lossyScale.z, t.lossyScale.y, 0.1f);
                        wall.transform.eulerAngles = new Vector3(0, 90, 0);
                        wall.transform.parent = t;
                        wall.layer = 6;
                        wall.GetComponent<Renderer>().enabled = false;
                        wall.GetComponent<Collider>().enabled = false;
                        if (wall != null)
                        {
                            RoomWall rw = new RoomWall()
                            {
                                Wall = wall.transform,
                                Position = wall.transform.position,
                                Scale = wall.transform.lossyScale,
                                Rotation = wall.transform.rotation,
                            };
                            RoomWalls.Add(rw);
                        }
                    }
                    //Floor---------------------------------------------------------------------------
                    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.name = "RoomFloor";
                    floor.transform.position = t.position - new Vector3(0, t.lossyScale.y * 0.5f - 0.05f, 0);
                    floor.transform.localScale = new Vector3(t.lossyScale.x, 0.1f, t.lossyScale.z);
                    floor.transform.parent = t;
                    floor.layer = 6;
                    floor.GetComponent<Renderer>().sharedMaterial = FloorMaterial;
                    //Ceiling-------------------------------------------------------------------------
                    GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ceiling.name = "Ceiling";
                    ceiling.transform.position = t.position + new Vector3(0, t.lossyScale.y * 0.5f - 0.05f, 0);
                    ceiling.transform.localScale = new Vector3(t.lossyScale.x, 0.1f, t.lossyScale.z);
                    ceiling.transform.parent = t;
                    ceiling.layer = 6;
                    ceiling.GetComponent<Renderer>().sharedMaterial = CeilingMaterial;
                }
            }
            //Add doors
            yield return new WaitForSecondsRealtime(1);
            transforms = Parent.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                if (t != null)
                {
                    if (t.name == "Room")
                    {
                        int numberofdoors = UnityEngine.Random.Range(1, 3);
                        for (int i = 0; i < numberofdoors; i++)
                        {
                            Transform floor = null;
                            List<Transform> walls = new List<Transform>();
                            Transform[] ts2 = t.GetComponentsInChildren<Transform>();
                            foreach (Transform t2 in ts2)
                            {
                                if (t2.name == "Wall")
                                {
                                    walls.Add(t2);
                                }
                                if (t2.name == "RoomFloor" || t2.name == "RoomFloorModified" || t2.name == "RoomFloorModifiedDone")
                                {
                                    floor = t2;
                                }
                            }
                            if (walls.Count != 0)
                            {
                                Transform chosenwall = walls[UnityEngine.Random.Range(0, walls.Count)];
                                Vector3 pos = new Vector3(chosenwall.position.x, floor.position.y, chosenwall.position.z) + chosenwall.TransformDirection(new Vector3(UnityEngine.Random.Range(-chosenwall.lossyScale.x * 0.5f + InteriorDoor.transform.lossyScale.x * 0.5f, chosenwall.lossyScale.x * 0.5f - InteriorDoor.transform.lossyScale.x * 0.5f), InteriorDoor.transform.lossyScale.y * 0.5f, 0));
                                bool Thereisanotherroom = false;
                                Debug.Log("ALMSOST");
                                Collider[] cols = Physics.OverlapBox(pos, InteriorDoor.transform.lossyScale * 0.5f, chosenwall.rotation);
                                foreach (Collider col in cols)
                                {
                                    Debug.Log("SEARCHINGINCOLS");
                                    if (col.transform.name == "Wall" && col.transform.parent.transform != t)
                                    {
                                        Debug.Log("ANOTHERROOM");
                                        Thereisanotherroom = true;
                                        pos.x = Mathf.Clamp(pos.x, col.transform.position.x - col.transform.lossyScale.x * 0.5f + InteriorDoor.transform.transform.lossyScale.x * 0.5f, col.transform.position.x + col.transform.lossyScale.x * 0.5f - InteriorDoor.transform.transform.lossyScale.x * 0.5f);
                                    }
                                }
                                if (Thereisanotherroom)
                                {
                                    Debug.Log("DOORCREATED");
                                    GameObject door = Instantiate(InteriorDoor, pos, chosenwall.rotation);
                                    door.name = "Door";
                                    door.layer = 7;
                                    door.transform.parent = chosenwall;
                                    Collider[] cols2 = Physics.OverlapBox(door.transform.position, door.transform.lossyScale * 0.5f, door.transform.rotation);
                                    foreach (Collider col in cols2)
                                    {
                                        if (col.transform != door.transform)
                                        {
                                            if (col.transform.name == "Wall" || col.transform.name == "Floor" || col.transform.name == "FloorModified" || col.transform.name == "Extenction")
                                            {
                                                if (col.transform.parent.transform.name != "Room" && chosenwall.transform.parent.transform.parent.transform != col.transform)
                                                {
                                                    if (col.transform.name == "Floor" || col.transform.name == "FloorModified")
                                                    {
                                                        door.transform.position += door.transform.TransformDirection(new Vector3(0, 0, 0.05f));
                                                    }
                                                }
                                                try
                                                {
                                                    Model m = CSG.Subtract(col.transform.gameObject, door.gameObject);
                                                    var composite = new GameObject();
                                                    int wallsid = 0;
                                                    for (int i2 = 0; i2 < RoomWalls.Count; i2++)
                                                    {
                                                        if (RoomWalls[i2].Wall == col.transform)
                                                        {
                                                            wallsid = i2;
                                                        }
                                                    }
                                                    if (col.transform.name == "Wall")
                                                    {
                                                        composite.name = "WallModified";
                                                    }
                                                    else
                                                    {
                                                        composite.name = "FloorModified";
                                                    }
                                                    composite.AddComponent<MeshFilter>().sharedMesh = m.mesh;
                                                    composite.AddComponent<MeshRenderer>().sharedMaterials = m.materials.ToArray();
                                                    composite.AddComponent<MeshCollider>();
                                                    composite.GetComponent<MeshCollider>().sharedMesh = m.mesh;
                                                    composite.transform.parent = col.transform.parent;
                                                    Transform[] tts = col.transform.GetComponentsInChildren<Transform>();
                                                    foreach (Transform tt in tts)
                                                    {
                                                        if (tt.parent == col.transform)
                                                        {
                                                            tt.parent = composite.transform;
                                                        }
                                                    }
                                                    DestroyImmediate(col.transform.gameObject);
                                                    RoomWalls[wallsid].Wall = composite.transform;
                                                }
                                                catch
                                                {
                                                    print("Couldn't create the door");
                                                }
                                            }
                                        }
                                    }
                                    Collider[] cols5 = Physics.OverlapBox(pos, new Vector3(InteriorDoor.transform.lossyScale.x * 0.4f, 0.1f, InteriorDoor.transform.lossyScale.z * 0.6f), door.transform.rotation);
                                    foreach (Collider col in cols5)
                                    {
                                        if (col != null && col.transform != door.transform)
                                        {
                                            if (col.transform.name == "Wall")
                                            {
                                                DestroyImmediate(col.transform.gameObject);
                                            }
                                            if (col != null)
                                            {
                                                if (col.transform.name == "WallModified")
                                                {
                                                    DestroyImmediate(col.transform.gameObject);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        yield return new WaitForSecondsRealtime(1);
                    }
                }
            }
            //Make everything static
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                t.gameObject.isStatic = true;
            }
            Console.Clear();
            if (PlaceCeilingLights)
            {
                CreateCeiling(Rooms);
                if (PlaceFurniture)
                {
                    yield return new WaitForSecondsRealtime(10);
                    StartGeneratorCoroutine(PutFurniture());
                }
            }
        }
        void CreateCeiling(List<Transform> Rooms)
        {
            foreach (Transform r in Rooms)
            {
                if (r != null)
                {
                    int lampsinx = Mathf.CeilToInt((r.lossyScale.x / CeilingLights.transform.lossyScale.x) * 0.5f);
                    int lampsinz = Mathf.CeilToInt((r.lossyScale.z / CeilingLights.transform.lossyScale.z) * 0.5f);
                    for (int x = 0; x < lampsinx; x++)
                    {
                        for (int z = 0; z < lampsinz; z++)
                        {
                            float posx = 0;
                            float posz = 0;
                            if (lampsinx != 1)
                            {
                                posx = r.position.x - r.lossyScale.x * 0.5f + CeilingLights.transform.lossyScale.x * 0.5f + (CeilingLights.transform.lossyScale.x * x * 2f);
                            }
                            else
                            {
                                posx = r.position.x;
                            }
                            if (lampsinz != 1)
                            {
                                posz = r.position.z - r.lossyScale.z * 0.5f + CeilingLights.transform.lossyScale.z * 0.5f + (CeilingLights.transform.lossyScale.z * z * 2f);
                            }
                            else
                            {
                                posz = r.position.z;
                            }
                            Vector3 pos = new Vector3(posx, (r.position.y + r.lossyScale.y * 0.5f - CeilingLights.transform.lossyScale.y * 0.5f) - 0.1f, posz);
                            bool TouchesWalls = false;
                            Collider[] cols = Physics.OverlapBox(pos, CeilingLights.transform.lossyScale * 0.5f, Quaternion.identity);
                            foreach (Collider col in cols)
                            {
                                if (col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform.name == "StairWay" || col.transform.name == "ElevatorShaft")
                                {
                                    TouchesWalls = true;
                                }
                            }
                            if (!TouchesWalls)
                            {
                                GameObject lamp = Instantiate(CeilingLights, pos, Quaternion.Euler(0, 0, 0));
                                lamp.transform.parent = r;
                                lamp.transform.name = "CeilingLights";
                            }
                        }
                    }
                }
            }
        }
        void DetermineRooms()
        {
            NumberOfSalons = 0;
            NumberOfKitchens = 0;
            NumberOfBedrooms = 0;
            RoomsType.Clear();
            Transform[] ts = Building.GetComponentsInChildren<Transform>();
            foreach (Transform t in ts)
            {
                if (t.name == "Room" && t.gameObject.layer == 6)
                {
                    List<string> strs = new List<string>() { "Salon", "Kitchen", "Bedroom" };
                    for (int i = 0; i < 1; i++)
                    {
                        if (strs.Count > 0)
                        {
                            string type = strs[UnityEngine.Random.Range(0, strs.Count - 1)];
                            if (type == "Salon" && !RoomsType.ContainsKey(t))
                            {
                                if (NumberOfSalons * 4 <= NumberOfBedrooms)
                                {
                                    RoomsType.Add(t, RoomType.Salon);
                                }
                            }
                            else
                            {
                                i -= 1;
                            }
                            if (type == "Kitchen" && !RoomsType.ContainsKey(t))
                            {
                                if (NumberOfKitchens < NumberOfSalons)
                                {
                                    RoomsType.Add(t, RoomType.Kitchen);
                                }
                            }
                            else
                            {
                                i -= 1;
                            }
                            if (type == "Bedroom" && !RoomsType.ContainsKey(t))
                            {
                                if (NumberOfBedrooms < NumberOfSalons * 4)
                                {
                                    RoomsType.Add(t, RoomType.Bedroom);
                                }
                            }
                            else
                            {
                                i -= 1;
                            }
                            strs.Remove(type);
                        }
                    }
                }
            }
        }
        IEnumerator PutFurniture()
        {
            DetermineRooms();
            yield return new WaitForSecondsRealtime(1);
            List<Transform> groundedfurnitures = new List<Transform>();
            foreach (var rt in RoomsType)
            {
                Transform r = rt.Key;
                foreach (var f in Furnitures)
                {
                    if (f.RoomToBePlacedIn == rt.Value)
                    {

                        if (f.Object.transform.lossyScale.x < MinimalScale && f.Object.transform.lossyScale.y < MaximalHeight && f.Object.transform.lossyScale.z < MinimalScale)
                        {
                            int number = UnityEngine.Random.Range(1, f.MaxNumberPerRoom + 1);
                            for (int i = 0; i < number; i++)
                            {
                                if (f.PlacementType == FurniturePlacementType.AwayFromWall)
                                {
                                    Vector3 pos = Vector3.zero;
                                    int numberoftries = 0;
                                    Quaternion rotation = Quaternion.identity;
                                    for (int i2 = 0; i2 < 1; i2++)
                                    {
                                        pos = new Vector3(r.position.x + UnityEngine.Random.Range(-r.lossyScale.x * 0.4f, r.lossyScale.x * 0.4f), r.position.y - r.lossyScale.y * 0.5f + f.Object.transform.lossyScale.y * 0.5f, r.position.z + UnityEngine.Random.Range(-r.lossyScale.z * 0.4f, r.lossyScale.z * 0.4f));
                                        Transform[] ts = r.GetComponentsInChildren<Transform>();
                                        foreach (Transform t in ts)
                                        {
                                            if (t.gameObject.layer == 3)
                                            {
                                                Vector3 newpos = new Vector3((pos.x + t.position.x) * 0.5f, r.position.y - r.lossyScale.y * 0.5f + f.Object.transform.lossyScale.y * 0.5f, (pos.z + t.position.z) * 0.5f);
                                                pos = newpos;
                                                if (Vector3.Distance(t.position, pos) < 3)
                                                {
                                                    rotation = t.rotation;
                                                }
                                            }
                                        }
                                        yield return new WaitForSecondsRealtime(0.1f);
                                        Collider[] cols = Physics.OverlapBox(pos, f.Object.transform.lossyScale * 0.5f, rotation);
                                        bool Collides = false;
                                        foreach (Collider col in cols)
                                        {
                                            if (col.gameObject.layer == 3 || col.transform.name == "Wall" || col.transform.name == "WallModified" || col.transform.name == "WallModifiedDone")
                                            {
                                                Collides = true;
                                            }
                                        }
                                        Collider[] cols2 = Physics.OverlapBox(pos, f.Object.transform.lossyScale * 0.7f, rotation);
                                        foreach (Collider col in cols2)
                                        {
                                            if (col.transform.name == "Door")
                                            {
                                                Collides = true;
                                            }
                                        }
                                        if (Collides)
                                        {
                                            if (numberoftries < 5)
                                            {
                                                i2 -= 1;
                                                numberoftries += 1;
                                            }
                                        }
                                    }
                                    if (numberoftries < 5)
                                    {
                                        GameObject furn = Instantiate(f.Object, pos, rotation);
                                        furn.transform.parent = r;
                                    }
                                }
                                if (f.PlacementType == FurniturePlacementType.NearWall)
                                {
                                    List<RoomWall> walls = new List<RoomWall>();
                                    Transform[] ts = r.GetComponentsInChildren<Transform>();
                                    foreach (Transform t in ts)
                                    {
                                        bool Contains = false;
                                        RoomWall Contained = new RoomWall();
                                        foreach (RoomWall rw in RoomWalls)
                                        {
                                            if (rw.Wall == t)
                                            {
                                                Contains = true;
                                                Contained = rw;
                                            }
                                        }
                                        if (Contains)
                                        {
                                            walls.Add(Contained);
                                        }
                                    }
                                    if (walls.Count > 0)
                                    {
                                        RoomWall chosenwall = walls[UnityEngine.Random.Range(0, walls.Count)];
                                        GameObject furn = Instantiate(f.Object, chosenwall.Position, chosenwall.Rotation);
                                        furn.layer = 3;
                                        furn.transform.parent = r;
                                        groundedfurnitures.Add(furn.transform);
                                        Vector3 pos = chosenwall.Position + furn.transform.TransformDirection(new Vector3(UnityEngine.Random.Range(-chosenwall.Scale.x * 0.5f + furn.transform.lossyScale.x * 0.5f + 0.1f, chosenwall.Scale.x * 0.5f - furn.transform.lossyScale.x * 0.5f - 0.1f), 0, furn.transform.lossyScale.z * 0.5f)) + new Vector3(0, -chosenwall.Scale.y * 0.5f + f.Object.transform.lossyScale.y * 0.5f, 0);
                                        int numberoftries = 0;
                                        for (int i2 = 0; i2 < 1; i2++)
                                        {
                                            pos = chosenwall.Position + furn.transform.TransformDirection(new Vector3(UnityEngine.Random.Range(-chosenwall.Scale.x * 0.5f + furn.transform.lossyScale.x * 0.5f + 0.1f, chosenwall.Scale.x * 0.5f - furn.transform.lossyScale.x * 0.5f - 0.1f), 0, furn.transform.lossyScale.z * 0.5f)) + new Vector3(0, -chosenwall.Scale.y * 0.5f + f.Object.transform.lossyScale.y * 0.5f, 0);
                                            bool touchesdoor = false;
                                            Collider[] cols2 = Physics.OverlapBox(pos, furn.transform.lossyScale * 0.6f, furn.transform.rotation);
                                            foreach (Collider col in cols2)
                                            {
                                                if (col.transform.name == "Door" && col.gameObject.layer == 7)
                                                {
                                                    touchesdoor = true;
                                                }
                                            }
                                            if (touchesdoor)
                                            {
                                                if (numberoftries < 5)
                                                {
                                                    i2 -= 1;
                                                    numberoftries += 1;
                                                }
                                            }
                                        }
                                        bool collides = false;
                                        furn.transform.position = pos;
                                        yield return new WaitForSecondsRealtime(0.1f);
                                        Collider[] cols = Physics.OverlapBox(pos, f.Object.transform.lossyScale * 0.5f, chosenwall.Rotation);
                                        foreach (Collider col in cols)
                                        {
                                            if (col.gameObject.layer == 3 && col.transform != furn.transform)
                                            {
                                                collides = true;
                                            }
                                        }
                                        if (collides || numberoftries >= 5)
                                        {
                                            if (furn != null)
                                            {
                                                DestroyImmediate(furn);
                                            }
                                        }
                                    }
                                }
                                if (f.PlacementType == FurniturePlacementType.OnWall)
                                {
                                    List<RoomWall> walls = new List<RoomWall>();
                                    Transform[] ts = r.GetComponentsInChildren<Transform>();
                                    foreach (Transform t in ts)
                                    {
                                        bool Contains = false;
                                        RoomWall Contained = new RoomWall();
                                        foreach (RoomWall rw in RoomWalls)
                                        {
                                            if (rw.Wall == t)
                                            {
                                                Contains = true;
                                                Contained = rw;
                                            }
                                        }
                                        if (Contains)
                                        {
                                            walls.Add(Contained);
                                        }
                                    }
                                    if (walls.Count > 0)
                                    {
                                        RoomWall chosenwall = walls[UnityEngine.Random.Range(0, walls.Count)];
                                        GameObject furn = Instantiate(f.Object, chosenwall.Position, chosenwall.Rotation);
                                        furn.layer = 3;
                                        furn.transform.parent = r;
                                        float posx = UnityEngine.Random.Range(-chosenwall.Scale.x * 0.5f + furn.transform.lossyScale.x * 0.5f + 0.1f, chosenwall.Scale.x * 0.5f - furn.transform.lossyScale.x * 0.5f - 0.1f);
                                        Vector3 pos = chosenwall.Position + furn.transform.TransformDirection(posx, 0, furn.transform.lossyScale.z * 0.5f) + new Vector3(0, UnityEngine.Random.Range(-0.5f, 0.5f), 0);
                                        int numberoftries = 0;
                                        for (int i2 = 0; i2 < 1; i2++)
                                        {
                                            posx = UnityEngine.Random.Range(-chosenwall.Scale.x * 0.5f + furn.transform.lossyScale.x * 0.5f + 0.1f, chosenwall.Scale.x * 0.5f - furn.transform.lossyScale.x * 0.5f - 0.1f);
                                            pos = chosenwall.Position + furn.transform.TransformDirection(posx, 0, furn.transform.lossyScale.z * 0.5f) + new Vector3(0, UnityEngine.Random.Range(-0.5f, 0.5f), 0);
                                            bool touchesdoor = false;
                                            Collider[] cols2 = Physics.OverlapBox(pos, furn.transform.lossyScale * 0.6f, furn.transform.rotation);
                                            foreach (Collider col in cols2)
                                            {
                                                if (col.transform.name == "Door" && col.gameObject.layer == 7)
                                                {
                                                    touchesdoor = true;
                                                }
                                            }
                                            if (touchesdoor)
                                            {
                                                if (numberoftries < 5)
                                                {
                                                    i2 -= 1;
                                                    numberoftries += 1;
                                                }
                                            }
                                        }
                                        yield return new WaitForSecondsRealtime(0.1f);
                                        Collider[] allignmentcols = Physics.OverlapSphere(pos, 2);
                                        foreach (Collider col in allignmentcols)
                                        {
                                            if (col.transform != furn.transform && col.transform.lossyScale.x > furn.transform.lossyScale.x && col.transform.lossyScale.y > furn.transform.lossyScale.y && col.transform.lossyScale.z > furn.transform.lossyScale.z && groundedfurnitures.Contains(col.transform))
                                            {
                                                posx = Mathf.Clamp(posx, -col.transform.lossyScale.x * 0.5f + furn.transform.lossyScale.x * 0.5f, col.transform.lossyScale.x * 0.5f - furn.transform.lossyScale.x * 0.5f);
                                                pos = chosenwall.Position + furn.transform.TransformDirection(posx, 0, furn.transform.lossyScale.z * 0.5f) + new Vector3(0, UnityEngine.Random.Range(-0.5f, 0.5f), 0);
                                            }
                                        }
                                        bool collides = false;
                                        furn.transform.position = pos;
                                        yield return new WaitForSecondsRealtime(0.1f);
                                        Collider[] cols = Physics.OverlapBox(pos, f.Object.transform.lossyScale * 0.5f, chosenwall.Rotation);
                                        foreach (Collider col in cols)
                                        {
                                            if (col.gameObject.layer == 3 && col.transform != furn.transform || col.transform.name == "WindowBox")
                                            {
                                                collides = true;
                                            }
                                        }
                                        Collider[] cols3 = Physics.OverlapBox(pos, f.Object.transform.lossyScale * 0.6f, chosenwall.Rotation);
                                        foreach (Collider col in cols3)
                                        {
                                            if (col.transform.name == "WindowBox")
                                            {
                                                collides = true;
                                            }
                                        }
                                        if (collides || numberoftries >= 5)
                                        {
                                            if (furn != null)
                                            {
                                                DestroyImmediate(furn);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void OnDrawGizmos()
        {
            if (Visualization == VisualType.Rooms && Building != null)
            {
                Transform[] ts = Building.GetComponentsInChildren<Transform>();
                foreach (Transform t in ts)
                {
                    if (t.name == "Room" && t.gameObject.layer == 6)
                    {
                        if (!GizmosDictionary.ContainsKey(t))
                        {
                            Gizmos.color = new Color(UnityEngine.Random.Range(0f, 2f), UnityEngine.Random.Range(0f, 2f), UnityEngine.Random.Range(0f, 2f));
                            GizmosDictionary.Add(t, Gizmos.color);
                        }
                        else
                        {
                            Color col = new Color();
                            GizmosDictionary.TryGetValue(t, out col);
                            Gizmos.color = col;
                        }
                        Gizmos.DrawCube(t.position, t.lossyScale);
                    }
                }
            }
            if (Visualization == VisualType.Doors && Building != null)
            {
                Transform[] ts = Building.GetComponentsInChildren<Transform>();
                foreach (Transform t in ts)
                {
                    if (t.gameObject.layer == 7)
                    {
                        if (!GizmosDictionary.ContainsKey(t))
                        {
                            Gizmos.color = new Color(UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(0f, 5f));
                            GizmosDictionary.Add(t, Gizmos.color);
                        }
                        else
                        {
                            Color col = new Color();
                            GizmosDictionary.TryGetValue(t, out col);
                            Gizmos.color = col;
                        }
                        Gizmos.DrawCube(t.position, t.TransformDirection(t.lossyScale));
                    }
                }
            }
            if (Visualization == VisualType.Furniture && Building != null)
            {
                Transform[] ts = Building.GetComponentsInChildren<Transform>();
                foreach (Transform t in ts)
                {
                    if (t.gameObject.layer == 3)
                    {
                        if (!GizmosDictionary.ContainsKey(t))
                        {
                            Gizmos.color = new Color(UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(0f, 5f), UnityEngine.Random.Range(0f, 5f));
                            GizmosDictionary.Add(t, Gizmos.color);
                        }
                        else
                        {
                            Color col = new Color();
                            GizmosDictionary.TryGetValue(t, out col);
                            Gizmos.color = col;
                        }
                        Gizmos.DrawCube(t.position, t.TransformDirection(t.lossyScale));
                    }
                }
            }
        }
    }
}
