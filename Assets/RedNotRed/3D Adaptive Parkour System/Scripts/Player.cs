using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
//Requirements
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))]
public class Player : MonoBehaviour
{
    [Header("Movements")]
    //Movements
    [Tooltip("Assign the player's rigidbody")]
    public Rigidbody rb;
    private Vector3 MoveVector;
    private float Speed;
    [Tooltip("Choose the normal speed")]
    public float NormalSpeed;
    [Tooltip("Choose the jump force")]
    public float JumpForce;
    private bool CanRotate = true;
    [Header("Camera")]
    //Camera
    [Tooltip("Assign the Camera")]
    public Transform Camera;
    [Tooltip("Choose the camera rotation speed")]
    public float CameraRotationSpeed;
    private Vector2 CameraRotation;
    private bool FirstPerson;
    [Tooltip("Choose max zoom")]
    public float MaxZoom;
    [Tooltip("Choose minimal zoom")]
    public float MinZoom;
    private float CurrentZoom;
    private float DemandedZoom;
    [Tooltip("Choose the 3rd person camera offset")]
    public Vector3 CameraOffset;
    private bool RotatingCamera;
    private Vector3 CameraShakePos;
    private Quaternion CameraShakeRot;
    public enum CameraShakeType {Manual, Animation}
    public CameraShakeType CameraFeelingType;
    [HideInInspector]
    [Range(0,0.5f)] public float ShakeAmount;
    [Header("Corp")]
    //Corp
    [Tooltip("Assign the player's head")]
    public Transform Head;
    [Tooltip("Assign the player's spine")]
    public Transform Spine;
    [Tooltip("Assign the player's hip")]
    public Transform Hip;
    [Tooltip("Assign the player's collider")]
    public CapsuleCollider Collider;
    [Tooltip("Assign the player's parts that are used in the ragdoll")]
    public List<Transform> RagdollParts = new List<Transform>();
    //WallRun
    private bool WallRunning;
    private bool WallRunningL;
    private bool WallRunningR;
    private float TimeAfterWallRun;
    //Jump
    private bool Grounded;
    //JumpOver
    private bool Grabbing;
    private bool CanJump;
    private bool CanGrab = true;
    private Transform GrabObject;
    private Vector3 GrabOffset;
    private float[] JumpOverCollider = {0.02f, 0.1f, 0.09f};
    private bool JumpingOver;
    private Transform Ground;
    private bool OnLader;
    private bool CanLadder = true;
    private Transform Lader;
    private bool CanHop;
    private float TimeAfterGrab;
    private bool FlyingOver;
    //Slide
    private float TimeAfterSlide;
    private bool Sliding;
    private float[] NormalCollider = {0.02f,0.1f,0.055f};
    private float[] SlideCollider = {0.02f, 0.1f,0.03f};
    //Roll
    private bool Rolling;
    [Header("Info")]
    //Info
    [Tooltip("Choose the player's full health")]
    public float Health = 100;
    [Tooltip("Choose the time to stand up")]
    public float TimeToStandUp;
    private bool PreviousGrounded;
    private bool CanUngrab = true;
    private bool ClimbingUp;
    private enum DoubleClickButton {None,A,D,S,W}
    private DoubleClickButton DoubleClick;
    private bool ragdoll = false;
    [Header("Animations")]
    //Animations
    public Animator Anim;
    private bool Idle;
    private bool Jumping;
    private bool Run;
    private bool RunRight;
    private bool RunLeft;
    private bool RunBack;
    private bool RunBackRight;
    private bool RunBackLeft;
    private bool RunForwardRight;
    private bool RunForwardLeft;
    private bool Hang;
    private bool HangRight;
    private bool HangLeft;
    private bool Fall;
    private bool RollingAnim;
    private bool WallHang;
    private bool WallHangRight;
    private bool WallHangLeft;
    private bool Flip;
    private int JumpOverId;
    [Tooltip("Choose the time after which the player will hop on another platform")]
    public float TimeToHopUp;
    [Tooltip("Choose the time after which the player will hop on another platform")]
    public float TimeToHopDown;
    [Tooltip("Choose the time after which the player will hop on another platform")]
    public float TimeToHopRight;
    [Tooltip("Choose the time after which the player will hop on another platform")]
    public float TimeToHopLeft;
    //IK
    public List<IKComponents> IK = new List<IKComponents>();
    [Serializable]
    public class IKComponents
    {
        public Transform IKTarget;
        public Transform Rig;
        public Vector3 GrabOffset;
        public Vector3 LadderOffset;
        public Transform BodyPart;
    }
    [Header("Visual Effects")]
    //FX
    [Tooltip("Assign the dust particle system which is inside the player")]
    public ParticleSystem Dust;
    [Header("KeyCodes")]
    //KeyCodes
    [Tooltip("Assign the keycode to jump, to hang, to use lader, to stop using lader and to stop hanging")]
    public KeyCode JumpButton;
    [Tooltip("Assign the keycode to wall run")]
    public KeyCode WallRunButton;
    [Tooltip("Assign the keycode to roll")]
    public KeyCode RollButton;
    [Tooltip("Assign the keycode to slide")]
    public KeyCode SlideButton;
    [Tooltip("Assign the keycode to flip")]
    public KeyCode FlipButton;
    [Header("Sound Effects")]
    //Sounds
    [Tooltip("Assign the material you want to play the sound and then the sound you want to play")]
    public List<MaterialsInfo> MaterialsSounds;
    private Dictionary<Material,AudioClip> MaterialsSoundsDictionary = new Dictionary<Material, AudioClip>();
    [Serializable]
    public class MaterialsInfo
    {
        public Material Material;
        public AudioClip Sound;
    }
    //Assigner
    [ContextMenu("Assign Propreties")]
    public void Assign()
    {
        rb = transform.GetComponent<Rigidbody>();
        NormalSpeed = 3;
        JumpForce = 3;
        Camera = GameObject.FindGameObjectWithTag("MainCamera").transform.parent.transform;
        CameraRotationSpeed = 3;
        MinZoom = 2;
        MaxZoom = 0;
        CameraOffset = Vector3.zero;
        Collider = transform.GetComponent<CapsuleCollider>();
        Health = 100;
        Anim = transform.GetComponent<Animator>();
        JumpButton = KeyCode.Space;
        WallRunButton = KeyCode.Space;
        RollButton = KeyCode.LeftShift;
        SlideButton = KeyCode.LeftShift;
        FlipButton = KeyCode.Alpha1;
        //Parts
        RagdollParts.Clear();
        IK.Clear();
        for(int i = 0; i < 4;i++)
        {
            IKComponents comp = new IKComponents();
            IK.Add(comp);
        }
        Transform[] parts = this.transform.GetComponentsInChildren<Transform>();
        for(int i = 0; i < parts.Length; i++)
        {
            //Ragdoll
            if (parts[i].GetComponent<Rigidbody>() && parts[i].transform != transform)
            {
                RagdollParts.Add(parts[i].transform);
            }
            //Dust
            if (parts[i].name == "Dust")
            {
                Dust = parts[i].GetComponent<ParticleSystem>();
            }
            //Head
            if(parts[i].name.Length >= 4)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 4).ToUpper() == "HEAD")
                {
                    Head = parts[i];
                }
            }
            //Spine
            if (parts[i].name.Length >= 7)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 7).ToUpper() == "SPINE01")
                {
                    Spine = parts[i];
                }
            }
            //Hip
            if (parts[i].name.Length >= 3)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 3).ToUpper() == "HIP")
                {
                    Hip = parts[i];
                }
            }
            //IK
            //RHand
            if (parts[i].name.Length == 5)
            {
                if (parts[i].name.ToUpper() == "RHAND")
                {
                    IK[0].Rig = parts[i].parent.transform;
                    IK[0].IKTarget = parts[i].GetChild(0);
                    IK[0].GrabOffset = new Vector3(0.1f, 0.12f, 0);
                    IK[0].LadderOffset = new Vector3(0f, -0.7f, 0);
                }
            }
            if (parts[i].name.Length >= 6)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 6).ToUpper() == "R_HAND")
                {
                    IK[0].BodyPart = parts[i];
                }
            }
            //LHand
            if (parts[i].name.Length == 5)
            {
                if (parts[i].name.ToUpper() == "LHAND")
                {
                    IK[1].Rig = parts[i].parent.transform;
                    IK[1].IKTarget = parts[i].GetChild(0);
                    IK[1].GrabOffset = new Vector3(-0.1f, 0.12f, 0);
                    IK[1].LadderOffset = new Vector3(0f, -0.7f, 0);
                }
            }
            if (parts[i].name.Length >= 6)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 6).ToUpper() == "L_HAND")
                {
                    IK[1].BodyPart = parts[i];
                }
            }
            //RFoot
            if (parts[i].name.Length == 5)
            {
                if (parts[i].name.ToUpper() == "RFOOT")
                {
                    IK[2].Rig = parts[i].parent.transform;
                    IK[2].IKTarget = parts[i].GetChild(0);
                    IK[2].GrabOffset = new Vector3(0, 0, 0);
                    IK[2].LadderOffset = new Vector3(0f, -0.2f, 0);
                }
            }
            if (parts[i].name.Length >= 6)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 6).ToUpper() == "R_FOOT")
                {
                    IK[2].BodyPart = parts[i];
                }
            }
            //LFoot
            if (parts[i].name.Length == 5)
            {
                if (parts[i].name.ToUpper() == "LFOOT")
                {
                    IK[3].Rig = parts[i].parent.transform;
                    IK[3].IKTarget = parts[i].GetChild(0);
                    IK[3].GrabOffset = new Vector3(0, 0, 0);
                    IK[3].LadderOffset = new Vector3(0f, -0.2f, 0);
                }
            }
            if (parts[i].name.Length >= 6)
            {
                if (parts[i].name.Substring(parts[i].name.Length - 6).ToUpper() == "L_FOOT")
                {
                    IK[3].BodyPart = parts[i];
                }
            }
        }
        //Debug
        if(MaterialsSounds.Count == 0)
        {
            Debug.Log("If you want, please add the material and the sound you want to play in materials sounds");
        }
        if (Head == null)
        {
            Debug.LogError("No object found ending by Head");
        }
        if (Spine == null)
        {
            Debug.LogError("No object found ending by Spine01");
        }
        if (Hip == null)
        {
            Debug.LogError("No object found ending by Hip");
        }
        if (Dust == null)
        {
            Debug.LogError("No object named Dust");
        }
        if (IK[0] == null || IK[1] == null || IK[2] == null || IK[3] == null)
        {
            Debug.LogError("Couldn't find all ik components");
        }
    }
