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
    public class FinalVignetteCommandBuffer : MonoBehaviour
    {

        #region Effect Parameters

        #region Vignette

        [Range(0.0f, 5.0f)]
        public float VignetteFalloff = 2.2f;

        [Range(0.0f, 1.0f)]
        public float VignetteOuterValue = 1.0f;

        [Range(0.0f, 1.0f)]
        public float VignetteInnerValue = 0.0f;

        public float VignetteInnerValueDistance = 0.0f;
        public float VignetteOuterValueDistance = 0.7f;

        public VignetteModeType VignetteMode = VignetteModeType.Standard;

        public Vector2 VignetteCenter = new Vector2(0.5f, 0.5f);

        public Color VignetteInnerColor = new Color(0, 0, 0, 0);

        public Color VignetteOuterColor = new Color(0, 0, 0, 1);

        public float VignetteInnerSaturation = 1.0f;
        public float VignetteOuterSaturation = 0.0f;

        public bool VignetteDebugEnabled = false;
        public bool IsAnamorphicVignette = false;

        public bool EnableSaturationVignette = false;

        #endregion

        #region Rendering Pipeline

        public Integration IntegrationType = Integration.CommandBuffer;

        public CameraEventType IntegrationStage = CameraEventType.AfterImageEffects;

        #endregion

        #endregion

        #region Effect Enums

        public enum VignetteModeType
        {
            Standard = 1,
            CustomColors = 2,
        }

        public enum Integration
        {
            ImageEffect = 1,
            CommandBuffer = 2,
        }

        public enum DebugModeType
        {
            None = 0,
            Vignette = 1
        }

        private enum ShaderPass
        {
            VignetteOnlyNoSaturationStandardMode,
            VignetteOnlyNoSaturationStandardModeBlend,
            VignetteOnlySaturationStandardMode,
            VignetteOnlySaturationStandardModeBlend,
            VignetteOnlyNoSaturationCustomMode,
            VignetteOnlyNoSaturationCustomModeBlend,
            VignetteOnlySaturationCustomMode,
            VignetteOnlySaturationCustomModeBlend,
            DebugDisplayPassStandard,
            DebugDisplayPassCustom
        }

        public enum CameraEventType
        {
            BeforeImageEffects,
            AfterImageEffects,
            AfterEverything
        }

        #endregion

        #region Effect Private Variables

        #region Command Buffer Variables

        private Dictionary<CameraEvent, CommandBuffer> cameraEventsRegistered = new Dictionary<CameraEvent, CommandBuffer>();
        private bool isCommandBufferAlive = false;

        private int destinationWidth;
        private int destinationHeight;

        private bool onDestroyCalled = false;

        private CameraEvent lastCameraEvent;

        #endregion

        #region Shader, Material, Camera

        public Shader FinalVignetteShader;

        private Camera myCamera = null;
        private bool isSupported;
        private Material FinalVignetteMaterial;

        #endregion

        #region Previous controls values

        public bool isHDR;
        public bool isSPSR;

        private bool lastIsHDR;
        private bool lastIsSPSR;
        private bool lastVignetteDebugEnabled;
        private bool lastEnableSaturationVignette;
        private VignetteModeType lastVignetteMode;

        int lastScreenTextureHeight;
        int lastScreenTextureWidth;

        int lastDestinationWidth;
        int lastDestinationHeight;

        #endregion

        #region Foldouts

        // Foldouts need to be public so that editor remembers their state

        public bool pipelineFoldout = false;
        public bool aboutFoldout = false;

        #endregion

        #endregion

        #region Unity Events

        void Start()
        {
            if (FinalVignetteShader == null) FinalVignetteShader = Shader.Find("Hidden/Wilberforce/FinalVignette");
            if (FinalVignetteShader == null)
            {
                ReportError("Could not locate Final Vignette Shader. Make sure there is 'FinalVignette.shader' file added to the project.");
                isSupported = false;
                enabled = false;
                return;
            }

            //if (!SystemInfo.supportsImageEffects || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) || SystemInfo.graphicsShaderLevel < 30)
            //{
            //    if (!SystemInfo.supportsImageEffects) ReportError("System does not support image effects.");

            //    isSupported = false;
            //    enabled = false;
            //    return;
            //}

            EnsureMaterials();

            if (!FinalVignetteMaterial || FinalVignetteMaterial.passCount != Enum.GetValues(typeof(ShaderPass)).Length)
            {
                ReportError("Could not create shader.");
                isSupported = false;
                enabled = false;
                return;
            }

            isSupported = true;
        }

        void OnEnable()
        {
            this.myCamera = GetComponent<Camera>();
            TeardownCommandBuffer();
        }

        void OnPreRender()
        {
            EnsureEffectIntegrationVersion();

            EnsureMaterials();

            TrySetUniforms();
            EnsureCommandBuffer(CheckSettingsChanges());

        }

        void OnPostRender()
        {

            if (myCamera == null || myCamera.activeTexture == null) return;

            // Check if cmd. buffer was created with correct target texture sizes and rebuild if necessary
            if (this.destinationWidth != myCamera.activeTexture.width || this.destinationHeight != myCamera.activeTexture.height || !isCommandBufferAlive)
            {
                this.destinationWidth = myCamera.activeTexture.width;
                this.destinationHeight = myCamera.activeTexture.height;

                TeardownCommandBuffer();
                EnsureCommandBuffer();
            }
            else
            {
                // Remember destination texture dimensions for use in command buffer (there are different values in camera.pixelWidth/Height which do not work in Single pass stereo)
                this.destinationWidth = myCamera.activeTexture.width;
                this.destinationHeight = myCamera.activeTexture.height;
            }

        }

        void OnDisable()
        {
            TeardownCommandBuffer();
        }

        void OnDestroy()
        {
            TeardownCommandBuffer();
            onDestroyCalled = true;
        }

        #endregion

        #region Image Effect Implementation

        protected void PerformOnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!isSupported || !FinalVignetteShader.isSupported)
            {
                enabled = false;
                return;
            }

            EnsureMaterials();

            if (ShouldUseCommandBuffer())
            {
                return; //< Return here, drawing will be done in command buffer
            }
            else
            {
                TeardownCommandBuffer();
            }

            int screenTextureWidth = source.width;
            int screenTextureHeight = source.height;

            DoEffect(null, source, destination, -1, screenTextureWidth, screenTextureHeight);

        }

        #endregion

        #region Command Buffer Implementation

        private bool ShouldUseCommandBuffer()
        {
            if (IntegrationType != Integration.ImageEffect) return true;
            return false;
        }

        private void EnsureCommandBuffer(bool settingsDirty = false)
        {
            if ((!settingsDirty && isCommandBufferAlive) || !ShouldUseCommandBuffer()) return;
            if (onDestroyCalled) return;

            try
            {
                CreateCommandBuffer();
                lastCameraEvent = GetCameraEvent(IntegrationStage);
                isCommandBufferAlive = true;
            }
            catch (Exception ex)
            {
                ReportError("There was an error while trying to create command buffer. " + ex.Message);
            }
        }

        private void CreateCommandBuffer()
        {
            CommandBuffer commandBuffer;

            FinalVignetteMaterial = null;
            EnsureMaterials();

            TrySetUniforms();
            CameraEvent cameraEvent = GetCameraEvent(IntegrationStage);

            if (cameraEventsRegistered.TryGetValue(cameraEvent, out commandBuffer))
            {
                commandBuffer.Clear();
            }
            else
            {
                commandBuffer = new CommandBuffer();
                myCamera.AddCommandBuffer(cameraEvent, commandBuffer);

                commandBuffer.name = "Final Vignette";

                // Register
                cameraEventsRegistered[cameraEvent] = commandBuffer;
            }

            int cameraWidth = this.destinationWidth;
            int cameraHeight = this.destinationHeight;

            if (cameraWidth <= 0) cameraWidth = myCamera.pixelWidth;
            if (cameraHeight <= 0) cameraHeight = myCamera.pixelHeight;

            int screenTextureWidth = cameraWidth;
            int screenTextureHeight = cameraHeight;

            int? screenTexture = null;
            if (EnableSaturationVignette || VignetteMode == VignetteModeType.CustomColors)
            {
                RenderTextureFormat screenTextureFormat = isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

                screenTexture = Shader.PropertyToID("screenTextureRT");
                commandBuffer.GetTemporaryRT(screenTexture.Value, cameraWidth, cameraHeight, 0, FilterMode.Bilinear, screenTextureFormat, RenderTextureReadWrite.Linear);

                // Remember input
                commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, screenTexture.Value);
            }

            // Perform actual effects
            DoEffect(commandBuffer, null, null, screenTexture, screenTextureWidth, screenTextureHeight);

            // Cleanup
            if (screenTexture != null) commandBuffer.ReleaseTemporaryRT(screenTexture.Value);
        }

        private void TeardownCommandBuffer()
        {
            if (!isCommandBufferAlive) return;

            try
            {
                isCommandBufferAlive = false;

                if (myCamera != null)
                {
                    foreach (var e in cameraEventsRegistered)
                    {
                        myCamera.RemoveCommandBuffer(e.Key, e.Value);
                    }
                }

                cameraEventsRegistered.Clear();
                FinalVignetteMaterial = null;
                EnsureMaterials();
            }
            catch (Exception ex)
            {
                ReportError("There was an error while trying to destroy command buffer. " + ex.Message);
            }
        }

        private CameraEvent GetCameraEvent(CameraEventType cameraEvent)
        {

            switch (cameraEvent)
            {
                case CameraEventType.BeforeImageEffects:
                    return CameraEvent.BeforeImageEffects;
                case CameraEventType.AfterImageEffects:
                    return CameraEvent.AfterImageEffects;
                case CameraEventType.AfterEverything:
                    return CameraEvent.AfterEverything;
                default:
                    return CameraEvent.AfterImageEffects;
            }
        }

        #endregion

        #region Shader Utilities

        private void TrySetUniforms()
        {
            if (FinalVignetteMaterial == null) return;

            FinalVignetteMaterial.SetInt("isSaturationMode", EnableSaturationVignette ? 1 : 0);
            FinalVignetteMaterial.SetInt("needsYFlip", (IntegrationType == Integration.CommandBuffer && IntegrationStage != CameraEventType.BeforeImageEffects && !myCamera.stereoEnabled) ? 1 : 0);
            FinalVignetteMaterial.SetInt("needsStereoAdjust", (IntegrationType == Integration.CommandBuffer && IntegrationStage == CameraEventType.AfterEverything && isSPSR) ? 1 : 0);

            int screenTextureWidth = myCamera.pixelWidth;
            int screenTextureHeight = myCamera.pixelHeight;

            float aspectRatio = ((float)screenTextureHeight) / ((float)screenTextureWidth);
            FinalVignetteMaterial.SetFloat("aspectRatio", aspectRatio);

            if (screenTextureWidth != lastScreenTextureWidth ||
                screenTextureHeight != lastScreenTextureHeight)
            {

                lastScreenTextureWidth = screenTextureWidth;
                lastScreenTextureHeight = screenTextureHeight;
            }

            // Vignette
            FinalVignetteMaterial.SetFloat("vignetteFalloff", VignetteFalloff);
            FinalVignetteMaterial.SetFloat("vignetteMax", VignetteOuterValue);
            FinalVignetteMaterial.SetFloat("vignetteMin", VignetteInnerValue);
            FinalVignetteMaterial.SetFloat("vignetteMaxDistance", VignetteOuterValueDistance);
            FinalVignetteMaterial.SetFloat("vignetteMinDistance", VignetteInnerValueDistance);

            FinalVignetteMaterial.SetVector("vignetteCenter", VignetteCenter);
            FinalVignetteMaterial.SetVector("vignetteInnerColor", VignetteInnerColor);
            FinalVignetteMaterial.SetVector("vignetteOuterColor", VignetteOuterColor);
            FinalVignetteMaterial.SetFloat("vignetteSaturationMin", VignetteInnerSaturation);
            FinalVignetteMaterial.SetFloat("vignetteSaturationMax", VignetteOuterSaturation);
            FinalVignetteMaterial.SetInt("isAnamorphicVignette", IsAnamorphicVignette ? 1 : 0);
            FinalVignetteMaterial.SetInt("vignetteMode", (int)VignetteMode);

        }

        private void EnsureMaterials()
        {
            if (FinalVignetteShader == null) FinalVignetteShader = Shader.Find("Hidden/Wilberforce/FinalVignette");

            if (!FinalVignetteMaterial && FinalVignetteShader.isSupported)
            {
                FinalVignetteMaterial = CreateMaterial(FinalVignetteShader);
            }

            if (!FinalVignetteShader.isSupported)
            {
                ReportError("Could not create shader (Shader not supported).");
            }
        }

        private static Material CreateMaterial(Shader shader)
        {
            if (!shader) return null;

            Material m = new Material(shader);
            m.hideFlags = HideFlags.HideAndDontSave;

            return m;
        }

        private static void DestroyMaterial(Material mat)
        {
            if (mat)
            {
                DestroyImmediate(mat);
                mat = null;
            }
        }

        #endregion

        #region Effect Implementation Utilities

        protected void EnsureEffectIntegrationVersion()
        {
            if (ShouldUseCommandBuffer() && (this is FinalVignetteCommandBuffer) && !(this is FinalVignette)) return;
            if (!ShouldUseCommandBuffer() && (this is FinalVignette)) return;

            var allComponents = GetComponents<Component>();
            var parameters = GetParameters();

            int oldComponentIndex = -1;
            Component newComponent = null;

            for (int i = 0; i < allComponents.Length; i++)
            {
                if (ShouldUseCommandBuffer() && (allComponents[i] == this))
                {

                    var oldGameObject = gameObject;
                    DestroyImmediate(this);
                    newComponent = oldGameObject.AddComponent<FinalVignetteCommandBuffer>();
                    (newComponent as FinalVignetteCommandBuffer).SetParameters(parameters);
                    oldComponentIndex = i;
                    break;
                }

                if (!ShouldUseCommandBuffer() && ((allComponents[i] == this)))
                {
                    var oldGameObject = gameObject;
                    TeardownCommandBuffer();
                    DestroyImmediate(this);
                    newComponent = oldGameObject.AddComponent<FinalVignette>();
                    (newComponent as FinalVignette).SetParameters(parameters);
                    oldComponentIndex = i;
                    break;
                }
            }

            if (oldComponentIndex >= 0 && newComponent != null)
            {
#if UNITY_EDITOR
                allComponents = newComponent.gameObject.GetComponents<Component>();
                int currentIndex = 0;

                for (int i = 0; i < allComponents.Length; i++)
                {
                    if (allComponents[i] == newComponent)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < currentIndex - oldComponentIndex; i++)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(newComponent);
                }
#endif
            }
        }

        private bool CheckSettingsChanges()
        {
            bool settingsDirty = false;

            if (GetCameraEvent(IntegrationStage) != lastCameraEvent)
            {
                TeardownCommandBuffer();
            }

            isHDR = isCameraHDR(myCamera);
            if (isHDR != lastIsHDR)
            {
                lastIsHDR = isHDR;
                settingsDirty = true;
            }

#if UNITY_EDITOR
            isSPSR = isCameraSPSR(myCamera);
            if (isSPSR != lastIsSPSR)
            {
                lastIsSPSR = isSPSR;
                settingsDirty = true;
            }
#endif

            if (VignetteDebugEnabled != lastVignetteDebugEnabled)
            {
                lastVignetteDebugEnabled = VignetteDebugEnabled;
                settingsDirty = true;
            }

            if (EnableSaturationVignette != lastEnableSaturationVignette)
            {
                lastEnableSaturationVignette = EnableSaturationVignette;
                settingsDirty = true;
            }

            if (VignetteMode != lastVignetteMode)
            {
                lastVignetteMode = VignetteMode;
                settingsDirty = true;
            }

            if (destinationWidth != lastDestinationWidth)
            {
                lastDestinationWidth = destinationWidth;
                settingsDirty = true;
            }

            if (destinationHeight != lastDestinationHeight)
            {
                lastDestinationHeight = destinationHeight;
                settingsDirty = true;
            }

            return settingsDirty;
        }

        #endregion

        #region Unity Utilities

        private void ReportError(string error)
        {
            if (Debug.isDebugBuild) Debug.LogError("Wilberforce Final Vignette Effect Error: " + error);
        }

        private void ReportWarning(string error)
        {
            if (Debug.isDebugBuild) Debug.LogWarning("Wilberforce Final Vignette Effect Warning: " + error);
        }

        private bool isCameraSPSR(Camera camera)
        {
            if (camera == null) return false;

#if UNITY_5_5_OR_NEWER
            if (camera.stereoEnabled)
            {
#if UNITY_2017_2_OR_NEWER

                return (UnityEngine.XR.XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes);
#else

#if !UNITY_WEBGL
#if UNITY_EDITOR
                if (camera.stereoEnabled && PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass)
                    return true;
#endif
#endif

#endif
            }
#endif

            return false;
        }

        private bool isCameraHDR(Camera camera)
        {

#if UNITY_5_6_OR_NEWER
            if (camera != null) return camera.allowHDR;
#else
            if (camera != null) return camera.hdr;
#endif
            return false;
        }

        protected List<KeyValuePair<FieldInfo, object>> GetParameters()
        {
            var result = new List<KeyValuePair<FieldInfo, object>>();

            var fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                result.Add(new KeyValuePair<FieldInfo, object>(field, field.GetValue(this)));
            }

            return result;
        }

        protected void SetParameters(List<KeyValuePair<FieldInfo, object>> parameters)
        {
            foreach (var parameter in parameters)
            {
                parameter.Key.SetValue(this, parameter.Value);
            }
        }

        #endregion

        #region Effect Implementations

        int GetShaderPass(bool canBlend)
        {
            if (VignetteDebugEnabled)
                if (VignetteMode == VignetteModeType.Standard)
                    return (int)ShaderPass.DebugDisplayPassStandard;
                else
                    return (int)ShaderPass.DebugDisplayPassCustom;

            if (canBlend)
            {
                if (EnableSaturationVignette)
                {
                    if (VignetteMode == VignetteModeType.Standard)
                    {
                        return (int)ShaderPass.VignetteOnlySaturationStandardModeBlend;
                    }
                    else
                    {
                        return (int)ShaderPass.VignetteOnlySaturationCustomModeBlend;
                    }
                }
                else
                {
                    if (VignetteMode == VignetteModeType.Standard)
                    {
                        return (int)ShaderPass.VignetteOnlyNoSaturationStandardModeBlend;
                    }
                    else
                    {
                        return (int)ShaderPass.VignetteOnlyNoSaturationCustomModeBlend;
                    }
                }
            }
            else
            {
                if (EnableSaturationVignette)
                {
                    if (VignetteMode == VignetteModeType.Standard)
                    {
                        return (int)ShaderPass.VignetteOnlySaturationStandardMode;
                    }
                    else
                    {
                        return (int)ShaderPass.VignetteOnlySaturationCustomMode;
                    }
                }
                else
                {
                    if (VignetteMode == VignetteModeType.Standard)
                    {
                        return (int)ShaderPass.VignetteOnlyNoSaturationStandardMode;
                    }
                    else
                    {
                        return (int)ShaderPass.VignetteOnlyNoSaturationCustomMode;
                    }
                }
            }

        }

        void DoEffect(CommandBuffer commandBuffer, RenderTexture source, RenderTexture destination, int? cmdBufferScreenTexture, int screenTextureWidth, int screenTextureHeight)
        {

            int passNumber = GetShaderPass(!EnableSaturationVignette && commandBuffer != null && !VignetteDebugEnabled && VignetteMode != VignetteModeType.CustomColors);

            if (commandBuffer != null)
                if (cmdBufferScreenTexture.HasValue)
                    commandBuffer.Blit(cmdBufferScreenTexture.Value, BuiltinRenderTextureType.CameraTarget, FinalVignetteMaterial, passNumber);
                else
                    commandBuffer.Blit(null, BuiltinRenderTextureType.CameraTarget, FinalVignetteMaterial, passNumber);
            else
                Graphics.Blit(source, destination, FinalVignetteMaterial, passNumber);

        }

        #endregion

    }

