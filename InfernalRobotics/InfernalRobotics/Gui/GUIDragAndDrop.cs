﻿using System;
using System.Collections.Generic;
using System.Linq;
using InfernalRobotics.Control;
using UnityEngine;
using InfernalRobotics.Command;
using InfernalRobotics.Utility;

namespace InfernalRobotics.Gui
{
    internal static class GUIDragAndDrop
    {
        /// <summary>
        ///     Use this to enable/disable functionality
        /// </summary>
        internal static bool Enabled = true;

        internal static bool ShowGroupHandles = false;

        //Internal flag so we only set up textures, etc once
        internal static bool GUISetupDone = false;

        internal static bool DraggingItem = false;

        #region Group Objects

        internal static GroupDetails GroupOver { get; set; }

        internal static bool GroupOverUpper { get; set; }

        internal static GroupDetails GroupIconOver { get; set; }

        internal static GroupDetails GroupDragging { get; set; }

        internal static GroupDetailsList Groups { get; set; }

        internal class GroupDetails
        {
            public string Name { get; set; }

            public int ID { get; set; }

            public Rect IconRect { get; set; }

            public Rect GroupRect { get; set; }
        }

        internal class GroupDetailsList : List<GroupDetails>
        {
            internal void Add(string name, int groupID, Rect iconRect, float windowWidth)
            {
                var newG = new GroupDetails
                {
                    Name = name,
                    ID = groupID,
                    IconRect = iconRect,
                    GroupRect = new Rect(iconRect) { y = iconRect.y - 5, width = windowWidth - 50, height = 52 }
                };
                Add(newG);
            }
        }

        #endregion Group Objects

        #region Servo Objects

        internal static ServoDetails ServoOver;
        internal static bool ServoOverUpper;
        internal static ServoDetails ServoIconOver;
        internal static ServoDetails ServoDragging;

        internal static ServoDetailsList Servos { get; set; }

        internal class ServoDetails
        {
            public string Name { get; set; }

            public int ID { get; set; }

            public int GroupID { get; set; }

            public Rect IconRect { get; set; }

            public Rect ServoRect { get; set; }
        }

        internal class ServoDetailsList : List<ServoDetails>
        {
            internal void Add(string name, int groupID, int servoID, Rect iconRect, float windowWidth)
            {
                var servo = new ServoDetails
                {
                    Name = name,
                    ID = servoID,
                    GroupID = groupID,
                    IconRect = iconRect,
                    ServoRect =
                        new Rect(iconRect) { y = iconRect.y - 5, width = windowWidth - 80, height = iconRect.height + 7 }
                };
                Add(servo);
            }
        }

        #endregion Servo Objects

        #region Texture Stuff

        internal static Texture2D ImgDrag { get; set; }

        internal static Texture2D ImgDragHandle { get; set; }

        internal static Texture2D ImgDragInsert { get; set; }

        internal static Texture2D ImgBackground { get; set; }

        internal static GUIStyle StyleDragInsert { get; set; }

        internal static GUIStyle StyleDragTooltip { get; set; }

        /// <summary>
        ///     Load the textures from file to memory
        /// </summary>
        private static void InitTextures()
        {
            TextureLoader.LoadImageFromFile(ImgDrag, "icon_drag.png");
            TextureLoader.LoadImageFromFile(ImgDragHandle, "icon_dragHandle.png");
            TextureLoader.LoadImageFromFile(ImgDragInsert, "icon_dragInsert.png");
            TextureLoader.LoadImageFromFile(ImgBackground, "icon_background.png");
        }

        /// <summary>
        ///     setup the styles that use textures
        /// </summary>
        private static void InitStyles()
        {
            //the border determines which bit doesn't repeat
            StyleDragInsert = new GUIStyle
            {
                normal =
                {
                    background = ImgDragInsert
                },
                border = new RectOffset(8, 8, 3, 3)
            };

            StyleDragTooltip = new GUIStyle
            {
                fontSize = 12,
                normal =
                {
                    textColor = new Color32(207, 207, 207, 255),
                    background = ImgBackground
                },
                stretchHeight = true,
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(4, 4, 6, 4),
                alignment = TextAnchor.MiddleLeft
            };
        }

        

        #endregion Texture Stuff

