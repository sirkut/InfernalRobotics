﻿using InfernalRobotics.Module;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using File = System.IO.File;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ControlsGUI : MonoBehaviour
    {
        protected static Rect ControlWindowPos;
        protected static Rect EditorWindowPos;
        protected static Rect PresetWindowPos;
        protected static int ControlWindowID;
        protected static int EditorWindowID;
        protected static int PresetWindowID;

        protected static bool ResetWindow = false;
        protected static Vector2 EditorScroll;
        protected static bool UseElectricCharge = true;
        protected static ControlsGUI GUIController;
        private IButton irMinimizeButton;

        internal List<ControlGroup> ServoGroups; //Changed Scope so draganddrop can use it
        private ApplicationLauncherButton button;
        private bool guiGroupEditorEnabled;
        private bool guiPresetsEnabled;
        private int partCounter;
        private MuMechToggle servoTweak;
        private string tmpMax = "";
        private string tmpMin = "";

        private Texture2D editorBGTex;
        private Texture2D stopButtonIcon;
        private Texture2D cogButtonIcon;

        private Texture2D expandIcon;
        private Texture2D collapseIcon;
        private Texture2D leftIcon;
        private Texture2D rightIcon;
        private Texture2D leftToggleIcon;
        private Texture2D rightToggleIcon;
        private Texture2D revertIcon;
        private Texture2D autoRevertIcon;
        private Texture2D downIcon;
        private Texture2D upIcon;
        private Texture2D trashIcon;
        private Texture2D presetsIcon;
        private Texture2D presetModeIcon;
        private Texture2D lockedIcon;
        private Texture2D unlockedIcon;
        private Texture2D invertedIcon;
        private Texture2D noninvertedIcon;
        private Texture2D nextIcon;
        private Texture2D prevIcon;

        public bool guiPresetMode = false;
        public bool guiHidden = false;

        private string tooltipText = "";
        private string lastTooltipText = "";
        private float tooltipTime;
        private const float TOOLTIP_MAX_TIME = 8f;
        private const float TOOLTIP_DELAY = 1.5f;

        private string lastFocusedControlName = "";

        //New sizes for a couple of things
        internal static Int32 EditorWindowWidth = 400;

        internal static Int32 ControlWindowWidth = 360;
        internal static Int32 EditorButtonHeights = 25;

        public bool GUIEnabled { get; set; }

        public static ControlsGUI IRGUI
        {
            get { return GUIController; }
        }

        static ControlsGUI()
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //basic constructor to initilize windowIDs
            ControlWindowID = UnityEngine.Random.Range(1000, 2000000) + assemblyName.GetHashCode();
            EditorWindowID = ControlWindowID + 1;
            PresetWindowID = ControlWindowID + 2;
        }

        private static void MoveServo(ControlGroup from, ControlGroup to, MuMechToggle servo)
        {
            to.AddControl(servo);
            from.RemoveControl(servo);
        }

        public static void AddServo(MuMechToggle servo)
        {
            if (!IRGUI)
                return;
            IRGUI.enabled = true;

            if (IRGUI.ServoGroups == null)
                IRGUI.ServoGroups = new List<ControlGroup>();

            ControlGroup controlGroup = null;

            if (!string.IsNullOrEmpty(servo.groupName))
            {
                foreach (ControlGroup cg in IRGUI.ServoGroups)
                {
                    if (servo.groupName == cg.Name)
                    {
                        controlGroup = cg;
                        break;
                    }
                }
                if (controlGroup == null)
                {
                    var newGroup = new ControlGroup(servo);
                    IRGUI.ServoGroups.Add(newGroup);
                    return;
                }
            }
            if (controlGroup == null)
            {
                if (IRGUI.ServoGroups.Count < 1)
                {
                    IRGUI.ServoGroups.Add(new ControlGroup());
                }
                controlGroup = IRGUI.ServoGroups[IRGUI.ServoGroups.Count - 1];
            }

            controlGroup.AddControl(servo);
        }

        public static void RemoveServo(MuMechToggle servo)
        {
            if (!IRGUI)
                return;

            if (IRGUI.ServoGroups == null)
                return;

            int num = 0;
            foreach (ControlGroup group in IRGUI.ServoGroups)
            {
                if (group.Name == servo.groupName)
                {
                    group.RemoveControl(servo);
                }
                num += group.Servos.Count;
            }
            IRGUI.enabled = num > 0;
        }

        private void OnVesselChange(Vessel v)
        {
            Logger.Log(string.Format("[GUI] vessel {0}", v.name));
            ServoGroups = null;
            ResetWindow = true;

            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.AddControl(servo);
                    }
                }
            }
            Logger.Log(string.Format("[GUI] {0} groups", groups.Count));

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                ServoGroups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = true;
                }
            }

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    servo.SetupJoints();
                }
            }
            Logger.Log("[GUI] OnVesselChange finished successfully", Logger.Level.Debug);
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.host;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    var temp = part.GetComponentInChildren<MuMechToggle>();
                    Vector3 tempAxis;

                    float orginalAngle;
                    temp.transform.rotation.ToAngleAxis(out orginalAngle, out tempAxis);
                    temp.OriginalAngle = orginalAngle;

                    if (temp.rotateJoint)
                    {
                        temp.OriginalAngle = temp.transform.eulerAngles.x;
                        temp.fixedMeshOriginalLocation = temp.transform.Find("model/" + temp.fixedMesh).eulerAngles;
                    }
                    else if (temp.translateJoint)
                    {
                        temp.OriginalTranslation = temp.transform.localPosition.y;
                    }
                }
            }
            catch (Exception ex) //Intentional Pokemon
            {
                Logger.Log("OnPartAttach: " + ex.Message, Logger.Level.Debug);
            }

            if ((EditorLogic.fetch.ship.parts.Count >= partCounter) &&
                (EditorLogic.fetch.ship.parts.Count != partCounter))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
            if ((EditorLogic.fetch.ship.parts.Count == 0) && (partCounter == 0))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
            Logger.Log("[GUI] OnPartAttach finished successfully", Logger.Level.Debug);
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.target;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    MuMechToggle temp = part.Modules.OfType<MuMechToggle>().First();

                    if (temp.rotateJoint)
                    {
                        if (!temp.part.name.Contains("IR.Rotatron.OffAxis"))
                        {
                            /*//silly check to prevent base creeping when reaching the limits
                            if (temp.rotation == temp.rotateMax && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else if (temp.rotation == temp.rotateMin && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.minTweak && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.maxTweak && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else*/
                            temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation);

                            temp.rotation = 0;
                        }
                    }
                    else if (temp.translateJoint)
                    {
                        //temp.part.transform.Find("model/" + temp.fixedMesh).position = temp.part.transform.position;
                        temp.FixedMeshTransform.position = temp.part.transform.position;
                        temp.translation = 0;
                    }
                }
            }
            catch
            {
            }

            foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
            {
                RemoveServo(p);
            }
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;

            if (part.Modules.OfType<MuMechToggle>().Any())
            {
                MuMechToggle temp1 = part.Modules.OfType<MuMechToggle>().First();
                if (temp1.part.name.Contains("IR.Rotatron.OffAxis"))
                {
                    temp1.rotation = 0;
                    //temp1.transform.Find("model/" + temp1.fixedMesh).eulerAngles = temp1.transform.eulerAngles;
                    temp1.FixedMeshTransform.eulerAngles = temp1.transform.eulerAngles;
                }
            }
            Logger.Log("[GUI] OnPartRemove finished successfully", Logger.Level.Debug);
        }

        private void OnEditorShipModified(ShipConstruct ship)
        {
            ServoGroups = null;

            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in ship.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.AddControl(servo);
                    }
                }
            }

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                ServoGroups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = true;
                }
            }

            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
            Logger.Log("[GUI] OnEditorShipModified finished successfully", Logger.Level.Debug);
        }

        /// <summary>
        ///     Load the textures from files to memory
        /// </summary>
        private void InitTextures()
        {
            stopButtonIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            GUIDragAndDrop.LoadImageFromFile(stopButtonIcon, "icon_stop.png");

            cogButtonIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(cogButtonIcon, "icon_cog.png");

            expandIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(expandIcon, "expand.png");

            collapseIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(collapseIcon, "collapse.png");

            leftIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(leftIcon, "left.png");

            rightIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(rightIcon, "right.png");

            leftToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(leftToggleIcon, "left_toggle.png");

            rightToggleIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(rightToggleIcon, "right_toggle.png");

            revertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(revertIcon, "revert.png");

            autoRevertIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(autoRevertIcon, "auto_revert.png");

            downIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(downIcon, "down.png");

            upIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(upIcon, "up.png");

            trashIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(trashIcon, "trash.png");

            presetsIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(presetsIcon, "presets.png");

            presetModeIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(presetModeIcon, "presetmode.png");

            lockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(lockedIcon, "locked.png");

            unlockedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(unlockedIcon, "unlocked.png");

            invertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(invertedIcon, "inverted.png");

            noninvertedIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(noninvertedIcon, "noninverted.png");

            nextIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(nextIcon, "next.png");

            prevIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile(prevIcon, "prev.png");
        }

        private void Awake()
        {
            LoadConfigXml();

            Logger.Log("[GUI] awake");

            GUIEnabled = false;

            guiGroupEditorEnabled = false;

            editorBGTex = CreateTextureFromColor(1, 1, new Color32(81, 86, 94, 255));

            InitTextures();

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT)
            {
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
                GUIController = this;
            }
            else if (scene == GameScenes.EDITOR)
            {
                //partCounter = EditorLogic.fetch.ship.parts.Count;
                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                GUIController = this;
            }
            else
            {
                GUIController = null;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
                irMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button_small";
                irMinimizeButton.ToolTip = "Infernal Robotics";
                irMinimizeButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT);
                irMinimizeButton.OnClick += e => GUIEnabled = !GUIEnabled;
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppReady);
            }

            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);

            Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
        }

        private void OnShowUI()
        {
            guiHidden = false;
        }

        private void OnHideUI()
        {
            guiHidden = true;
        }

        private void OnAppReady()
        {
            if (button == null)
            {
                try
                {
                    var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                    texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../Textures/icon_button.png")));

                    button = ApplicationLauncher.Instance.AddModApplication(delegate { GUIEnabled = true; },
                        delegate { GUIEnabled = false; }, null, null, null, null,
                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB |
                        ApplicationLauncher.AppScenes.SPH, texture);

                    ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("[GUI OnnAppReady Exception, {0}", ex.Message), Logger.Level.Fatal);
                }
            }
        }

        private void OnHideCallback()
        {
            GUIEnabled = false;
        }

        private void OnVesselWasModified(Vessel v)
        {
            if (v == FlightGlobals.ActiveVessel)
            {
                ServoGroups = null;

                OnVesselChange(v);
            }
        }

        private void OnDestroy()
        {
            Logger.Log("[GUI] destroy");
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton.Destroy();
            }
            else
            {
                try
                {
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnAppReady);

                    if (button != null && ApplicationLauncher.Instance != null)
                    {
                        ApplicationLauncher.Instance.RemoveModApplication(button);
                        button = null;
                    }

                    if (ApplicationLauncher.Instance != null)
                        ApplicationLauncher.Instance.RemoveOnHideCallback(OnHideCallback);
                }
                catch (Exception e)
                {
                    Logger.Log("[GUI] Failed unregistering AppLauncher handlers," + e.Message);
                }
            }

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            EditorLock(false);
            SaveConfigXml();
            Logger.Log("[GUI] OnDestroy finished sucessfully", Logger.Level.Debug);
        }

        //servo control window used in flight
        private void ControlWindow(int windowID)
        {
            GUILayoutOption width20 = GUILayout.Width(20);

            GUILayout.BeginVertical();

            const int BUTTON_HEIGHT = 22;

            var buttonStyle = new GUIStyle(GUI.skin.button);

            var padding2px = new RectOffset(2, 2, 2, 2);

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            //use of for instead of foreach in intentional
            for (int i = 0; i < ServoGroups.Count; i++)
            {
                ControlGroup g = ServoGroups[i];

                if (g.Servos.Any())
                {
                    GUILayout.BeginHorizontal();

                    if (g.Expanded)
                    {
                        g.Expanded = !GUILayout.Button(collapseIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }
                    else
                    {
                        g.Expanded = GUILayout.Button(expandIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }

                    //overload default GUIStyle with bold font
                    var t = new GUIStyle(GUI.skin.label.name)
                    {
                        fontStyle = FontStyle.Bold
                    };

                    GUILayout.Label(g.Name, t, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                    g.Speed = GUILayout.TextField(g.Speed, GUILayout.Width(30), GUILayout.Height(BUTTON_HEIGHT));

                    Rect last = GUILayoutUtility.GetLastRect();
                    Vector2 pos = Event.current.mousePosition;
                    if (last.Contains(pos) && Event.current.type == EventType.Repaint)
                        tooltipText = "Speed Multiplier";

                    bool toggleVal = GUILayout.Toggle(g.MovingNegative, new GUIContent(leftToggleIcon, "Toggle Move -"), buttonStyle,
                        GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));

                    SetTooltipText();

                    if (g.MovingNegative != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingNegative = toggleVal;
                    }

                    if (g.MovingNegative)
                    {
                        g.MovingPositive = false;
                        g.MoveNegative();
                    }

                    if (guiPresetMode)
                    {
                        if (GUILayout.Button(new GUIContent(prevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePrevPreset();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(autoRevertIcon, "Reset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(nextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNextPreset();
                        }
                        SetTooltipText();
                    }
                    else
                    {
                        if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNegative();

                            g.ButtonDown = true;
                        }

                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePositive();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();
                    }

                    toggleVal = GUILayout.Toggle(g.MovingPositive, new GUIContent(rightToggleIcon, "Toggle Move +"), buttonStyle,
                                                            GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));
                    SetTooltipText();

                    if (g.MovingPositive != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingPositive = toggleVal;
                    }

                    if (g.MovingPositive)
                    {
                        g.MovingNegative = false;
                        g.MovePositive();
                    }

                    GUILayout.EndHorizontal();

                    if (g.Expanded)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(5));
                        GUILayout.EndHorizontal();

                        foreach (MuMechToggle servo in g.Servos)
                        {
                            GUILayout.BeginHorizontal();

                            var dotStyle = new GUIStyle(GUI.skin.label)
                            {
                                richText = true,
                                alignment = TextAnchor.MiddleCenter
                            };

                            string servoStatus = servo.Translator.IsMoving() ? "<color=lime>■</color>" : "<color=yellow>■</color>";

                            if (servo.isMotionLock)
                                servoStatus = "<color=red>■</color>";

                            GUILayout.Label(servoStatus, dotStyle, GUILayout.Width(20), GUILayout.Height(BUTTON_HEIGHT));

                            var nameStyle = new GUIStyle(GUI.skin.label)
                            {
                                alignment = TextAnchor.MiddleLeft,
                                clipping = TextClipping.Clip
                            };

                            GUILayout.Label(servo.servoName, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                            nameStyle.fontStyle = FontStyle.Italic;
                            nameStyle.alignment = TextAnchor.MiddleCenter;

                            GUILayout.Label(string.Format("{0:#0.##}", servo.Position), nameStyle, GUILayout.Width(45), GUILayout.Height(BUTTON_HEIGHT));

                            bool servoLocked = servo.isMotionLock;
                            servoLocked = GUILayout.Toggle(servoLocked,
                                            servoLocked ? new GUIContent(lockedIcon, "Unlock Servo") : new GUIContent(unlockedIcon, "Lock Servo"),
                                            buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));
                            servo.SetLock(servoLocked);

                            SetTooltipText();

                            if (guiPresetMode)
                            {
                                if (GUILayout.Button(new GUIContent(prevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.MovePrevPreset();
                                }
                                SetTooltipText();

                                bool servoPresetsOpen = guiPresetsEnabled && (servo == servoTweak);
                                toggleVal = GUILayout.Toggle(servoPresetsOpen, new GUIContent(presetsIcon, "Edit Presets"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT));
                                if (servoPresetsOpen != toggleVal)
                                {
                                    if (guiPresetsEnabled && servoTweak == servo)
                                        guiPresetsEnabled = !guiPresetsEnabled;
                                    else
                                    {
                                        servoTweak = servo;
                                        if (!guiPresetsEnabled)
                                            guiPresetsEnabled = true;
                                    }
                                }
                                SetTooltipText();

                                if (GUILayout.Button(new GUIContent(nextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.MoveNextPreset();
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move(float.NegativeInfinity, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move(0f, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move(float.PositiveInfinity, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();
                            }
                            bool servoInverted = servo.invertAxis;

                            servoInverted = GUILayout.Toggle(servoInverted,
                                servoInverted ? new GUIContent(invertedIcon, "Un-invert Axis") : new GUIContent(noninvertedIcon, "Invert Axis"),
                                buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));

                            SetTooltipText();

                            if (servo.invertAxis != servoInverted)
                                servo.InvertAxisToggle();

                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal(GUILayout.Height(5));
                        GUILayout.EndHorizontal();
                    }

                    if (g.ButtonDown && Input.GetMouseButtonUp(0))
                    {
                        //one of the repeat buttons in the group was pressed, but now mouse button is up
                        g.ButtonDown = false;
                        g.Stop();
                    }
                }
            }

            GUILayout.BeginHorizontal(GUILayout.Height(32));

            if (GUILayout.Button(guiGroupEditorEnabled ? "Close Edit" : "Edit Groups", GUILayout.Height(32)))
            {
                guiGroupEditorEnabled = !guiGroupEditorEnabled;
            }

            guiPresetMode = GUILayout.Toggle(guiPresetMode, new GUIContent(presetModeIcon, "Preset Mode"), buttonStyle,
                GUILayout.Width(32), GUILayout.Height(32));
            SetTooltipText();

            buttonStyle.padding = new RectOffset(3, 3, 3, 3);

            if (GUILayout.Button(new GUIContent(stopButtonIcon, "Emergency Stop"), buttonStyle, GUILayout.Width(32), GUILayout.Height(32)))
            {
                foreach (ControlGroup g in ServoGroups)
                {
                    g.Stop();
                }
            }
            SetTooltipText();

            buttonStyle.padding = padding2px;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        /// <summary>
        /// Has to be called after any GUI element with tooltips.
        /// </summary>
        private void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
            {
                tooltipText = GUI.tooltip;
            }
        }

        /// <summary>
        /// Called in the end of OnGUI(), draws tooltip saved in tooltipText
        /// </summary>
        private void DrawTooltip()
        {
            Vector2 pos = Event.current.mousePosition;
            if (tooltipText != "" && tooltipTime < TOOLTIP_MAX_TIME)
            {
                var tooltipStyle = new GUIStyle
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = new Color32(207, 207, 207, 255),
                        background = GUIDragAndDrop.ImgBackground
                    },
                    stretchHeight = true,
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(4, 4, 6, 4),
                    alignment = TextAnchor.MiddleLeft
                };

                var tooltip = new GUIContent(tooltipText);
                Vector2 size = tooltipStyle.CalcSize(tooltip);

                var tooltipPos = new Rect(pos.x - (size.x / 4), pos.y + 17, size.x, size.y);

                if (tooltipText != lastTooltipText)
                {
                    //reset timer
                    tooltipTime = 0f;
                }

                if (tooltipTime > TOOLTIP_DELAY)
                {
                    GUI.Label(tooltipPos, tooltip, tooltipStyle);
                    GUI.depth = 0;
                }

                tooltipTime += Time.deltaTime;
            }

            if (tooltipText != lastTooltipText) tooltipTime = 0f;
            lastTooltipText = tooltipText;
        }

        /// <summary>
        /// Creates the solid texture of given size and Color.
        /// </summary>
        /// <returns>The texture from color.</returns>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="col">Color</param>
        private static Texture2D CreateTextureFromColor(int width, int height, Color col)
        {
            var pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        /// <summary>
        /// Implements Group Editor window. Used both in VAB/SPH and in Flight,
        /// uses HighLogic.LoadedScene to check whether to display certain fields.
        /// </summary>
        /// <param name="windowID">Window ID</param>
        private void EditorWindow(int windowID)
        {
            GUILayoutOption expand = GUILayout.ExpandWidth(true);
            GUILayoutOption rowHeight = GUILayout.Height(22);

            GUILayoutOption maxHeight = GUILayout.MaxHeight(Screen.height * 0.67f);

            var buttonStyle = new GUIStyle(GUI.skin.button);
            var padding1px = new RectOffset(1, 1, 1, 1);
            var padding2px = new RectOffset(2, 2, 2, 2);

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            var cogButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(3, 3, 3, 3)
            };

            bool isEditor = (HighLogic.LoadedScene == GameScenes.EDITOR);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            EditorScroll = GUILayout.BeginScrollView(EditorScroll, false, false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(EditorWindowPos, EditorScroll);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            // pad   name     cog  keys   move  ec/s  remove
            // <-20-><-flex-><-20-><-45-><-45-><-33-><-45->

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();

            GUILayout.Label("Group Name", expand, rowHeight);
            GUILayout.Label("Keys", GUILayout.Width(45), rowHeight);

            if (isEditor)
                GUILayout.Label("Move", GUILayout.Width(45), rowHeight);

            if (UseElectricCharge)
            {
                GUILayout.Label("EC/s", GUILayout.Width(33), rowHeight);
            }

            //make room for remove button
            if (ServoGroups.Count > 1)
            {
                GUILayout.Space(45);
            }

            var alternateBG = new GUIStyle("label")
            {
                normal =
                {
                    background = editorBGTex
                }
            };

            GUILayout.EndHorizontal();

            for (int i = 0; i < ServoGroups.Count; i++)
            {
                ControlGroup grp = ServoGroups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.Name, i);

                string tmp = GUILayout.TextField(grp.Name, expand, rowHeight);

                grp.Name = tmp;

                if (isEditor)
                {
                    grp.Expanded = GUILayout.Toggle(grp.Expanded, new GUIContent(cogButtonIcon, "Adv. settings"), cogButtonStyle, GUILayout.Width(22), rowHeight);
                    SetTooltipText();
                }
                //<-keys->
                tmp = GUILayout.TextField(grp.ReverseKey, GUILayout.Width(20), rowHeight);
                grp.ReverseKey = tmp;

                tmp = GUILayout.TextField(grp.ForwardKey, GUILayout.Width(20), rowHeight);
                grp.ForwardKey = tmp;

                if (isEditor)
                {
                    if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            servo.MoveLeft();
                        }
                    }
                    SetTooltipText();

                    if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            servo.MoveRight();
                        }
                    }
                    SetTooltipText();
                }

                if (UseElectricCharge)
                {
                    var t = new GUIStyle(GUI.skin.label.name) { alignment = TextAnchor.MiddleCenter };
                    GUILayout.Label(grp.TotalElectricChargeRequirement.ToString(), t, GUILayout.Width(33), rowHeight);
                }

                if (i > 0)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent(trashIcon, "Delete Group"), buttonStyle, GUILayout.Width(30), rowHeight))
                    {
                        while (grp.Servos.Any())
                        {
                            var s = grp.Servos.First();
                            MoveServo(grp, ServoGroups[i - 1], s);
                        }

                        ServoGroups.RemoveAt(i);
                        ResetWindow = true;
                        return;
                    }
                    SetTooltipText();
                    GUILayout.Space(5);
                }
                else
                {
                    if (ServoGroups.Count > 1)
                    {
                        GUILayout.Space(45);
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(alternateBG);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                // handle name   pos   range  move speed  group
                // <-20-><-flex-><-40-><-40-><-30-><-40-><-30->

                //Pad the text so it still aligns with the text box
                GUIDragAndDrop.PadText();

                GUILayout.Label("Servo Name", expand, rowHeight);

                GUILayout.Space(25);

                GUILayout.Label("Pos.", GUILayout.Width(30), rowHeight);

                if (isEditor)
                    GUILayout.Label("Move", GUILayout.Width(45), rowHeight);

                GUILayout.Label("Group", GUILayout.Width(45), rowHeight);

                GUILayout.EndHorizontal();

                for (int iS = 0; iS < grp.Servos.Count; iS++)
                {
                    MuMechToggle servo = grp.Servos[iS];
                    if (!servo.freeMoving)
                    {
                        GUILayout.BeginHorizontal();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.servoName, i, iS);

                        if (isEditor)
                        {
                            GUILayout.BeginVertical();
                            GUILayout.BeginHorizontal();
                        }

                        servo.servoName = GUILayout.TextField(servo.servoName, expand, rowHeight);

                        servo.groupName = grp.Name;
                        servo.reverseKey = grp.ReverseKey;
                        servo.forwardKey = grp.ForwardKey;

                        if (EditorWindowPos.Contains(mousePos))
                        {
                            Rect last = GUILayoutUtility.GetLastRect();
                            Vector2 pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.part.SetHighlight(highlight, false);
                        }

                        bool servoPresetsOpen = guiPresetsEnabled && (servo == servoTweak);
                        bool toggleVal = GUILayout.Toggle(servoPresetsOpen, new GUIContent(presetsIcon, "Edit Presets"), buttonStyle, GUILayout.Width(22), rowHeight);
                        if (servoPresetsOpen != toggleVal)
                        {
                            if (guiPresetsEnabled && servoTweak == servo)
                                guiPresetsEnabled = !guiPresetsEnabled;
                            else
                            {
                                servoTweak = servo;
                                if (!guiPresetsEnabled)
                                    guiPresetsEnabled = true;
                            }
                        }
                        SetTooltipText();

                        GUILayout.Label(string.Format("{0:#0.##}", servo.Position), GUILayout.Width(30), rowHeight);

                        //individual servo movement when in editor
                        if (isEditor)
                        {
                            if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.MoveLeft();
                            }
                            SetTooltipText();

                            if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.MoveRight();
                            }
                            SetTooltipText();
                        }

                        if (grp.Expanded && isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();

                            GUILayout.Label("Range: ", GUILayout.Width(40), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0#}", servo.minTweak), GUILayout.Width(40), rowHeight);
                            float tmpValue;

                            float minPossibleRange = servo.rotateJoint ? servo.rotateMin : servo.translateMin;
                            float maxPossibleRange = servo.rotateJoint ? servo.rotateMax : servo.translateMax;

                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.minTweak = Mathf.Clamp(tmpValue, minPossibleRange, maxPossibleRange);
                            }

                            tmpMax = GUILayout.TextField(string.Format("{0:#0.0#}", servo.maxTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMax, out tmpValue))
                            {
                                servo.maxTweak = Mathf.Clamp(tmpValue, minPossibleRange, maxPossibleRange);
                            }

                            GUILayout.Label("Spd: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}", servo.speedTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.speedTweak = Math.Max(tmpValue, 0.01f);
                            }

                            GUILayout.Label("Acc: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}", servo.accelTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.accelTweak = Math.Max(tmpValue, 0.05f);
                            }
                        }

                        if (isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                        }

                        if (ServoGroups.Count > 1)
                        {
                            buttonStyle.padding = padding1px;

                            if (i > 0)
                            {
                                if (GUILayout.Button(new GUIContent(upIcon, "To previous Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    MoveServo(grp, ServoGroups[i - 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }
                            if (i < (ServoGroups.Count - 1))
                            {
                                if (GUILayout.Button(new GUIContent(downIcon, "To next Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    MoveServo(grp, ServoGroups[i + 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }

                            buttonStyle.padding = padding2px;
                        }
                        else
                        {
                            GUILayout.Space(45);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                //Updates the Groups Details with a height for all servos
                GUIDragAndDrop.EndDrawGroup(i);

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                //empty line
                GUILayout.BeginHorizontal();
                GUILayout.Label(" ", expand, GUILayout.Height(7));
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add new Group"))
            {
                var temp = new ControlGroup { Name = string.Format("New Group {0}", (ServoGroups.Count + 1)) };
                ServoGroups.Add(temp);
            }

            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            //Do the End of window Code for DragAnd Drop
            GUIDragAndDrop.WindowEnd();

            //If we are dragging an item disable the windowdrag
            if (!GUIDragAndDrop.DraggingItem)
                GUI.DragWindow();
        }

        //Used by DragAndDrop to scroll the scrollview when dragging at top or bottom of window
        internal static void SetEditorScrollYPosition(Single newY)
        {
            EditorScroll.y = newY;
        }

        private void PresetsEditWindow(int windowID)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button);
            //var padding1px = new RectOffset(1, 1, 1, 1);
            var padding2px = new RectOffset(2, 2, 2, 2);

            GUILayoutOption rowHeight = GUILayout.Height(22);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preset position", GUILayout.ExpandWidth(true), rowHeight);
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(30), rowHeight))
            {
                servoTweak.PresetPositions.Add(servoTweak.Position);
            }
            GUILayout.EndHorizontal();

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < servoTweak.PresetPositions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("Preset " + i);
                string tmp = GUILayout.TextField(string.Format("{0:#0.0#}", servoTweak.PresetPositions[i]), GUILayout.ExpandWidth(true), rowHeight);

                float tmpValue;
                if (float.TryParse(tmp, out tmpValue))
                {
                    tmpValue = Mathf.Clamp(tmpValue, servoTweak.minTweak, servoTweak.maxTweak);
                    servoTweak.PresetPositions[i] = tmpValue;
                }

                if (GUILayout.Button(new GUIContent(trashIcon, "Delete preset"), buttonStyle, GUILayout.Width(30), rowHeight))
                {
                    servoTweak.PresetPositions.RemoveAt(i);
                }
                SetTooltipText();
                GUILayout.EndHorizontal();
            }

            if (lastFocusedControlName != GUI.GetNameOfFocusedControl())
            {
                servoTweak.PresetPositions.Sort();
                lastFocusedControlName = GUI.GetNameOfFocusedControl();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Symmetry", buttonStyle))
            {
                servoTweak.PresetPositions.Sort();
                servoTweak.presetPositionsSerialized = servoTweak.SerializePresets();

                if (servoTweak.part.symmetryCounterparts.Count > 1)
                {
                    foreach (Part part in servoTweak.part.symmetryCounterparts)
                    {
                        ((MuMechToggle)part.Modules["MuMechToggle"]).presetPositionsSerialized = servoTweak.presetPositionsSerialized;
                        ((MuMechToggle)part.Modules["MuMechToggle"]).ParsePresetPositions();
                    }
                }
            }

            if (GUILayout.Button("Save&Exit", buttonStyle, GUILayout.Width(70)))
            {
                servoTweak.PresetPositions.Sort();
                servoTweak.presetPositionsSerialized = servoTweak.SerializePresets();
                guiPresetsEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void RefreshKeysFromGUI()
        {
            foreach (ControlGroup g in ServoGroups)
            {
                if (g.Servos.Any())
                {
                    foreach (MuMechToggle servo in g.Servos)
                    {
                        servo.reverseKey = g.ReverseKey;
                        servo.forwardKey = g.ForwardKey;
                    }
                }
            }
        }

        private void OnGUI()
        {
            // This particular test isn't needed due to the GUI being enabled
            // and disabled as appropriate, but it saves potential NREs.

            if (ServoGroups == null)
                return;

            //what is that for?
            //if (InputLockManager.IsLocked(ControlTypes.LINEAR)) return;

            if (ControlWindowPos.x == 0 && ControlWindowPos.y == 0)
            {
                ControlWindowPos = new Rect(Screen.width - 510, 70, 10, 10);
            }
            if (EditorWindowPos.x == 0 && EditorWindowPos.y == 0)
            {
                EditorWindowPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (PresetWindowPos.x == 0 && PresetWindowPos.y == 0)
            {
                PresetWindowPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (ResetWindow)
            {
                ControlWindowPos = new Rect(ControlWindowPos.x, ControlWindowPos.y, 10, 10);
                EditorWindowPos = new Rect(EditorWindowPos.x, EditorWindowPos.y, 10, 10);
                PresetWindowPos = new Rect(PresetWindowPos.x, PresetWindowPos.y, 10, 10);
                ResetWindow = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            GUIDragAndDrop.OnGUIOnceOnly();

            if (scene == GameScenes.FLIGHT)
            {
                float maxServoNameLabelSize = 0f;
                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = false
                };

                var boldStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };

                foreach (ControlGroup grp in ServoGroups)
                {
                    Vector2 size = boldStyle.CalcSize(new GUIContent(grp.Name));
                    if (size.x > maxServoNameLabelSize) maxServoNameLabelSize = size.x;

                    foreach (MuMechToggle s in grp.Servos)
                    {
                        size = nameStyle.CalcSize(new GUIContent(s.servoName));
                        if (size.x > maxServoNameLabelSize) maxServoNameLabelSize = size.x;
                    }
                }

                ControlWindowWidth = (int)Math.Round(maxServoNameLabelSize + 240);

                if (ControlWindowWidth > Screen.width * 0.7) ControlWindowWidth = (int)Math.Round(Screen.width * 0.7f);

                GUILayoutOption height = GUILayout.Height(Screen.height / 2f);
                if (GUIEnabled && !guiHidden)
                {
                    ControlWindowPos = GUILayout.Window(ControlWindowID, ControlWindowPos,
                        ControlWindow,
                        "Servo Control",
                        GUILayout.Width(ControlWindowWidth),
                        GUILayout.Height(80));
                    if (guiGroupEditorEnabled)
                        EditorWindowPos = GUILayout.Window(EditorWindowID, EditorWindowPos,
                            EditorWindow,
                            "Servo Group Editor",
                            GUILayout.Width(EditorWindowWidth), //Using a variable here
                            height);
                    if (guiPresetsEnabled)
                        PresetWindowPos = GUILayout.Window(PresetWindowID, PresetWindowPos,
                            PresetsEditWindow,
                            servoTweak.servoName,
                            GUILayout.Width(200),
                            GUILayout.Height(80));
                }
                RefreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height / 2f);

                if (GUIEnabled && !guiHidden)
                {
                    EditorWindowPos = GUILayout.Window(958, EditorWindowPos,
                        EditorWindow,
                        "Servo Configuration",
                        GUILayout.Width(EditorWindowWidth), //Using a variable here
                        height);
                    if (guiPresetsEnabled)
                        PresetWindowPos = GUILayout.Window(960, PresetWindowPos,
                            PresetsEditWindow,
                            servoTweak.servoName,
                            GUILayout.Width(200),
                            GUILayout.Height(80));
                }

                var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                bool lockEditor = GUIEnabled && (EditorWindowPos.Contains(mousePos) || PresetWindowPos.Contains(mousePos));

                EditorLock(lockEditor);
            }

            GUIDragAndDrop.OnGUIEvery();
            DrawTooltip();
        }

        /// <summary>
        ///     Applies or removes the lock
        /// </summary>
        /// <param name="apply">Which way are we going</param>
        internal void EditorLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") != ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] AddingLock-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);

                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "IRGUILockOfEditor");
                }
            }
            //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") == ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] Removing-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);
                    InputLockManager.RemoveControlLock("IRGUILockOfEditor");
                }
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.load();
            EditorWindowPos = config.GetValue<Rect>("editorWinPos");
            PresetWindowPos = config.GetValue<Rect>("presetWinPos");
            ControlWindowPos = config.GetValue<Rect>("controlWinPos");
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("editorWinPos", EditorWindowPos);
            config.SetValue("presetWinPos", PresetWindowPos);
            config.SetValue("controlWinPos", ControlWindowPos);
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        public class ControlGroup
        {
            private bool stale;
            private float totalElectricChargeRequirement;
            private string speed;
            private string forwardKey;
            private string reverseKey;
            private readonly List<MuMechToggle> servos;

            public ControlGroup(MuMechToggle servo)
                : this()
            {
                Name = servo.groupName;
                ForwardKey = servo.forwardKey;
                ReverseKey = servo.reverseKey;
                Speed = servo.customSpeed.ToString("g");
                ShowGUI = servo.showGUI;
                servos.Add(servo);
            }

            public ControlGroup()
            {
                servos = new List<MuMechToggle>();
                Expanded = false;
                Name = "New Group";
                ForwardKey = string.Empty;
                ReverseKey = string.Empty;
                Speed = "1";
                ShowGUI = true;
                MovingNegative = false;
                MovingPositive = false;
                ButtonDown = false;
                stale = true;
            }

            public bool ButtonDown { get; set; }

            public bool Expanded { get; set; }

            public string Name { get; set; }

            public bool ShowGUI { get; private set; }

            public bool MovingNegative { get; set; }

            public bool MovingPositive { get; set; }

            public IList<MuMechToggle> Servos
            {
                get { return servos; }
            }

            public string ForwardKey
            {
                get { return forwardKey; }
                set
                {
                    forwardKey = value;
                    PropogateForward();
                }
            }

            public string ReverseKey
            {
                get { return reverseKey; }
                set
                {
                    reverseKey = value;
                    PropogateReverse();
                }
            }

            public string Speed
            {
                get { return speed; }
                set
                {
                    speed = value;
                    PropogateSpeed();
                }
            }

            public float TotalElectricChargeRequirement
            {
                get
                {
                    if (stale) Freshen();
                    return totalElectricChargeRequirement;
                }
            }

            public void AddControl(MuMechToggle control)
            {
                servos.Add(control);
                control.groupName = Name;
                control.forwardKey = ForwardKey;
                control.reverseKey = ReverseKey;
                stale = true;
            }

            public void RemoveControl(MuMechToggle control)
            {
                servos.Remove(control);
                stale = true;
            }

            public void MovePositive()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(float.PositiveInfinity, servo.customSpeed * servo.speedTweak);
                    }
                }
            }

            public void MoveNegative()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(float.NegativeInfinity, servo.customSpeed * servo.speedTweak);
                    }
                }
            }

            public void MoveCenter()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move(0f, servo.customSpeed * servo.speedTweak); //TODO: to be precise this should be not Zero but a default rotation/translation as set in VAB/SPH
                    }
                }
            }

            public void MoveNextPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MoveNextPreset();
                    }
                }
            }

            public void MovePrevPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MovePrevPreset();
                    }
                }
            }

            public void Stop()
            {
                MovingNegative = false;
                MovingPositive = false;

                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Stop();
                    }
                }
            }

            private void Freshen()
            {
                if (Servos == null) return;

                if (UseElectricCharge)
                {
                    float chargeRequired = Servos.Where(s => s.freeMoving == false).Select(s => s.electricChargeRequired).Sum();
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.GroupElectricChargeRequired = chargeRequired;
                    }
                    totalElectricChargeRequirement = chargeRequired;
                }

                stale = false;
            }

            private void PropogateForward()
            {
                if (Servos == null) return;

                foreach (var servo in Servos)
                {
                    servo.forwardKey = ForwardKey;
                }
            }

            private void PropogateReverse()
            {
                if (Servos == null) return;

                foreach (var servo in Servos)
                {
                    servo.reverseKey = ReverseKey;
                }
            }

            private void PropogateSpeed()
            {
                if (Servos == null) return;

                float parsedSpeed;
                var isFloat = float.TryParse(speed, out parsedSpeed);
                if (!isFloat) return;

                foreach (var servo in Servos)
                {
                    servo.customSpeed = parsedSpeed;
                }
            }
        }
    }
}