#if UNITY_EDITOR
    //Show Settings
    [CustomEditor(typeof(Player))]
    public class ShowSettings : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var pl = (Player)target;
            if(pl.CameraFeelingType == CameraShakeType.Manual)
            {
                pl.ShakeAmount = EditorGUILayout.Slider("Shake Amount",pl.ShakeAmount,0,0.5f);
            }
        }
    }
#endif
    //Controller
    void Start()
    {
        Speed = NormalSpeed;
        Cursor.lockState = CursorLockMode.Locked;
        //Ragdoll
        for(int i = 0; i < RagdollParts.ToArray().Length; i++)
        {
            RagdollParts[i].GetComponent<Collider>().enabled = false;
            RagdollParts[i].GetComponent<Rigidbody>().isKinematic = true;
        }
        Collider.enabled = true;
        Anim.enabled = true;
        ragdoll = false;
        //Create Dictionary
        for (int i = 0; i < MaterialsSounds.Count; i++)
        {
            MaterialsSoundsDictionary.Add(MaterialsSounds[i].Material, MaterialsSounds[i].Sound);
        }
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    void Update()
    {
        if(!ragdoll)
        {
            //Move
            Move();
            //Jump
            Jump();
            //WallRun
            WallRun();
            //JumpOverObstacle
            JumpOver();
            //Slide
            Slide();
            //Roll
            Roll();
            //Animations
            Animate();
            //VisualEffects
            FX();
            //SoundEffects
            SoundFX();
        }
        else
        {
            //FX
            Dust.Stop();
            GetComponent<AudioSource>().Stop();
            //Stand Up
            if(Input.GetKeyDown(JumpButton))
            {
                Ragdoll(false);
                ClimbingUp = false;
            }
        }
    }
    private void FixedUpdate()
    {
        //Camera
        CameraControl();
        CameraFX();
    }
    void Move()
    {
        if(!ClimbingUp)
        {
            MoveVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            Vector3 MoveVelocity = transform.TransformDirection(MoveVector) * Speed;
            //Normal Movement
            if (!WallRunning && !Grabbing && !Sliding && !JumpingOver && !OnLader && !FlyingOver)
            {
                rb.linearVelocity = new Vector3(MoveVelocity.x, rb.linearVelocity.y, MoveVelocity.z);
                rb.useGravity = true;
                CanRotate = true;
            }
            //WallRun Movement
            if (WallRunning)
            {
                float wallruntime = Mathf.Clamp(TimeAfterWallRun, 0, 2);
                rb.linearVelocity = transform.TransformDirection(new Vector3(0, rb.linearVelocity.y * (0.5f * (2 - wallruntime)), Speed - wallruntime));
                rb.useGravity = true;
                Grabbing = false;
                Sliding = false;
                JumpingOver = false;
                CanRotate = true;
            }
            //Grabbing Movement
            if (Grabbing)
            {
                if (CanUngrab)
                {
                    rb.linearVelocity = transform.TransformDirection(new Vector3((MoveVector.x * Speed * 0.3f), 0, 0)) + GrabOffset;
                    transform.position = new Vector3(transform.position.x, (GrabObject.position.y + GrabObject.localScale.y / 2) - new Vector3(0, 1.1f, 0).y, transform.position.z);
                }
                else
                {
                    if (CanHop)
                    {
                        rb.position = Vector3.Lerp(transform.position, new Vector3(GrabObject.position.x, (GrabObject.position.y + GrabObject.localScale.y / 2) - new Vector3(0, 1.5f, 0).y, GrabObject.position.z) - transform.TransformDirection(0.1f, 0, 0.1f), 0.1f);
                    }
                }
                rb.useGravity = false;
                WallRunning = false;
                JumpingOver = false;
                Sliding = false;
                CanRotate = false;
                Collider[] colliders = Physics.OverlapSphere(Head.position, 0.7f);
                colliders.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                if (!colliders.ToList().Contains(GrabObject.GetComponent<Collider>()) && CanUngrab)
                {
                    StartCoroutine(WaitToCanGrab());
                    Grabbing = false;
                    GrabObject = null;
                }
                foreach (Collider near in colliders)
                {
                    if (near.transform != transform && near.transform.tag != "Lader" && CanUngrab && Vector3.Distance(new Vector3(0, near.transform.position.y + near.transform.localScale.y / 2, 0), new Vector3(0, Spine.position.y, 0)) > 0.5f && Vector3.Distance(new Vector3(0, near.transform.position.y + near.transform.localScale.y / 2, 0), new Vector3(0, Spine.position.y, 0)) < 1f)
                    {
                        GrabObject = near.transform;
                    }
                }
                RaycastHit hit;
                if (GrabObject != null && CanUngrab)
                {
                    if (Physics.Linecast(Head.position, GrabObject.position, out hit) && hit.transform == GrabObject)
                    {
                        Quaternion rot;
                        rot = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.right, hit.normal) * Quaternion.Euler(0, -90, 0), 0.1f);
                        rot.x = Mathf.Clamp(rot.x, 0, 0);
                        rot.z = Mathf.Clamp(rot.z, 0, 0);
                        transform.rotation = rot;
                    }
                    if (Physics.Raycast(Head.position, transform.forward, out hit) && hit.transform == GrabObject)
                    {
                        GrabOffset = (hit.point - transform.position) * hit.distance;
                    }
                }
                //ChangeGrabObject
                //Vertical
                //Up
                if (Input.GetKeyDown(KeyCode.W))
                {
                    if (DoubleClick == DoubleClickButton.W)
                    {
                        DoubleClick = DoubleClickButton.None;
                        Collider[] Platforms = Physics.OverlapSphere(Head.position, 2);
                        Platforms.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                        foreach (Collider Platform in Platforms)
                        {
                            Vector3 pos = transform.InverseTransformPoint(new Vector3(Platform.transform.position.x, Platform.transform.position.y + Platform.transform.localScale.y / 2, Platform.transform.position.z));
                            if (pos.y > 0.2f && pos.y < 0.3f && Platform.transform != GrabObject && Platform.transform != transform)
                            {
                                CanUngrab = false;
                                GrabObject = Platform.transform;
                                StartCoroutine(Hop(TimeToHopUp));
                                //Animation
                                if (transform.InverseTransformPoint(GrabObject.position).x < 0.1f && transform.InverseTransformPoint(GrabObject.position).x > -0.1f)
                                {
                                    ChangeAnimation("HopUp");
                                }
                                if (transform.InverseTransformPoint(GrabObject.position).x < -0.1f)
                                {
                                    ChangeAnimation("HopLeft");
                                }
                                if (transform.InverseTransformPoint(GrabObject.position).x > 0.1f)
                                {
                                    ChangeAnimation("HopRight");
                                }
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(StopDoubleClick(DoubleClickButton.W));
                    }
                }
                //Down
                if (Input.GetKeyDown(KeyCode.S))
                {
                    if (DoubleClick == DoubleClickButton.S)
                    {
                        Collider[] Platforms = Physics.OverlapSphere(Spine.position, 3);
                        Platforms.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                        foreach (Collider Platform in Platforms)
                        {
                            Vector3 pos = transform.InverseTransformPoint(new Vector3(Platform.transform.position.x, Platform.transform.position.y + Platform.transform.localScale.y / 2, Platform.transform.position.z));
                            if (pos.y < -0.01f && pos.y > -0.05f && Platform.transform != GrabObject && Platform.transform != transform)
                            {
                                CanUngrab = false;
                                GrabObject = Platform.transform;
                                StartCoroutine(Hop(TimeToHopDown));
                                //Animation
                                if (transform.InverseTransformPoint(GrabObject.position).x < 0.1f && transform.InverseTransformPoint(GrabObject.position).x > -0.1f)
                                {
                                    ChangeAnimation("HopDown");
                                }
                                if (transform.InverseTransformPoint(GrabObject.position).x < -0.1f)
                                {
                                    ChangeAnimation("HopLeft");
                                }
                                if (transform.InverseTransformPoint(GrabObject.position).x > 0.1f)
                                {
                                    ChangeAnimation("HopRight");
                                }
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(StopDoubleClick(DoubleClickButton.S));
                    }
                }
                //Horizontal
                //Right
                if (Input.GetKeyDown(KeyCode.D))
                {
                    if (DoubleClick == DoubleClickButton.D)
                    {
                        Collider[] Platforms = Physics.OverlapSphere(Spine.position, 1);
                        Platforms.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                        foreach (Collider Platform in Platforms)
                        {
                            Vector3 pos = transform.InverseTransformPoint(new Vector3(Platform.transform.position.x, Platform.transform.position.y + Platform.transform.localScale.y / 2, Platform.transform.position.z));
                            if (pos.x > 0.1f && pos.y < 0.3f && pos.y > -0.05f && Platform.transform != GrabObject && Platform.transform != transform)
                            {
                                CanUngrab = false;
                                GrabObject = Platform.transform;
                                StartCoroutine(Hop(TimeToHopRight));
                                //Animation
                                ChangeAnimation("HopRight");
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(StopDoubleClick(DoubleClickButton.D));
                    }
                }
                //Left
                if (Input.GetKeyDown(KeyCode.A))
                {
                    if (DoubleClick == DoubleClickButton.A)
                    {
                        Collider[] Platforms = Physics.OverlapSphere(Spine.position, 1);
                        Platforms.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                        foreach (Collider Platform in Platforms)
                        {
                            Vector3 pos = transform.InverseTransformPoint(new Vector3(Platform.transform.position.x, Platform.transform.position.y + Platform.transform.localScale.y / 2, Platform.transform.position.z));
                            if (pos.x < -0.1f && pos.y < 0.3f && pos.y > -0.05f && Platform.transform != GrabObject && Platform.transform != transform)
                            {
                                CanUngrab = false;
                                GrabObject = Platform.transform;
                                StartCoroutine(Hop(TimeToHopLeft));
                                //Animation
                                ChangeAnimation("HopLeft");
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(StopDoubleClick(DoubleClickButton.A));
                    }
                }
                if (GrabObject != null)
                {
                    Collider[] ungrabs = Physics.OverlapSphere(Head.position, 0.2f);
                    ungrabs.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
                    if (ungrabs.ToList().Contains(GrabObject.GetComponent<Collider>()))
                    {
                        CanUngrab = true;
                    }
                }
                else
                {
                    CanUngrab = true;
                }
            }
            else
            {
                CanUngrab = true;
            }
            //Jump Over Movement && Flying Over
            if (JumpingOver || FlyingOver)
            {
                rb.linearVelocity = transform.TransformDirection(new Vector3(0, 0, Speed));
                rb.useGravity = false;
                CanRotate = false;
                WallRunning = false;
                Grabbing = false;
                Sliding = false;
                Collider.direction = 2;
                Collider.center = new Vector3(0, JumpOverCollider[2], 0);
                Collider.radius = JumpOverCollider[0];
                Collider.height = JumpOverCollider[1];
            }
            //Lader Movement
            if (OnLader)
            {
                rb.linearVelocity = transform.TransformDirection(new Vector3(0, (MoveVector.z * Speed) * 0.5f, 0.1f));
                rb.useGravity = false;
                CanRotate = false;
                RaycastHit hit;
                if (!Physics.Raycast(Spine.position, Spine.forward, out hit, 1))
                {
                    OnLader = false;
                    Lader = null;
                    rb.AddForce(Vector3.up * JumpForce * 0.2f, ForceMode.Impulse);
                }
                if (Lader != null)
                {
                    transform.eulerAngles = new Vector3(Lader.eulerAngles.z, Lader.eulerAngles.y - 90, 0);
                }
            }
            //Slide Movement
            if (Sliding)
            {
                float slidetime = Mathf.Clamp(TimeAfterWallRun, 0, 2);
                rb.linearVelocity = transform.TransformDirection(new Vector3(0, rb.linearVelocity.y, Speed - slidetime));
                rb.useGravity = true;
                WallRunning = false;
                Grabbing = false;
                JumpingOver = false;
                CanRotate = false;
                Collider.direction = 2;
                Collider.center = new Vector3(0, SlideCollider[2], 0);
                Collider.radius = SlideCollider[0];
                Collider.height = SlideCollider[1];
            }
            else
            {
                if (!JumpingOver && !FlyingOver)
                {
                    Collider.direction = 1;
                    Collider.center = new Vector3(0, NormalCollider[2], 0);
                    Collider.radius = NormalCollider[0];
                    Collider.height = NormalCollider[1];
                }
            }
            if (!RotatingCamera && CanRotate)
            {
                if (FirstPerson)
                {
                    transform.eulerAngles = new Vector3(0, Camera.eulerAngles.y, 0);
                }
                else
                {
                    if (MoveVector != Vector3.zero)
                    {
                        transform.eulerAngles = new Vector3(0, Camera.eulerAngles.y, 0);
                    }
                }
            }
        }
    }
    IEnumerator Hop(float Time)
    {
        CanHop = false;
        yield return new WaitForSeconds(Time);
        CanHop = true;
    }
    IEnumerator StopDoubleClick(DoubleClickButton button)
    {
        DoubleClick = button;
        yield return new WaitForSeconds(0.2f);
        DoubleClick = DoubleClickButton.None;
    }
    void CameraControl()
    {
        CameraRotation.x += Input.GetAxis("Mouse X");
        CameraRotation.y -= Input.GetAxis("Mouse Y");
        if (!FirstPerson)
        {
            Camera.LookAt(Head);
            Camera.position = Vector3.Lerp(Camera.position, Head.position + Quaternion.Euler(CameraRotation.y, CameraRotation.x, 0) * CameraOffset,0.2f);
            CameraOffset.z = CurrentZoom;
        }
        else
        {
            CameraRotation.y = Mathf.Clamp(CameraRotation.y, -55, 55);
            if(Grabbing || OnLader || Sliding || Rolling || Flip)
            {
                float rotyp = 45 + transform.eulerAngles.y;
                float rotym = -45 + transform.eulerAngles.y;
                CameraRotation.x = Mathf.Clamp(CameraRotation.x, rotym, rotyp);
            }
            Camera.position = Head.position + Head.TransformDirection(new Vector3(0, 0, 0.2f));
            Camera.rotation = Quaternion.Lerp(Camera.rotation, Quaternion.Euler(CameraRotation.y, CameraRotation.x,0),0.5f);
        }
        //ChangeZoom
        DemandedZoom += Input.mouseScrollDelta.y;
        DemandedZoom = Mathf.Clamp(DemandedZoom, MaxZoom, MinZoom);
        //ChangePerson
        if(DemandedZoom == MaxZoom)
        {
            FirstPerson = true;
        }
        else
        {
            FirstPerson = false;
        }
        //RotatingCamera
        if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            RotatingCamera = true;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            RotatingCamera = false;
        }
        //AntiClipping
        if(!FirstPerson)
        {
            RaycastHit hit;
            if (Physics.Linecast(Head.position, Camera.position, out hit) && hit.transform != transform)
            {
                CurrentZoom = MinZoom - (MinZoom - hit.distance);
            }
            else
            {
                CurrentZoom = DemandedZoom;
            }    
        }
    }
    void Jump()
    {
        //CheckGround
        PreviousGrounded = Grounded;
        RaycastHit hit;
        if(Physics.Raycast(Spine.position,Vector3.down,out hit,1))
        {
            Grounded = true;
            Ground = hit.transform;
        }
        else
        {
            Grounded = false;
            Ground = null;
        }
        //Jump
        if(Input.GetKeyDown(JumpButton) && Grounded && !JumpingOver && !Grabbing && !OnLader)
        {
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        }
    }
    void WallRun()
    {
        //Start WallRun
        if (Input.GetKeyDown(WallRunButton) && !WallRunning && !Grounded && MoveVector.z > 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(Spine.position, Spine.TransformDirection(Vector3.left), out hit, 1))
            {
                WallRunning = true;
                WallRunningL = true;
                WallRunningR = false;
                TimeAfterWallRun = 0;
                CancelInvoke("CalculateTimeAfter");
                InvokeRepeating("CalculateTimeAfter", 0.1f, 0.1f);
            }
            if (Physics.Raycast(Spine.position, Spine.TransformDirection(Vector3.right), out hit, 1))
            {
                WallRunning = true;
                WallRunningL = false;
                WallRunningR = true;
                TimeAfterWallRun = 0;
                CancelInvoke("CalculateTimeAfter");
                InvokeRepeating("CalculateTimeAfter", 0.1f, 0.1f);
            }
        }
        //Cancel WallRun
        if (Input.GetKeyDown(WallRunButton) && WallRunning && TimeAfterWallRun > 1)
        {
            WallRunning = false;
            WallRunningL = false;
            WallRunningR = false;
            TimeAfterWallRun = 0;
            CancelInvoke("CalculateTimeAfter");
            rb.AddForce(Vector3.up * JumpForce / 2, ForceMode.Impulse);
        }
        if(Grounded)
        {
            WallRunning = false;
            WallRunningL = false;
            WallRunningR = false;
            TimeAfterWallRun = 0;
            CancelInvoke("CalculateTimeAfter");
        }
        if(WallRunningL)
        {
            RaycastHit hit;
            if (!Physics.Raycast(Spine.position, Spine.TransformDirection(Vector3.left), out hit, 1))
            {
                WallRunning = false;
                WallRunningL = false;
                WallRunningR = false;
                TimeAfterWallRun = 0;
                CancelInvoke("CalculateTimeAfter");
                rb.AddForce(Vector3.up * JumpForce / 2, ForceMode.Impulse);
            }
        }
        if (WallRunningR)
        {
            RaycastHit hit;
            if (!Physics.Raycast(Spine.position, Spine.TransformDirection(Vector3.right), out hit, 1))
            {
                WallRunning = false;
                WallRunningL = false;
                WallRunningR = false;
                TimeAfterWallRun = 0;
                CancelInvoke("CalculateTimeAfter");
                rb.AddForce(Vector3.up * JumpForce / 2, ForceMode.Impulse);
            }
        }
    }
    void CalculateTimeAfter()
    {
        TimeAfterWallRun += 0.1f;
    }
    void JumpOver()
    {
        if(Input.GetKeyDown(JumpButton))
        {
            Collider[] colliders = Physics.OverlapSphere(Head.position, 0.5f);
            colliders.OrderBy(x => Vector3.Distance(transform.position, x.transform.position));
            foreach (Collider near in colliders)
            {
                //Grab
                if (near.transform.tag != "LadderStick" && near.transform.tag != "Lader" && near.transform != transform && Vector3.Distance(new Vector3(0, near.transform.position.y + near.transform.localScale.y / 2, 0), new Vector3(0, Spine.position.y, 0)) > 0.5f && Vector3.Distance(new Vector3(0, near.transform.position.y + near.transform.localScale.y / 2, 0), new Vector3(0, Spine.position.y, 0)) < 1f && near.transform.position.y + near.transform.localScale.y / 2 > Spine.position.y && CanGrab && !Grabbing)
                {
                    Grabbing = true;
                    GrabObject = near.transform;
                    StartCoroutine(WaitToCanJump());
                }
            }
            Collider[] colliders2 = Physics.OverlapSphere(Spine.position, 0.7f);
            colliders2.OrderBy(x => Spine.position.y - (x.transform.position.y + x.transform.localScale.y / 2));
            foreach (Collider near in colliders2)
            {
                float NearSurfaceHeight = Spine.position.y - (near.transform.position.y + near.transform.localScale.y / 2);
                //JumpOver
                if (near.transform != Ground && near.transform != transform && NearSurfaceHeight > 0.1f && NearSurfaceHeight < 0.3f && near.transform.position.y + near.transform.localScale.y / 2 < Spine.position.y && !JumpingOver && !FlyingOver)
                {
                    StartCoroutine(WaitToStopJumping());
                    int anim = UnityEngine.Random.Range(0,2);
                    JumpOverId = anim;
                }
                //FlyOver
                if (near.transform != Ground && near.transform != transform && NearSurfaceHeight > 0f && NearSurfaceHeight < 0.1f && near.transform.position.y + near.transform.localScale.y / 2 < Spine.position.y && !JumpingOver && !FlyingOver)
                {
                    RaycastHit dist;
                    if(Physics.Linecast(Spine.position,near.transform.position,out dist) && dist.distance > 0.5f && dist.transform == near.transform)
                    {
                        StartCoroutine(WaitToStopFlying());
                    }
                }
                //Lader
                if (near.transform.tag == "Lader" && !OnLader && Lader == null && CanLadder)
                {
                    StartCoroutine(WaitToCanJump());
                    OnLader = true;
                    Lader = near.transform;
                }
            }
        }
        //Cancel Grabbing
        if(Input.GetKeyDown(JumpButton) && Grabbing && CanJump)
        {
            TimeAfterGrab = 0;
            CancelInvoke("CalculateTimeAfterGrab");
            InvokeRepeating("CalculateTimeAfterGrab",0.1f,0.1f);
        }
        if (Input.GetKeyUp(JumpButton) && Grabbing && CanJump && TimeAfterGrab > 0f)
        {
            CancelInvoke("CalculateTimeAfterGrab");
            StartCoroutine(WaitToCanGrab());
            if(TimeAfterGrab < 0.5f)
            {
                rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            }
            else
            {
                RaycastHit hit;
                if(!Physics.Raycast(Head.position + new Vector3(0,0.3f,0),transform.forward,out hit,0.5f))
                {
                    transform.position = new Vector3(transform.position.x, (GrabObject.transform.position.y + GrabObject.localScale.y / 2), transform.position.z) + transform.TransformDirection(new Vector3(0, -0.5f, 0.5f));
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    ClimbingUp = true;
                    ChangeAnimation("ClimbingUp");
                }
                else
                {
                    if (hit.transform != GrabObject)
                    {
                        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
                    }
                    else
                    {
                        transform.position = new Vector3(transform.position.x, (GrabObject.transform.position.y + GrabObject.localScale.y / 2), transform.position.z) + transform.TransformDirection(new Vector3(0, -0.5f, 0.5f));
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        ClimbingUp = true;
                        ChangeAnimation("ClimbingUp");
                    }
                }
            }
            Grabbing = false;
            GrabObject = null;
            TimeAfterGrab = 0;
        }
        //Cancel Lader
        if (Input.GetKeyDown(JumpButton) && OnLader && CanJump)
        {
            StartCoroutine(WaitToCanLadder());
            OnLader = false;
            Lader = null;
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        }
    }
    void CalculateTimeAfterGrab()
    {
        TimeAfterGrab += 0.1f;
    }
    public void StopClimbingUp()
    {
        ClimbingUp = false;
    }
    IEnumerator WaitToCanJump()
    {
        CanJump = false;
        yield return new WaitForSeconds(0.01f);
        CanJump = true;
    }
    IEnumerator WaitToCanLadder()
    {
        CanLadder = false;
        yield return new WaitForSeconds(0.01f);
        CanLadder = true;
    }
    IEnumerator WaitToCanGrab()
    {
        CanGrab = false;
        yield return new WaitForSeconds(0.01f);
        CanGrab = true;
    }
    IEnumerator WaitToStopJumping()
    {
        JumpingOver = true;
        yield return new WaitForSeconds(0.35f);
        JumpingOver = false;
    }
    IEnumerator WaitToStopFlying()
    {
        FlyingOver = true;
        yield return new WaitForSeconds(2.10f);
        FlyingOver = false;
    }
    void Slide()
    {
        if(Input.GetKeyDown(SlideButton) && Grounded && !Sliding && MoveVector.z > 0)
        {
            Sliding = true;
            TimeAfterSlide = 0;
            CancelInvoke("CalculateTimeAfterSlide");
            InvokeRepeating("CalculateTimeAfterSlide", 0.1f, 0.1f);
        }
        if(Input.GetKeyUp(SlideButton))
        {
            Sliding = false;
            CancelInvoke("CalculateTimeAfterSlide");
        }
        if(!Grounded)
        {
            Sliding = false;
            CancelInvoke("CalculateTimeAfterSlide");
        }
        if(TimeAfterSlide > 1.9f)
        {
            Sliding = false;
            CancelInvoke("CalculateTimeAfterSlide");
        }
    }
    void CalculateTimeAfterSlide()
    {
        TimeAfterSlide += 0.1f;
    }
    void Roll()
    {
        //Damage
        if(PreviousGrounded != Grounded && rb.linearVelocity.y < -2)
        {
            if (!Rolling)
            {
                Health += rb.linearVelocity.y;
            }
            else
            {
                Health += rb.linearVelocity.y * 0.1f;
            }
        }
        //Roll
        if(!Grounded && Input.GetKeyDown(RollButton))
        {
            StartCoroutine(RollTimer());
        }
    }
    IEnumerator RollTimer()
    {
        Rolling = true;
        yield return new WaitForSeconds(0.6f);
        Rolling = false;
    }
    void Animate()
    {
        //Booleans
        RaycastHit hit;
        if(Grounded && MoveVector == Vector3.zero && !RollingAnim && !Sliding && !JumpingOver && !Flip && !Grabbing && !FlyingOver)
        {
            Idle = true;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.z > 0 && MoveVector.x == 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !Grabbing && !FlyingOver)
        {
            Idle = false;
            Run = true;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.z < 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !Grabbing && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = true;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.x > 0 && MoveVector.z == 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = true;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.x < 0 && MoveVector.z == 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = true;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.x < 0 && MoveVector.z < 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = true;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.x > 0 && MoveVector.z < 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = true;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grounded && MoveVector.x > 0 && MoveVector.z > 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = true;
        }
        if (Grounded && MoveVector.x < 0 && MoveVector.z > 0 && !RollingAnim && !Sliding && !JumpingOver && !Flip && !FlyingOver)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = true;
            RunForwardRight = false;
        }
        if (Grounded && Input.GetKeyDown(JumpButton))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = true;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if(Grounded && Input.GetKeyUp(JumpButton))
        {
            Jumping = false;
        }
        if(!Grounded && !Grabbing && rb.linearVelocity.y > 0)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = true;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector == Vector3.zero && !Physics.Raycast(Hip.position,Hip.TransformDirection(Vector3.forward),out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = true;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector.x < 0 && !Physics.Raycast(Hip.position, Hip.TransformDirection(Vector3.forward), out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = true;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector.x > 0 && !Physics.Raycast(Hip.position, Hip.TransformDirection(Vector3.forward), out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = true;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector == Vector3.zero && Physics.Raycast(Hip.position, Hip.TransformDirection(Vector3.forward), out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = true;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector.x < 0 && Physics.Raycast(Hip.position, Hip.TransformDirection(Vector3.forward), out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = true;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (Grabbing && MoveVector.x > 0 && Physics.Raycast(Hip.position, Hip.TransformDirection(Vector3.forward), out hit, 0.5f))
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = true;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if (!Grounded && rb.linearVelocity.y < 0 && !RollingAnim)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = true;
            RollingAnim = false;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
        }
        if(Grounded && Rolling && !RollingAnim)
        {
            Idle = false;
            Run = false;
            Hang = false;
            HangRight = false;
            HangLeft = false;
            WallHang = false;
            WallHangRight = false;
            WallHangLeft = false;
            Jumping = false;
            Fall = false;
            RollingAnim = true;
            RunBack = false;
            RunRight = false;
            RunLeft = false;
            RunBackRight = false;
            RunBackLeft = false;
            RunForwardLeft = false;
            RunForwardRight = false;
            StartCoroutine(ControlRolling());
        }
        if (!Grounded && !Grabbing && Input.GetKeyDown(FlipButton) && !Flip)
        {
            StartCoroutine(ControlTrick(1));
        }
        //Animation Booleans
        if(CanUngrab && !ClimbingUp)
        {
            if (Idle)
            {
                ChangeAnimation("Idle");
            }
            if (Run)
            {
                ChangeAnimation("Run");
            }
            if (RunBack)
            {
                ChangeAnimation("RunBack");
            }
            if (RunRight)
            {
                ChangeAnimation("RunRight");
            }
            if (RunLeft)
            {
                ChangeAnimation("RunLeft");
            }
            if (RunBackRight)
            {
                ChangeAnimation("RunBackRight");
            }
            if (RunBackLeft)
            {
                ChangeAnimation("RunBackLeft");
            }
            if (RunForwardRight)
            {
                ChangeAnimation("RunForwardRight");
            }
            if (RunForwardLeft)
            {
                ChangeAnimation("RunForwardLeft");
            }
            if (Hang)
            {
                ChangeAnimation("Hang");
            }
            if (HangRight)
            {
                ChangeAnimation("HangRight");
            }
            if (HangLeft)
            {
                ChangeAnimation("HangLeft");
            }
            if (WallHang)
            {
                ChangeAnimation("WallHang");
            }
            if (WallHangRight)
            {
                ChangeAnimation("WallHangRight");
            }
            if (WallHangLeft)
            {
                ChangeAnimation("WallHangLeft");
            }
            if (Jumping)
            {
                ChangeAnimation("Jump");
            }
            if (Fall)
            {
                ChangeAnimation("Fall");
            }
            if (RollingAnim)
            {
                ChangeAnimation("Roll");
            }
            if (Sliding)
            {
                ChangeAnimation("Slide");
            }
            if (WallRunningR)
            {
                ChangeAnimation("WallRunLeft");
            }
            if (WallRunningL)
            {
                ChangeAnimation("WallRunRight");
            }
            if(JumpingOver)
            {
                if(JumpOverId == 0)
                {
                    ChangeAnimation("JumpOver");
                }
                if (JumpOverId == 1)
                {
                    ChangeAnimation("JumpOver2");
                }
            }
            if (FlyingOver)
            {
                ChangeAnimation("FlyOver");
            }
            if (OnLader && MoveVector.z == 0)
            {
                ChangeAnimation("ClimbIdle");
            }
            if (OnLader && MoveVector.z > 0)
            {
                ChangeAnimation("Climbing");
            }
            if (OnLader && MoveVector.z < 0)
            {
                ChangeAnimation("ClimbDown");
            }
            if (Flip)
            {
                ChangeAnimation("Flip");
            }
        }
        //IK
        //Weight
        if (!Grabbing && !OnLader && !JumpingOver && !FlyingOver && !ClimbingUp)
        {
            SetRigWeight(IK[0].Rig, 0);
            SetRigWeight(IK[1].Rig, 0);
            SetRigWeight(IK[2].Rig, 0f);
            SetRigWeight(IK[3].Rig, 0f);
        }
        if(Grabbing || OnLader)
        {
            SetRigWeight(IK[0].Rig, 0.5f);
            SetRigWeight(IK[1].Rig, 0.5f);
            SetRigWeight(IK[2].Rig, 0f);
            SetRigWeight(IK[3].Rig, 0f);
        }
        if(JumpingOver)
        {
            SetRigWeight(IK[0].Rig, 1);
            SetRigWeight(IK[1].Rig, 1);
            SetRigWeight(IK[2].Rig, 0f);
            SetRigWeight(IK[3].Rig, 0f);
        }
        if (FlyingOver)
        {
            SetRigWeight(IK[0].Rig, 0);
            SetRigWeight(IK[1].Rig, 0);
        }
        if(ClimbingUp)
        {
            SetRigWeight(IK[0].Rig, 0.5f);
            SetRigWeight(IK[1].Rig, 0.5f);
            SetRigWeight(IK[2].Rig, 0.5f);
            SetRigWeight(IK[3].Rig, 0.5f);
        }
        //Grabbing
        if (Grabbing)
        {
            if(GrabObject != null)
            {
                RaycastHit wall;
                if (Physics.Raycast(Head.position, transform.forward, out wall) && wall.transform == GrabObject)
                {
                    IK[0].IKTarget.position = wall.point + transform.TransformDirection(IK[0].GrabOffset);
                    IK[1].IKTarget.position = wall.point + transform.TransformDirection(IK[1].GrabOffset);
                }
            }
        }
        //Ladder
        if(OnLader)
        {
            if(Lader != null)
            {
                foreach (var BodyPart in IK)
                {
                    SetRigWeight(BodyPart.Rig, 0.5f);
                    Collider[] col = Physics.OverlapSphere(BodyPart.BodyPart.position,0.4f);
                    col.OrderBy(x => Vector3.Distance(x.transform.position,BodyPart.BodyPart.position));
                    foreach(Collider LadderStick in col)
                    {
                        if(LadderStick.transform.tag == "LadderStick" && LadderStick.transform.position.y + transform.TransformDirection(BodyPart.LadderOffset).y >= Spine.position.y)
                        {
                            BodyPart.IKTarget.position = Vector3.Lerp(BodyPart.IKTarget.position, LadderStick.transform.position + transform.TransformDirection(BodyPart.LadderOffset),0.5f);
                        }
                    }
                }
            }
        }
        if(JumpingOver)
        {
            RaycastHit platform;
            if (Physics.Raycast(Spine.position, Vector3.down, out platform))
            {
                IK[0].IKTarget.position = platform.point;
                IK[1].IKTarget.position = platform.point;
            }
        }
        if (FlyingOver)
        {
            RaycastHit platform;
            if (Physics.Raycast(Spine.position, Vector3.down, out platform,0.3f))
            {
                SetRigWeight(IK[2].Rig, 0.5f);
                SetRigWeight(IK[3].Rig, 0.5f);
                IK[2].IKTarget.position = platform.point + transform.TransformDirection(0.2f,0,0);
                IK[3].IKTarget.position = platform.point + transform.TransformDirection(-0.2f, 0, 0);
            }
            else
            {
                SetRigWeight(IK[2].Rig, 0f);
                SetRigWeight(IK[3].Rig, 0f);
            }
        }
        if(ClimbingUp)
        {
            foreach(var BodyPart in IK)
            {
                Collider[] colliders = Physics.OverlapSphere(BodyPart.BodyPart.position,0.5f);
                foreach(Collider near in colliders)
                {
                    RaycastHit Near;
                    if(Physics.Linecast(BodyPart.BodyPart.position,near.transform.position,out Near) && Near.transform == near.transform && near.transform != transform)
                    {
                        BodyPart.IKTarget.position = Near.point;
                    }
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale -= 0.1f;
        }
    }
    void ChangeAnimation(string Animation)
    {
        if(Anim.GetBool(Animation) == false)
        {
            for (int i = 0; i < Anim.parameterCount; i++)
            {
                if (Anim.GetParameter(i).name != Animation)
                {
                    Anim.SetBool(Anim.GetParameter(i).name, false);
                }
                else
                {
                    Anim.SetBool(Animation, true);
                }
            }
        }
    }
    IEnumerator ControlRolling()
    {
        RollingAnim = true;
        yield return new WaitForSeconds(1f);
        RollingAnim = false;
    }
    IEnumerator ControlTrick(float time)
    {
        Flip = true;
        yield return new WaitForSeconds(time);
        Flip = false;
    }
    void CameraFX()
    {
        if(CameraFeelingType == CameraShakeType.Manual)
        {
            if (FirstPerson)
            {
                if (WallRunningL)
                {
                    Camera.GetChild(0).GetComponent<Animator>().enabled = false;
                    Camera.GetChild(0).localRotation = Quaternion.Lerp(Camera.GetChild(0).localRotation, Quaternion.Euler(0, 0, -25), 0.1f);
                }
                if (WallRunningR)
                {
                    Camera.GetChild(0).GetComponent<Animator>().enabled = false;
                    Camera.GetChild(0).localRotation = Quaternion.Lerp(Camera.GetChild(0).localRotation, Quaternion.Euler(0, 0, 25), 0.1f);
                }
                if (!WallRunning && !RollingAnim)
                {
                    Camera.GetChild(0).GetComponent<Animator>().enabled = false;
                    Camera.GetChild(0).localRotation = Quaternion.Lerp(Camera.GetChild(0).localRotation, Quaternion.Euler(0, 0, 0), 0.1f);
                }
                if (RollingAnim || Flip)
                {
                    Camera.GetChild(0).GetComponent<Animator>().enabled = true;
                    Camera.GetChild(0).GetComponent<Animator>().Play("RollCamera");
                }
            }
            else
            {
                Camera.GetChild(0).GetComponent<Animator>().enabled = false;
            }
            //Shake
            CameraShake();
        }
        else
        {
            Camera.GetChild(0).GetComponent<Animator>().enabled = false;
            if (FirstPerson)
            {
                Quaternion rot = Quaternion.Euler(Camera.eulerAngles.x,Camera.eulerAngles.y,Head.eulerAngles.z);
                Camera.GetChild(0).rotation = rot;
            }
        }
    }
    void CameraShake()
    {
        if(FirstPerson)
        {
            float speed = Mathf.Floor(rb.linearVelocity.sqrMagnitude);
            if (speed > 0)
            {
                if(Vector3.Distance(Camera.GetChild(0).localPosition,CameraShakePos) < 0.01f)
                {
                    float posx = UnityEngine.Random.Range(-ShakeAmount,ShakeAmount);
                    float posy = UnityEngine.Random.Range(-ShakeAmount, ShakeAmount);
                    float rotx = UnityEngine.Random.Range(-ShakeAmount * 100, ShakeAmount * 100);
                    float rotz = UnityEngine.Random.Range(-ShakeAmount * 100, ShakeAmount * 100);
                    CameraShakePos = new Vector3(posx,posy,0);
                    CameraShakeRot = Quaternion.Euler(rotx,0,rotz);
                }
            }
            else
            {
                CameraShakePos = Vector3.zero;
                CameraShakeRot = Quaternion.Euler(0, 0, 0);
            }
        }
        else
        {
            CameraShakePos = Vector3.zero;
            CameraShakeRot = Quaternion.Euler(0, 0, 0);
        }
        Camera.GetChild(0).localPosition = Vector3.Lerp(Camera.GetChild(0).localPosition, CameraShakePos,ShakeAmount * Speed * 0.5f);
        Camera.GetChild(0).localRotation = Quaternion.Lerp(Camera.GetChild(0).localRotation, CameraShakeRot, ShakeAmount * Speed * 0.1f);
    }
    void FX()
    {
        //Dust
        if(Grounded && MoveVector != Vector3.zero)
        {
            Dust.Play();
        }
        if(!Grounded || MoveVector == Vector3.zero || Grabbing)
        {
            Dust.Stop();
        }
        RaycastHit hit;
        if(Physics.Raycast(Spine.position,Vector3.down,out hit))
        {
            if(hit.transform.GetComponent<Renderer>())
            {
                Texture2D tex = hit.transform.GetComponent<Renderer>().sharedMaterial.mainTexture as Texture2D;
                if (tex != null)
                {
                    int x = Mathf.FloorToInt(hit.point.x / hit.transform.localScale.x * tex.width);
                    int z = Mathf.FloorToInt(hit.point.z / hit.transform.localScale.z * tex.height);
                    Color color = tex.GetPixel(x, z).linear;
                    color.a = 0.3f;
                    Dust.GetComponent<ParticleSystemRenderer>().material.color = color;
                }
                else
                {
                    Dust.GetComponent<ParticleSystemRenderer>().material.color = Color.white;
                }
            }
            else
            {
                Dust.GetComponent<ParticleSystemRenderer>().material.color = Color.white;
            }
        }
    }
    void SoundFX()
    {
        if(!Idle && !Sliding && !Rolling && !JumpingOver)
        {
            if (Ground != null)
            {
                if (!GetComponent<AudioSource>().isPlaying)
                {
                    GetComponent<AudioSource>().Play();
                }
                AudioClip audio = null;
                if(Ground.GetComponent<Renderer>())
                {
                    if (MaterialsSoundsDictionary.ContainsKey(Ground.GetComponent<Renderer>().sharedMaterial))
                    {
                        MaterialsSoundsDictionary.TryGetValue(Ground.GetComponent<Renderer>().sharedMaterial, out audio);
                    }
                }
                else
                {
                    audio = null;
                }
                if (GetComponent<AudioSource>().clip != audio)
                {
                    GetComponent<AudioSource>().clip = audio;
                }
            }
            else
            {
                if (GetComponent<AudioSource>().isPlaying)
                {
                    GetComponent<AudioSource>().Stop();
                }
            }
        }
        else
        {
            if (GetComponent<AudioSource>().isPlaying)
            {
                GetComponent<AudioSource>().Stop();
            }
        }
        if(Grabbing)
        {
            GetComponent<AudioSource>().Stop();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.impulse.sqrMagnitude > 50)
        {
            Ragdoll(true);
        }
    }
    void Ragdoll(bool yes)
    {
        if (yes)
        {
            ragdoll = true;
            Anim.enabled = false;
            Collider.enabled = false;
            for (int i = 0; i < RagdollParts.ToArray().Length; i++)
            {
                RagdollParts[i].GetComponent<Collider>().enabled = true;
                RagdollParts[i].GetComponent<Rigidbody>().isKinematic = false;
            }
            rb.isKinematic = true;
        }
        else
        {
            for (int i = 0; i < RagdollParts.ToArray().Length; i++)
            {
                RagdollParts[i].GetComponent<Collider>().enabled = false;
                RagdollParts[i].GetComponent<Rigidbody>().isKinematic = true;
                if(i == RagdollParts.ToArray().Length - 1)
                {
                    Hip.localRotation = Quaternion.Euler(90, 0, 0);
                    transform.position = Hip.position;
                    Collider.enabled = true;
                    Anim.enabled = true;
                    StartCoroutine(StandingUp());
                    rb.isKinematic = false;
                    Hip.parent.transform.localPosition = new Vector3(-0.00419999985f, 0.00600000005f, 0.0260000005f);
                    Hip.localPosition = new Vector3(0f, 0.0381209143f, 1.09165502f);
                    Head.parent.parent.transform.localPosition = new Vector3(-1.4493709e-07f, 0.275578707f, -1.2435914e-05f);
                }
            }
        }
    }
    IEnumerator StandingUp()
    {
        ChangeAnimation("StandingUp");
        yield return new WaitForSeconds(TimeToStandUp);
        ragdoll = false;
    }

    void SetRigWeight(Transform rigTransform, float weight)
    {
        if (rigTransform == null)
        {
            return;
        }

        Component[] components = rigTransform.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component.GetType().Name != "Rig")
            {
                continue;
            }

            System.Reflection.PropertyInfo weightProperty = component.GetType().GetProperty("weight");
            if (weightProperty != null && weightProperty.CanWrite)
            {
                weightProperty.SetValue(component, weight, null);
                return;
            }

            System.Reflection.FieldInfo weightField = component.GetType().GetField("weight");
            if (weightField != null)
            {
                weightField.SetValue(component, weight);
            }

            return;
        }
    }
}