        static GUIDragAndDrop()
        {
            Groups = new GroupDetailsList();
            Servos = new ServoDetailsList();
            ImgDrag = new Texture2D(16, 16, TextureFormat.ARGB32, false);
            ImgDragHandle = new Texture2D(16, 16, TextureFormat.ARGB32, false);
            ImgDragInsert = new Texture2D(18, 9, TextureFormat.ARGB32, false);

            ImgBackground = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        }

        internal static bool Disabled
        {
            get { return !Enabled; }
        }

        internal static Vector2 MousePosition { get; set; }

        internal static Vector2 ScrollPosition { get; set; }

        internal static Rect WindowRect { get; set; }

        internal static Rect RectScrollTop { get; set; }

        internal static Rect RectScrollBottom { get; set; }

        //This is called all the time, but only run once per scene
        internal static void OnGUIOnceOnly()
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            if (!GUISetupDone)
            {
                InitTextures();
                InitStyles();
                GUISetupDone = true;
            }
        }

        /// <summary>
        ///     Called at Start of DrawWindow routine
        /// </summary>
        /// <param name="windowRect">Rect of the window</param>
        /// <param name="editorScroll">Scroll Position of the scroll window</param>
        internal static void WindowBegin(Rect windowRect, Vector2 editorScroll)
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            //store the Position of the mouse and window elements for use later
            MousePosition = new Vector2(Input.mousePosition.x - windowRect.x,
                Screen.height - Input.mousePosition.y - windowRect.y);
            ScrollPosition = editorScroll;
            WindowRect = windowRect;
            if (Event.current.type == EventType.Repaint)
            {
                Groups = new GroupDetailsList();
                Servos = new ServoDetailsList();
            }

