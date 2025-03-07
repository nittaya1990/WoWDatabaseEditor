﻿using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SceneBuffer
    {
        public Matrix ViewMatrix;
        public Matrix ProjectionMatrix;
        public Vector4 CameraPosition;
        public Vector4 LightDirection;
        public Vector3 LightColor;
        public float LightIntensity;
        public Vector4 AmbientColor;
        public Vector3 LightPosition;
        public float Align0;
        public Vector4 SecondaryLightDirection;
        public Vector3 SecondaryLightColor;
        public float SecondaryLightIntensity;
        public float ScreenWidth;
        public float ScreenHeight;
        public float Time;

        public float Align1;
        public float Align2;
        public float Align3;
        
        public float Align4;
        public float Align5;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectBuffer
    {
        public Matrix WorldMatrix;
        public Matrix InverseWorldMatrix;
    }
}
