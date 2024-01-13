/*
MIT License

Copyright (c) 2024 Chen Chin Jieh

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace ChinJieh.SimpleUnityObjectHistory.Editor {
    public class SimpleUnityObjectHistoryWindow : EditorWindow {
        const int MAX_ITEMS = 20;

        Vector2 scrollPosition;
        QueueWithLast<ObjectHistoryEntry> objectEntries = new QueueWithLast<ObjectHistoryEntry>();

        GUIContent selectButtonContent;
        GUIContent openInspectorButtonContent;
        float buttonMaxWidth;

        struct ObjectHistoryEntry {
            public Object objectEntry;
        }

        class QueueWithLast<T> {
            Queue<T> queue = new Queue<T>();
            T lastElement;

            public void Enqueue(T element) {
                queue.Enqueue(element);
                lastElement = element;
            }

            public T Dequeue() {
                T element = queue.Dequeue();
                if (queue.Count == 0) {
                    lastElement = default(T);
                }

                return element;
            }

            public void Clear() {
                this.queue.Clear();
                lastElement = default(T);
            }

            public int Count => this.queue.Count;

            public IEnumerable<T> GetElements() {
                return queue;
            }

            public T GetLastQueued() { return this.lastElement; }
        }

        [MenuItem("Window/General/Simple Object History")]
        static void MenuItem_OpenObjectHistory() {
            SimpleUnityObjectHistoryWindow window = SimpleUnityObjectHistoryWindow.GetWindow<SimpleUnityObjectHistoryWindow>(title: "Simple Object History");
            window.Show();
        }

        private void OnEnable() {
            Selection.selectionChanged += HandleSelectionChanged;

            // Create cached gui contents
            this.selectButtonContent = new GUIContent(EditorGUIUtility.IconContent("scenepicking_pickable_hover"));
            this.selectButtonContent.tooltip = "Select Object";
            this.openInspectorButtonContent = new GUIContent(EditorGUIUtility.IconContent("Search Icon"));
            this.openInspectorButtonContent.tooltip = "Open Inspector";
            this.buttonMaxWidth = Mathf.Max(this.selectButtonContent.image.width, this.openInspectorButtonContent.image.width);

            LoadHistory();
        }

        private void OnDisable() {
            SaveHistory();

            Selection.selectionChanged -= HandleSelectionChanged;

            this.selectButtonContent = null;
            this.openInspectorButtonContent = null;
            this.objectEntries.Clear();
            this.buttonMaxWidth = 0;
        }

        void LoadHistory() {
            List<string> assetPaths = ObjectHistorySave.instance.GetAssetPaths();
            this.objectEntries.Clear();
            foreach (string assetPath in assetPaths) {
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (asset != null) {
                    this.objectEntries.Enqueue(new ObjectHistoryEntry() {
                        objectEntry = asset
                    });
                }
            }
        }

        void SaveHistory() {
            List<string> assetPaths = new List<string>(this.objectEntries.Count);
            foreach (ObjectHistoryEntry entry in this.objectEntries.GetElements()) {
                if (entry.objectEntry != null) {
                    string assetPath = AssetDatabase.GetAssetPath(entry.objectEntry);
                    if (!string.IsNullOrEmpty(assetPath)) {
                        assetPaths.Add(assetPath);
                    }
                }
            }

            ObjectHistorySave.instance.SetAssetPaths(assetPaths);
        }

        private void OnGUI() {
            string header = $"Object History ({this.objectEntries.Count} / {MAX_ITEMS})";
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            if (this.objectEntries.Count > 0) {
                if (GUILayout.Button("Clear History")) {
                    if (EditorUtility.DisplayDialog("Clear History?", "Are you sure you want to clear the object history?", "Yes", "No")) {
                        this.objectEntries.Clear();
                        Repaint();
                        return;
                    }
                }

                EditorGUILayout.Space(10);

                this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition, GUILayout.ExpandHeight(true));

                float maxHeight = EditorGUIUtility.singleLineHeight;
                EditorGUILayout.BeginVertical();
                foreach (ObjectHistoryEntry entry in this.objectEntries.GetElements()) {
                    EditorGUILayout.BeginHorizontal();

                    // Draw object label
                    EditorGUI.BeginDisabledGroup(true);
                    // A workaround to make disabled groups not faded - Set alpha multiplier to 2 (disabled groups are 0.5 alpha)
                    Color guiColor = GUI.color;
                    guiColor.a = 2;
                    GUI.color = guiColor;
                    EditorGUILayout.ObjectField(entry.objectEntry, typeof(Object), allowSceneObjects: true);
                    GUI.color = guiColor;
                    EditorGUI.EndDisabledGroup();

                    // Draw buttons
                    bool canOpen = entry.objectEntry != null;
                    EditorGUI.BeginDisabledGroup(!canOpen);
                    if (GUILayout.Button(selectButtonContent, GUILayout.MaxWidth(buttonMaxWidth), GUILayout.MaxHeight(maxHeight))) {
                        if (entry.objectEntry != null) {
                            Selection.activeObject = entry.objectEntry;
                            EditorGUIUtility.PingObject(entry.objectEntry);
                        }
                    }

                    if (GUILayout.Button(openInspectorButtonContent, GUILayout.MaxWidth(buttonMaxWidth), GUILayout.MaxHeight(maxHeight))) {
                        if (entry.objectEntry != null) {
                            EditorUtility.OpenPropertyEditor(entry.objectEntry);
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
            }
            else {
                EditorGUILayout.LabelField("There are no items in the history.");
            }
        }

        void HandleSelectionChanged() {
            Object selection = Selection.activeObject;
            if (selection != null) {
                // Ignore identical object if it is already at the end of the history
                if (objectEntries.Count > 0 && objectEntries.GetLastQueued().objectEntry == selection)
                    return;

                ObjectHistoryEntry newEntry = new ObjectHistoryEntry() {
                    objectEntry = selection
                };

                this.objectEntries.Enqueue(newEntry);
                if (this.objectEntries.Count > MAX_ITEMS) {
                    this.objectEntries.Dequeue();
                }

                // Move down to the bottom of the scroll position when changed
                this.scrollPosition = new(this.scrollPosition.x, this.position.height);

                Repaint();
            }
        }
    }

    /// <summary>
    /// Use this to temporarily store object histories while the window is reloading / closed
    /// </summary>
    public class ObjectHistorySave : ScriptableSingleton<ObjectHistorySave> {
        [SerializeField] List<string> assetPaths = new List<string>();

        public void SetAssetPaths(List<string> assetPaths) {
            this.assetPaths = assetPaths;
        }

        public List<string> GetAssetPaths() { return this.assetPaths; }
    }
}