            RectScrollTop = new Rect(8, 30, windowRect.width - 25, 15);
            RectScrollBottom = new Rect(8, windowRect.height - 22, windowRect.width - 25, 15);
        }

        /// <summary>
        ///     Add a 20pox pad for when a handle is not visible and Drag and Drop is enabled
        /// </summary>
        internal static void PadText()
        {
            if (Enabled)
                GUILayout.Space(20);
        }

        /// <summary>
        ///     Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="name">String name of the Group</param>
        /// <param name="groupID">Index of the Group</param>
        internal static void DrawGroupHandle(string name, int groupID)
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            //Draw the drag handle
            GUILayout.Label(ShowGroupHandles ? ImgDragHandle : new Texture2D(0, 0));

            if (Event.current.type == EventType.Repaint)
            {
                //If its the repaint event then use GUILayoutUtility to get the location of the handle
                //And build the structure so we know where on the screen it is
                Groups.Add(name, groupID, GUILayoutUtility.GetLastRect(), WindowRect.width);
            }
        }

        /// <summary>
        ///     Called after we draw the last servo in a group
        /// </summary>
        /// <param name="groupID">What Group was it</param>
        internal static void EndDrawGroup(int groupID)
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            //Only do this if there is a group that contains Servos
            if (Groups.Count < 1 || Servos.Count < 1 || Servos.All(x => x.GroupID != groupID)) return;

            try
            {
                //Set the height of the group to contain all the servos
                var newRect = new Rect(Groups[groupID].GroupRect)
                {
                    height = Servos.Last(x => x.GroupID == groupID).ServoRect.y +
                             Servos.Last(x => x.GroupID == groupID).ServoRect.height -
                             Groups[groupID].GroupRect.y
                };
                Groups[groupID].GroupRect = newRect;
            }
            catch (Exception ex) // Intentional Pokemon
            {
                Logger.Log(ex.Message, Logger.Level.Fatal);
            }
        }

        /// <summary>
        ///     Draw the Group Handle and do any maths
        /// </summary>
        /// <param name="name">Text name of the Servo</param>
        /// <param name="groupID">Index of the Group</param>
        /// <param name="servoID">Index of the Servo</param>
        internal static void DrawServoHandle(string name, int groupID, int servoID)
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            //Draw the drag handle
            GUILayout.Label(ImgDragHandle);

            if (Event.current.type == EventType.Repaint)
            {
                //If its the repaint event then use GUILayoutUtility to get the location of the handle
                //And build the structure so we know where on the screen it is
                Servos.Add(name, groupID, servoID, GUILayoutUtility.GetLastRect(), WindowRect.width);
            }
        }

        internal static void WindowEnd()
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            // Draw the Yellow insertion strip
            Rect insertRect;
            if (DraggingItem && ServoDragging != null && ServoOver != null)
            {
                //What is the insert position of the dragged servo
                int insertIndex = ServoOver.ID + (ServoOverUpper ? 0 : 1);
                if ((ServoDragging.GroupID != ServoOver.GroupID) ||
                    (ServoDragging.ID != insertIndex && (ServoDragging.ID + 1) != insertIndex))
                {
                    //Only in here if the drop will cause the list to change
                    float rectResMoveY;
                    //is it dropping in the list or at the end
                    if (insertIndex < Servos.Where(x => x.GroupID == ServoOver.GroupID).ToList().Count)
                        rectResMoveY =
                            Servos.Where(x => x.GroupID == ServoOver.GroupID).ToList()[insertIndex].ServoRect.y;
                    else
                        rectResMoveY = Servos.Where(x => x.GroupID == ServoOver.GroupID).ToList().Last().ServoRect.y +
                                       Servos.Where(x => x.GroupID == ServoOver.GroupID)
                                           .ToList()
                                           .Last()
                                           .ServoRect.height;

                    //calculate and draw the graphic
                    insertRect = new Rect(12,
                        rectResMoveY + 26 - ScrollPosition.y,
                        WindowRect.width - 34, 9);
                    GUI.Box(insertRect, "", StyleDragInsert);
                }
            }
            else if (DraggingItem && GroupDragging != null && GroupOver != null)
            {
                //What is the insert position of the dragged group
                int insertIndex = GroupOver.ID + (GroupOverUpper ? 0 : 1);
                if (GroupDragging.ID != insertIndex && (GroupDragging.ID + 1) != insertIndex)
                {
                    //Only in here if the drop will cause the list to change
                    float rectResMoveY;
                    //is it dropping in the list or at the end
                    if (insertIndex < Groups.Count)
                        rectResMoveY = Groups[insertIndex].GroupRect.y;
                    else
                        rectResMoveY = Groups.Last().GroupRect.y + Groups.Last().GroupRect.height;

                    //calculate and draw the graphic
                    insertRect = new Rect(12,
                        rectResMoveY + 26 - ScrollPosition.y,
                        WindowRect.width - 34, 9);
                    GUI.Box(insertRect, "", StyleDragInsert);
                }
            }
            else if (DraggingItem && ServoDragging != null && GroupOver != null &&
                     Servos.All(x => x.GroupID != GroupOver.ID))
            {
                //This is the case for an empty Group
                //is it dropping in the list or at the end
                float rectResMoveY = GroupOver.GroupRect.y + GroupOver.GroupRect.height;

                //calculate and draw the graphic
                insertRect = new Rect(12,
                    rectResMoveY + 26 - ScrollPosition.y,
                    WindowRect.width - 34, 9);
                GUI.Box(insertRect, "", StyleDragInsert);
            }

            //What is the mouse over
            if (MousePosition.y > 32 && MousePosition.y < WindowRect.height - 8)
            {
                //inside the scrollview
                //check what group
                GroupOver =
                    Groups.FirstOrDefault(x => x.GroupRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                GroupIconOver =
                    Groups.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                if (GroupOver != null)
                    GroupOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - GroupOver.GroupRect.y) <
                                     GroupOver.GroupRect.height / 2;

                //or servo
                ServoOver =
                    Servos.FirstOrDefault(x => x.ServoRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                ServoIconOver =
                    Servos.FirstOrDefault(x => x.IconRect.Contains(MousePosition + ScrollPosition - new Vector2(8, 29)));
                if (ServoOver != null)
                    ServoOverUpper = ((MousePosition + ScrollPosition - new Vector2(8, 29)).y - ServoOver.ServoRect.y) <
                                     ServoOver.ServoRect.height / 2;
            }
            else
            {
                //Otherwise empty the variables
                GroupOver = null;
                ServoOver = null;
                GroupIconOver = null;
                ServoIconOver = null;
            }

            //MouseDown - left mouse button
            if (Event.current.type == EventType.mouseDown &&
                Event.current.button == 0)
            {
                if (GroupIconOver != null)
                {
                    //If we click on the drag icon then start the drag
                    GroupDragging = GroupOver;
                    DraggingItem = true;
                }
                else if (ServoIconOver != null)
                {
                    //If we click on the drag icon then start the drag
                    ServoDragging = ServoOver;
                    DraggingItem = true;
                }
            }

            //did we release the mouse
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0)
            {
                if (GroupDragging != null && GroupOver != null)
                {
                    //were we dragging a group
                    if (GroupDragging.ID != (GroupOver.ID + (GroupOverUpper ? 0 : 1)))
                    {
                        //And it will cause a reorder
                        Logger.Log(string.Format("Reordering:{0}-{1}", GroupDragging.ID,
                            (GroupOver.ID - (GroupOverUpper ? 1 : 0))));

                        //where are we inserting the dragged item
                        int insertAt = (GroupOver.ID - (GroupOverUpper ? 1 : 0));
                        if (GroupOver.ID < GroupDragging.ID) insertAt += 1;

                        //move em around
                        ServoController.ControlGroup g = ServoController.Instance.ServoGroups[GroupDragging.ID];
                        ServoController.Instance.ServoGroups.RemoveAt(GroupDragging.ID);
                        ServoController.Instance.ServoGroups.Insert(insertAt, g);
                    }
                }
                else if (ServoDragging != null && ServoOver != null)
                {
                    //were we dragging a servo
                    //where are we inserting the dragged item
                    int insertAt = (ServoOver.ID + (ServoOverUpper ? 0 : 1));
                    if (ServoOver.GroupID == ServoDragging.GroupID && ServoDragging.ID < ServoOver.ID)
                        insertAt -= 1;

                    Logger.Log(string.Format("Reordering:({0}-{1})->({2}-{3})", ServoDragging.GroupID, ServoDragging.ID,
                        ServoOver.GroupID, insertAt));

                    //move em around
                    IServo s = ServoController.Instance.ServoGroups[ServoDragging.GroupID].Servos[ServoDragging.ID];
                    ServoController.Instance.ServoGroups[ServoDragging.GroupID].Servos.RemoveAt(ServoDragging.ID);
                    ServoController.Instance.ServoGroups[ServoOver.GroupID].Servos.Insert(insertAt, s);
                }
                else if (ServoDragging != null && GroupOver != null && Servos.All(x => x.GroupID != GroupOver.ID))
                {
                    //dragging a servo to an empty group
                    const int INSERT_AT = 0;
                    IServo s = ServoController.Instance.ServoGroups[ServoDragging.GroupID].Servos[ServoDragging.ID];
                    ServoController.Instance.ServoGroups[ServoDragging.GroupID].Servos.RemoveAt(ServoDragging.ID);
                    ServoController.Instance.ServoGroups[GroupOver.ID].Servos.Insert(INSERT_AT, s);
                }

                //reset the dragging stuff
                DraggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }

            //If we are dragging and in the bottom or top area then scrtoll the list
            if (DraggingItem && RectScrollBottom.Contains(MousePosition))
                ControlsGUI.SetEditorScrollYPosition(ScrollPosition.y + (Time.deltaTime * 40));
            if (DraggingItem && RectScrollTop.Contains(MousePosition))
                ControlsGUI.SetEditorScrollYPosition(ScrollPosition.y - (Time.deltaTime * 40));
        }

        internal static void OnGUIEvery()
        {
            if (Disabled) return; //If the Drag and Drop is Disabled then just go back

            //disable resource dragging if we mouseup outside the window
            if (Event.current.type == EventType.mouseUp &&
                Event.current.button == 0 &&
                !WindowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                DraggingItem = false;
                GroupDragging = null;
                ServoDragging = null;
            }

            //If we are dragging, show what we are dragging
            if (DraggingItem && (ServoDragging != null || GroupDragging != null))
            {
                //set the Style
                //set and draw the text like a tooltip
                string message = "Moving ";
                if (GroupDragging != null) message += " group: " + GroupDragging.Name;
                if (ServoDragging != null) message += " servo: " + ServoDragging.Name;
                var labelPos = new Rect(Input.mousePosition.x - 5, Screen.height - Input.mousePosition.y - 9, 200, 22);
                GUI.Label(labelPos, message, StyleDragTooltip);

                //On top of everything
                GUI.depth = 0;
            }
        }

        //Variables to hold details of the drag stuff
    }
}