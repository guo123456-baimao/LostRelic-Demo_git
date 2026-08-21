using Cinemachine;
using UnityEngine;

namespace LostRelic
{
    public static class CinemachineCameraRig
    {
        public static Transform AttachFirstPerson(
            GameObject renderCamera,
            Transform player,
            float eyeHeight)
        {
            if (renderCamera == null || player == null)
            {
                return null;
            }

            if (renderCamera.GetComponent<CinemachineBrain>() == null)
            {
                renderCamera.AddComponent<CinemachineBrain>();
            }

            var vcamGo = new GameObject("PlayerVirtualCamera");
            var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = 10;
            vcam.Follow = null;
            vcam.LookAt = null;
            vcam.m_Lens.FieldOfView = 70f;

            var pov = vcam.AddCinemachineComponent<CinemachinePOV>();
            pov.m_HorizontalAxis.m_InputAxisName = "Mouse X";
            pov.m_HorizontalAxis.m_MaxSpeed = 300f;
            pov.m_HorizontalAxis.m_AccelTime = 0f;
            pov.m_HorizontalAxis.m_DecelTime = 0f;
            pov.m_HorizontalAxis.m_MinValue = -180f;
            pov.m_HorizontalAxis.m_MaxValue = 180f;
            pov.m_HorizontalAxis.m_Wrap = true;

            pov.m_VerticalAxis.m_InputAxisName = "Mouse Y";
            pov.m_VerticalAxis.m_MaxSpeed = 250f;
            pov.m_VerticalAxis.m_MinValue = -85f;
            pov.m_VerticalAxis.m_MaxValue = 85f;
            pov.m_VerticalAxis.m_AccelTime = 0f;
            pov.m_VerticalAxis.m_DecelTime = 0f;

            vcamGo.transform.position = player.position + Vector3.up * eyeHeight;
            return vcamGo.transform;
        }
    }
}
