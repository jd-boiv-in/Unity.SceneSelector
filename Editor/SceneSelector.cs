namespace JD.SceneSelector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// An editor script that places a drop down selection to quickly load Unity scenes, which are listed in the BuildSettings.
    /// </summary>
    [InitializeOnLoad]
    public class SceneSelector
    {
        private static ScriptableObject _toolbar;
        private static string[] _scenePaths;
        private static string[] _sceneNames;

        static SceneSelector()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            };
        }

        private static void Update()
        {
            if (_toolbar == null)
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;

                UnityEngine.Object[] toolbars =
                    UnityEngine.Resources.FindObjectsOfTypeAll(editorAssembly.GetType("UnityEditor.Toolbar"));
                _toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (_toolbar != null)
                {
                    var root = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(_toolbar);
                    var mRoot = rawRoot as VisualElement;
                    RegisterCallback("ToolbarZoneRightAlign", OnGUI);

                    void RegisterCallback(string root, Action cb)
                    {
                        var toolbarZone = mRoot.Q(root);
                        if (toolbarZone != null)
                        {
                            var parent = new VisualElement()
                            {
                                style =
                                {
                                    flexGrow = 1,
                                    flexDirection = FlexDirection.Row,
                                }
                            };
                            var container = new IMGUIContainer();
                            container.onGUIHandler += () => { cb?.Invoke(); };
                            parent.Add(container);
                            toolbarZone.Add(parent);
                        }
                    }
                }
            }

            if (_scenePaths == null || _scenePaths.Length != EditorBuildSettings.scenes.Length)
            {
                List<string> scenePaths = new List<string>();
                List<string> sceneNames = new List<string>();

                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    if (scene.path == null || scene.path.StartsWith("Assets") == false)
                        continue;

                    string scenePath = Application.dataPath + scene.path.Substring(6);

                    scenePaths.Add(scenePath);
                    sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
                }

                _scenePaths = scenePaths.ToArray();
                _sceneNames = sceneNames.ToArray();
            }

            if (_scenePaths != null && _scenePaths.Length == 0)
            {
                // If no scene have been added to build settings, yet, display all of them.
                var sceneGuids = AssetDatabase.FindAssets("t:scene");
                if (_scenePaths.Length != sceneGuids.Length)
                {
                    _scenePaths = sceneGuids.Select(assetGuid => AssetDatabase.GUIDToAssetPath(assetGuid)).ToArray();
                    _sceneNames = _scenePaths.Select(scenePath => Path.GetFileNameWithoutExtension(scenePath))
                        .ToArray();
                }
            }
        }

        private static void OnGUI()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                int sceneIndex = -1;

                for (int i = 0; i < _sceneNames.Length; ++i)
                {
                    if (sceneName == _sceneNames[i])
                    {
                        sceneIndex = i;
                        break;
                    }
                }

                int newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200.0f));
                if (newSceneIndex != sceneIndex)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(_scenePaths[newSceneIndex], OpenSceneMode.Single);
                    }
                }
            }
        }
    }
}