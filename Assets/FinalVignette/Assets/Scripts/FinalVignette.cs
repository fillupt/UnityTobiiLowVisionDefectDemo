// Copyright (c) 2018 Jakub Boksansky - All Rights Reserved
// Final Vignette Unity Plugin 1.0

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Reflection;

namespace Wilberforce.FinalVignette
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [HelpURL("https://projectwilberforce.github.io/finalvignette/manual/")]
    [AddComponentMenu("Image Effects/Rendering/Final Vignette")]
    public class FinalVignette : FinalVignetteCommandBuffer
    {
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            this.PerformOnRenderImage(source, destination);
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(FinalVignette))]
    public class CameraLensEditorImageEffect : FinalVignetteEditor { }

#endif
}