#if UNITY_EDITOR


    [CustomEditor(typeof(FinalVignetteCommandBuffer))]
    public class FinalVignetteEditorCmdBuffer : FinalVignetteEditor { }

    public class FinalVignetteEditor : Editor
    {

    #region Labels

        private readonly GUIContent pipelineLabelContent = new GUIContent("Rendering Pipeline Settings:", "");
        private readonly GUIContent integrationLabelContent = new GUIContent("Integration Type:", "Way of integration of effect within Unity.");
        private readonly GUIContent integrationStageLabelContent = new GUIContent("Stage:", "Rendering stage at which will be vignette executed.");
        
        private readonly GUIContent anamorphicLensLabel = new GUIContent("Anamorphic Lens (Aspect Ratio):", "Turning on will simulate effect of anamorphic lens.");

        private readonly GUIContent vignetteFalloffLinearityLabelContent = new GUIContent("Falloff Linearity: ", "Falloff curve of vignette intensity (based on distance from image center).");
        private readonly GUIContent vignetteMinMaxLabelContent = new GUIContent("Min/Max Intensity: ", "Maximum brightness value (in the center), minimum brightness value (in the corners).");
        private readonly GUIContent vignetteMinMaxDistanceLabelContent = new GUIContent("Min/Max Distance: ", "Distance from center where minimum/maximum intensity is achieved.");
        private readonly GUIContent vignetteModeLabelContent = new GUIContent("Mode: ", "Vignetting mode - Standard mode adjusts brightness, Saturation mode adjusts color saturation.");
        private readonly GUIContent vignetteInnerSaturationLabelContent = new GUIContent("Center Saturation: ", "Color saturation in the center.");
        private readonly GUIContent vignetteOuterSaturationLabelContent = new GUIContent("Corners Saturation: ", "Color saturation in corners.");
        private readonly GUIContent vignetteInnerColorLabelContent = new GUIContent("Center Color: ", "Color in the center.");
        private readonly GUIContent vignetteOuterColorLabelContent = new GUIContent("Corners Color: ", "Color in corners.");
        private readonly GUIContent vignetteCenterLabelContent = new GUIContent("Center Position: ", "Position of vignetting center on the screen.");
        private readonly GUIContent saturationLabel = new GUIContent("Saturation Adjustment: ", "Enables the adjustment of saturation - WARNING: affects performance.");
        
    #endregion

    #region Unity Utilities

        private Camera camera;

        private void SetIcon()
        {
            try
            {
                Texture2D icon = (Texture2D)Resources.Load("wilberforce_script_icon");
                Type editorGUIUtilityType = typeof(UnityEditor.EditorGUIUtility);
                System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
                object[] args = new object[] { target, icon };
                editorGUIUtilityType.InvokeMember("SetIconForObject", bindingFlags, null, null, args);
            }
            catch (Exception ex)
            {
                if (Debug.isDebugBuild) Debug.Log("Wilberforce Final Vignette Effect Error: There was an exception while setting icon to Wilberforce Final Vignette script: " + ex.Message);
            }
        }

        void OnEnable()
        {

            FinalVignetteCommandBuffer effect = (target as FinalVignetteCommandBuffer);
            camera = effect.GetComponent<Camera>();

            SetIcon();

        }
        
        override public void OnInspectorGUI()
        {
            var script = target as FinalVignetteCommandBuffer;

            EditorGUILayout.Space();

            EditorGUI.indentLevel++;
            script.pipelineFoldout = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), script.pipelineFoldout, pipelineLabelContent, true, EditorStyles.foldout);
            if (script.pipelineFoldout)
            {
                EditorGUI.indentLevel++;
                script.IntegrationType = (FinalVignette.Integration)EditorGUILayout.EnumPopup(integrationLabelContent, script.IntegrationType);
                EditorGUILayout.Space();

                EditorGUI.BeginDisabledGroup(script.IntegrationType == FinalVignetteCommandBuffer.Integration.ImageEffect);
                script.IntegrationStage = (FinalVignette.CameraEventType)EditorGUILayout.EnumPopup(integrationStageLabelContent, script.IntegrationStage);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;

            }

            EditorGUILayout.Space();

            VignetteGUI(script);

            EditorGUILayout.Space();

            script.aboutFoldout = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), script.aboutFoldout, "About", true, EditorStyles.foldout);
            if (script.aboutFoldout)
            {
                EditorGUILayout.HelpBox("Final Vignette v1.0 by Project Wilberforce.\n\nThank you for your purchase and if you have any questions, issues or suggestions, feel free to contact us at <projectwilberforce@gmail.com>.", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Contact Support"))
                {
                    Application.OpenURL("mailto:projectwilberforce@gmail.com");
                }

                if (GUILayout.Button("Asset Store Page"))
                {
                    Application.OpenURL("http://u3d.as/1bZ6"); 
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUI.changed)
            {

                // Mark as dirty
                EditorUtility.SetDirty(target);
            }

            Undo.RecordObject(target, "Wilberforce Final Lens change");
        }
        
        private void VignetteGUI(FinalVignetteCommandBuffer script)
        {

            EditorGUILayout.MinMaxSlider(vignetteMinMaxLabelContent, ref script.VignetteInnerValue, ref script.VignetteOuterValue, 0.0f, 1.0f);

            script.VignetteFalloff = EditorGUILayout.Slider(vignetteFalloffLinearityLabelContent, script.VignetteFalloff, 1.0f, 10.0f);

            EditorGUILayout.Space();

            EditorGUILayout.MinMaxSlider(vignetteMinMaxDistanceLabelContent, ref script.VignetteInnerValueDistance, ref script.VignetteOuterValueDistance, 0.0f, 2.0f);

            EditorGUILayout.Space();
            script.VignetteMode = (FinalVignette.VignetteModeType)EditorGUILayout.EnumPopup(vignetteModeLabelContent, script.VignetteMode);

            if (script.VignetteMode == FinalVignette.VignetteModeType.CustomColors)
            {
                EditorGUI.indentLevel++;
                script.VignetteInnerColor = EditorGUILayout.ColorField(vignetteInnerColorLabelContent, script.VignetteInnerColor);
                script.VignetteOuterColor = EditorGUILayout.ColorField(vignetteOuterColorLabelContent, script.VignetteOuterColor);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            script.EnableSaturationVignette = EditorGUILayout.Toggle(saturationLabel, script.EnableSaturationVignette);
            
            EditorGUI.BeginDisabledGroup(!script.EnableSaturationVignette);

            EditorGUI.indentLevel++;
            script.VignetteInnerSaturation = EditorGUILayout.Slider(vignetteInnerSaturationLabelContent, script.VignetteInnerSaturation, 0.0f, 1.0f);
            script.VignetteOuterSaturation = EditorGUILayout.Slider(vignetteOuterSaturationLabelContent, script.VignetteOuterSaturation, 0.0f, 1.0f);
            EditorGUI.indentLevel--;

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            script.VignetteCenter = EditorGUILayout.Vector2Field(vignetteCenterLabelContent, script.VignetteCenter);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40.0f);

            if (GUILayout.Button("Reset Center", new GUILayoutOption[] {
                GUILayout.Width(100.0f),
            }))
            {
                script.VignetteCenter.x = script.VignetteCenter.y = 0.5f;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            script.IsAnamorphicVignette = EditorGUILayout.Toggle(anamorphicLensLabel, script.IsAnamorphicVignette);
            EditorGUILayout.Space();

            script.VignetteDebugEnabled = EditorGUILayout.Toggle("Show Debug Output (Vignette): ", script.VignetteDebugEnabled);

        }

    #endregion

    }

#endif
}