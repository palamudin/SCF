using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Builder
{
    public class Elevator : MonoBehaviour
    {
        [Serializable]
        public class Floor
        {
            public Transform LeftDoor;
            [HideInInspector] public Vector3 LeftDoorInitialPos;
            public Transform RightDoor;
            [HideInInspector] public Vector3 RightDoorInitialPos;
            public float Height;
        }
        public List<Floor> Floors;
        public Transform LeftDoor;
        public Transform RightDoor;
        public int CurrentChosenFloor;
        private int CurrentFloor;
        private int PreviousCurrentFloor;
        private enum DoorState { Closed, Closing, Opening, Opened }
        private DoorState DoorsState;
        private Vector3 LeftDoorInitionalPos;
        private Vector3 RightDoorInitionalPos;
        private bool AlreadyOpened = false;
        private void Start()
        {
            foreach (Floor fl in Floors)
            {
                fl.LeftDoorInitialPos = LeftDoor.position;
                fl.RightDoorInitialPos = RightDoor.position;
            }
            PreviousCurrentFloor = CurrentFloor;
            LeftDoorInitionalPos = LeftDoor.position;
            RightDoorInitionalPos = RightDoor.position;
        }
        void Update()
        {
            CurrentChosenFloor = Mathf.Clamp(CurrentChosenFloor, 0, Floors.Count - 1);
            if (PreviousCurrentFloor != CurrentChosenFloor)
            {
                AlreadyOpened = false;
                StartCoroutine(CloseDoors());
            }
            PreviousCurrentFloor = CurrentChosenFloor;
            if (DoorsState == DoorState.Closed)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, Floors[CurrentChosenFloor].Height + transform.lossyScale.y * 0.5f, transform.position.z), 0.01f);
            }
            if (DoorsState == DoorState.Closing)
            {
                LeftDoor.position = Vector3.Lerp(LeftDoor.position, new Vector3(LeftDoorInitionalPos.x, LeftDoor.position.y, LeftDoorInitionalPos.z), 0.1f);
                RightDoor.position = Vector3.Lerp(RightDoor.position, new Vector3(RightDoorInitionalPos.x, RightDoor.position.y, RightDoorInitionalPos.z), 0.1f);
                Floors[CurrentFloor].LeftDoor.position = Vector3.Lerp(Floors[CurrentFloor].LeftDoor.position, new Vector3(Floors[CurrentFloor].LeftDoorInitialPos.x, Floors[CurrentFloor].LeftDoor.position.y, Floors[CurrentFloor].LeftDoorInitialPos.z), 0.1f);
                Floors[CurrentFloor].RightDoor.position = Vector3.Lerp(Floors[CurrentFloor].RightDoor.position, new Vector3(Floors[CurrentFloor].RightDoorInitialPos.x, Floors[CurrentFloor].RightDoor.position.y, Floors[CurrentFloor].RightDoorInitialPos.z), 0.1f);
            }
            if (DoorsState == DoorState.Opening)
            {
                CurrentFloor = CurrentChosenFloor;
                LeftDoor.position = Vector3.Lerp(LeftDoor.position, new Vector3(LeftDoorInitionalPos.x, LeftDoor.position.y, LeftDoorInitionalPos.z) - LeftDoor.TransformDirection(new Vector3(LeftDoor.lossyScale.x, 0, 0)), 0.1f);
                RightDoor.position = Vector3.Lerp(RightDoor.position, new Vector3(RightDoorInitionalPos.x, RightDoor.position.y, RightDoorInitionalPos.z) + RightDoor.TransformDirection(new Vector3(RightDoor.lossyScale.x, 0, 0)), 0.1f);
                Floors[CurrentFloor].LeftDoor.position = Vector3.Lerp(Floors[CurrentFloor].LeftDoor.position, new Vector3(Floors[CurrentFloor].LeftDoorInitialPos.x, Floors[CurrentFloor].LeftDoor.position.y, Floors[CurrentFloor].LeftDoorInitialPos.z) - Floors[CurrentFloor].LeftDoor.TransformDirection(new Vector3(Floors[CurrentFloor].LeftDoor.lossyScale.x, 0, 0)), 0.1f);
                Floors[CurrentFloor].RightDoor.position = Vector3.Lerp(Floors[CurrentFloor].RightDoor.position, new Vector3(Floors[CurrentFloor].RightDoorInitialPos.x, Floors[CurrentFloor].RightDoor.position.y, Floors[CurrentFloor].RightDoorInitialPos.z) + Floors[CurrentFloor].RightDoor.TransformDirection(new Vector3(Floors[CurrentFloor].RightDoor.lossyScale.x, 0, 0)), 0.1f);
            }
            if (Vector3.Distance(transform.position, new Vector3(transform.position.x, Floors[CurrentChosenFloor].Height + transform.lossyScale.y * 0.5f, transform.position.z)) < 0.01f && !AlreadyOpened)
            {
                AlreadyOpened = true;
                StartCoroutine(OpenDoors());
            }
        }
        IEnumerator CloseDoors()
        {
            DoorsState = DoorState.Closing;
            yield return new WaitForSeconds(2);
            DoorsState = DoorState.Closed;
        }
        IEnumerator OpenDoors()
        {
            DoorsState = DoorState.Opening;
            yield return new WaitForSeconds(2);
            DoorsState = DoorState.Opened;
            yield return new WaitForSeconds(2);
            StartCoroutine(CloseDoors());
        }
    }
}