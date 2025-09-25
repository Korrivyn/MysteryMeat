/*Script created by Pierre Stempin*/

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EmptyAtZeroCreator
{
    /// <summary>
    /// Provides shared helper methods for creating empty GameObjects at common anchor points via the Unity editor menu.
    /// </summary>
    public class EmptyCreator
    {
        public const string _GameObject = "GameObject";
        public const string _Tools = "Tools";

        public const string _Space = " ";
        public const string Slash = "/";

        public const string CreateEmpty_ = create + _Space + empty + _Space;
        public const string EmptyAtZeroCreator = empty + _Space + At + _Space + Zero + _Space + creator;
        public const string CreateEmptyChildAt_ = CreateEmpty_ + Child + _Space + At + _Space;

        private const string create = "Create";
        private const string empty = "Empty";
        private const string creator = "Creator";
        private const string Child = "Child";

        public const string At = "At";
        public const string Zero = "Zero";

        public const string ShortcutLetter = "N";
        public const string ControlSymbol = "%";
        public const string ShiftSymbol = "#";
        public const string AltSymbol = "&";

        /// <summary>
        /// Bridges editor logging to the runtime debug log system when available.
        /// </summary>
        private static class EditorDebugLogBridge
        {
            private static Type _debugType;
            private static MethodInfo _verboseMethod;

            private static Type ResolveDebugType()
            {
                if (_debugType != null)
                {
                    return _debugType;
                }

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type candidate = assembly.GetType("KitchenMysteryMeat.Systems.Logging.DebugLogSystem");
                    if (candidate != null)
                    {
                        _debugType = candidate;
                        break;
                    }
                }

                return _debugType;
            }

            private static MethodInfo ResolveVerboseMethod()
            {
                if (_verboseMethod != null)
                {
                    return _verboseMethod;
                }

                Type debugType = ResolveDebugType();
                if (debugType != null)
                {
                    _verboseMethod = debugType.GetMethod("LogVerbose", BindingFlags.Public | BindingFlags.Static);
                }

                return _verboseMethod;
            }

            public static void LogVerbose(string message)
            {
                MethodInfo method = ResolveVerboseMethod();
                if (method != null)
                {
                    try
                    {
                        method.Invoke(null, new object[] { message });
                        return;
                    }
                    catch (Exception)
                    {
                        // Fall back to Unity logging when the runtime assembly is not available.
                    }
                }

                Debug.Log(message);
            }
        }

        /// <summary>
        /// Emits verbose diagnostics through the shared logging bridge so menu actions can be traced.
        /// </summary>
        /// <param name="message">The message to record.</param>
        internal static void LogVerbose(string message)
        {
            EditorDebugLogBridge.LogVerbose(message);
        }

#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
        /// <summary>
        /// Creates a new empty GameObject and optionally parents or zeroes it based on the caller preferences.
        /// </summary>
        /// <param name="featureName">The menu feature name used for undo context.</param>
        /// <param name="hasToDeselect">Whether to deselect the current object before spawning the new one.</param>
        /// <param name="hasToResetLocalValues">Whether to zero the spawned object's local transform.</param>
        /// <param name="menuCommand">The originating menu command so parenting is applied correctly.</param>
        public static void CreateEmptyGameObject(string featureName, bool hasToDeselect, bool hasToResetLocalValues, MenuCommand menuCommand)
#else
        public static void CreateEmptyGameObject(string featureName, bool hasToDeselect, bool hasToResetLocalValues)
#endif
        {
            // Guard: reset the selection to avoid inheriting the wrong parent when requested.
            if (hasToDeselect)
            {
                Selection.activeGameObject = null;
            }

            // Create the new empty GameObject so designers can position it immediately.
            string gameObjectName = _GameObject;
            GameObject spawnedGameObject = new GameObject(gameObjectName);

            if (hasToDeselect)
            {
#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
                GameObjectUtility.SetParentAndAlign(spawnedGameObject, menuCommand.context as GameObject);
#endif
            }

            // Register undo information so the action can be reverted cleanly.
            Undo.RegisterCreatedObjectUndo(spawnedGameObject, featureName);

            if (Selection.activeGameObject != null)
            {
                // Parent the spawned object to the current selection when available.
                spawnedGameObject.transform.parent = Selection.activeGameObject.transform;

                if (hasToResetLocalValues)
                {
                    // Reset the local transform so anchors align with the parent.
                    spawnedGameObject.transform.localPosition = Vector3.zero;
                    spawnedGameObject.transform.localRotation = Quaternion.identity;
                    spawnedGameObject.transform.localScale = Vector3.one;
                }
            }

            // Select the spawned GameObject so further edits affect it immediately.
            Selection.activeGameObject = spawnedGameObject;

#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
            // Add a RectTransform when the parent uses UI layout components.
            if (spawnedGameObject.transform.parent != null)
            {
                RectTransform parentRectTransform = spawnedGameObject.transform.parent.GetComponent<RectTransform>();

                if (parentRectTransform != null)
                {
                    RectTransform rectTransform = spawnedGameObject.gameObject.AddComponent(typeof(RectTransform)) as RectTransform;
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
            }
#endif

            EditorDebugLogBridge.LogVerbose(string.Format("Created empty GameObject '{0}' via {1} with parent {2}.",
                spawnedGameObject.name,
                featureName,
                spawnedGameObject.transform.parent != null ? spawnedGameObject.transform.parent.name : "<none>"));
        }
    }
}
