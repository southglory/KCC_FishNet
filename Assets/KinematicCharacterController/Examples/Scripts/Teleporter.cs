using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using KinematicCharacterController.Offline;

namespace KinematicCharacterController.Examples
{
    public class Teleporter : MonoBehaviour
    {
        public Teleporter TeleportTo;

        public UnityAction<OfflineCharacterController> OnCharacterTeleport;

        public bool isBeingTeleportedTo { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (!isBeingTeleportedTo)
            {
                OfflineCharacterController cc = other.GetComponent<OfflineCharacterController>();
                if (cc)
                {
                    cc.Motor.SetPositionAndRotation(TeleportTo.transform.position, TeleportTo.transform.rotation);

                    if (OnCharacterTeleport != null)
                    {
                        OnCharacterTeleport(cc);
                    }
                    TeleportTo.isBeingTeleportedTo = true;
                }
            }

            isBeingTeleportedTo = false;
        }
    }
}