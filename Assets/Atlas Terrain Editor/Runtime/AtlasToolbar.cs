#if UNITY_EDITOR
#if UNITY_2020_2_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace Atlas.Unity {

    [Overlay(typeof(SceneView), overlayID, toolbarDisplayName,"",true)]
    internal class AtlasToolbar : IMGUIOverlay {

        public const string overlayID = "Atlas Toolbar";

        const string toolbarDisplayName = "Atlas";

        private GUIContent contentOn;
        private GUIContent contentOff;

        public static Func<bool> onInterceptActivate;
        public static Func<bool> onInterceptDeactivate;

        public override void OnCreated() {

            contentOn = new GUIContent(AtlasUtils.LoadIcon("", "l:Atlas_activationtool_icon"),"Start Edit mode.");
            contentOff = new GUIContent(AtlasUtils.LoadIcon("", "l:Atlas_activationtool_icon_off"), "Stop Edit mode.");

        }

        public override void OnGUI() {

            if(GUILayout.Button(contentOn,GUILayout.Width(36))){

                Activate();

            }

            if(GUILayout.Button(contentOff, GUILayout.Width(36))) {

                Deactivate();

            }

        }

        private void Activate() {

            if (onInterceptActivate != null) {

                if (onInterceptActivate()) {

                    return;

                }

            }

            var allPreviewVolumes = Resources.FindObjectsOfTypeAll<AtlasUnityPreviewVolume>();

            var havePreviewVolume = allPreviewVolumes != null && allPreviewVolumes.Length > 0;

            if (havePreviewVolume == false) {

                EditorUtility.DisplayDialog("Atlas Preview Volume Activation Tool", "Atlas wants to start editing.\nBut no preview volume was found.", "ok");

                return;

            }

            AtlasUtils.RememberTerrainDrawStates(true);

            foreach (var i in allPreviewVolumes) {

                if (i.isActiveAndEnabled) {

                    if (i.editing == false) {

                        if ((i.editing && i.FirstTimeRenderProceed()) || i.editing == false) {

                            i.OnDisable();

                            i.editing = !i.editing;

                            i.editedOnce = true;

                            i.OnEnable();

                        }

                    }

                }

            }

        }

        private void Deactivate() {

            if (onInterceptDeactivate != null) {

                if (onInterceptDeactivate()) {

                    return;

                }

            }

            var allPreviewVolumes = Resources.FindObjectsOfTypeAll<AtlasUnityPreviewVolume>();

            var havePreviewVolume = allPreviewVolumes != null && allPreviewVolumes.Length > 0;

            if (havePreviewVolume == false) {

                EditorUtility.DisplayDialog("Atlas Preview Volume Activation Tool", "Atlas wants to stop editing.\nBut no preview volume was found.", "ok");

                return;

            }

            AtlasUtils.RememberTerrainDrawStates(false);

            foreach (var i in allPreviewVolumes) {

                if (i.isActiveAndEnabled) {

                    if (i.editing == true) {

                        if ((i.editing && i.FirstTimeRenderProceed()) || i.editing == false) {

                            i.OnDisable();

                            i.editing = !i.editing;

                            i.editedOnce = true;

                            i.OnEnable();

                        }

                    }

                }

            }

        }
    
    }

}

#endif
#endif