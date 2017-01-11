﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPWheel
{
    public class KSPWheelTracks : KSPWheelMotor
    {

        [KSPField]
        public float trackLength = 10f;

        [KSPField]
        public int trackDir = 1;

        [KSPField]
        public float trackSpeedMult = 1.0f;

        private SkinnedMeshRenderer smr;
        private Vector2 offset = Vector2.zero;
        private Material mat;
        private float trackRPM = 0f;

        internal override void postWheelCreated()
        {
            base.postWheelCreated();
            smr = part.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                mat = smr.material;
                if (mat != null)
                {
                    Vector2 scaling = mat.mainTextureScale;
                    scaling.x *= trackDir;
                    mat.SetTextureScale("_MainTex", scaling);
                    mat.SetTextureScale("_BumpMap", scaling);
                }
            }
        }

        internal override void preWheelFrameUpdate()
        {
            base.preWheelFrameUpdate();
            if (mat != null)
            {
                float offsetAmount = (((-trackRPM * 2f * Mathf.PI) / 60f) * Time.deltaTime * trackDir) / trackLength;
                offset.x += offsetAmount;
                mat.SetTextureOffset("_MainTex", offset);
                mat.SetTextureOffset("_BumpMap", offset);
            }
        }

        /// <summary>
        /// This method sets each wheel torque to that which would be needed
        /// exert the same linear velocity change on each wheel in the group.
        /// The moment of inertia of each wheel determines a wheels share
        /// of the torque.
        /// </summary>
        protected override void updateMotor()
        {
            base.updateMotor();
            //TODO remove ALL allocations, assign permanent calc variables??
            float factorSum = 0f;
            float totalSystemTorque = 0f;
            float totalBrakeTorque = this.wheel.brakeTorque;
            float totalMotorTorque = torqueOutput;
            float[] factors = new float[controller.wheelData.Length];//TODO remove this allocation, can be moved to an initialized-once on wheel-creation scenario
            float[] shares = new float[controller.wheelData.Length];
            KSPWheelCollider wheel;
            int len = controller.wheelData.Length;
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                factors[i] = wheel.momentOfInertia / wheel.radius;
                factorSum += factors[i];
                totalSystemTorque += controller.wheelData[i].wheel.angularVelocity * controller.wheelData[i].wheel.momentOfInertia;
            }
            for (int i = 0; i < len; i++)
            {
                wheel = controller.wheelData[i].wheel;
                shares[i] = factors[i] / factorSum;
                wheel.motorTorque = shares[i] * totalMotorTorque;
                wheel.brakeTorque = shares[i] * totalBrakeTorque;
                wheel.angularVelocity = shares[i] * totalSystemTorque / wheel.momentOfInertia;
            }
            trackRPM = this.wheel.rpm * trackSpeedMult;
        }

    }
}
