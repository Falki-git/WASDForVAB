using KSP.OAB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx.Logging;
using HarmonyLib;

namespace KSPTestMod
{
    public class WASDPatches
    {
        public class WASDPatchesState
        {
            public float yaw = 0;
            public float pitch = 0;
            public Vector3d lookDirection;
            public ManualLogSource Logger;
            public float movementSpeed = 20.5f;
            public ObjectAssemblyCameraManager cameraManager;
            public Vector3d cameraPos = new Vector3d();

            public void OnLoad(ObjectAssemblyCameraManager cameraManager, ManualLogSource Logger)
            {
                Logger.LogInfo("WASDPatches::OnLoad was called");
                this.Logger = Logger;
                this.cameraManager = cameraManager;
                pitch = cameraManager.CameraGimbal.transform.rotation.eulerAngles.x;
                yaw = cameraManager.CameraGimbal.transform.rotation.eulerAngles.z;
                Logger.LogInfo("Pitch: " + pitch);
                Logger.LogInfo("Yaw: " + yaw);
            }

            public void OnMove(Vector3d inputVector, float deltaTime)
            {
                if (cameraManager != null)
                {
                    Vector3d forward = cameraManager.gimbalTransform.transform.forward;
                    Vector3d right = -Vector3d.Cross(forward, Vector3d.up);
                    Vector3d up = Vector3d.up;
                    Vector3d movement = forward * inputVector.z + right * inputVector.x + up * inputVector.y;
                    movement *= deltaTime * movementSpeed;
                    cameraManager.gimbalTransform.position = cameraManager.gimbalTransform.position + movement;
                }
            }
        }

        public static WASDPatchesState patchState = new WASDPatchesState();

        [HarmonyPatch(typeof(ObjectAssemblyPlacementTool), "ProcessInputCameraRotation")]
        class ProcessInputCameraRotationPatch
        {
            static bool Prefix(Vector3 orbitTargetPos, float prevYaw, float prevPitch, float deltaYaw, float deltaPitch, float distance, ref Quaternion lookRotation, ref Vector3 lookDirection, ref Vector3 lookPosition)
            {
                if (patchState.Logger == null)
                {
                    return true;
                }

                patchState.Logger.LogInfo("Euler: " + patchState.cameraManager.gimbalTransform.transform.eulerAngles);
                var currentPitch = patchState.cameraManager.gimbalTransform.transform.eulerAngles.x;
                var currentYaw = patchState.cameraManager.gimbalTransform.transform.eulerAngles.y;
                var newPitch = currentPitch + deltaPitch;

                if (currentPitch > 180.0f && currentPitch + deltaPitch < 270.0f)
                {
                    newPitch = 270.0f;
                }
                else if (currentPitch < 90.0f && currentPitch + deltaPitch > 89.0f)
                {
                    newPitch = 89.0f;
                }

                var newYaw = currentYaw + deltaYaw;
                patchState.Logger.LogInfo("Pitch: " + newPitch);
                patchState.Logger.LogInfo("Yaw: " + newYaw);
                Vector3d lookDir = new Vector3d(0, 0, 1);
                lookRotation = QuaternionD.AngleAxis(newYaw, Vector3d.up) * QuaternionD.AngleAxis(newPitch, Vector3d.right);
                lookDir = lookRotation * lookDir;
                lookDirection = lookDir;
                lookPosition = patchState.cameraManager.gimbalTransform.position;
                return false;
            }
        }
    }
}