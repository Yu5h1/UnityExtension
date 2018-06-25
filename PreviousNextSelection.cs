using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class PreviousNextSelection : EditorWindow {

	public static List<int[]> m_ObjectsRecorder;
	public static List<int[]> ObjectsRecorder {
		get {
			if (m_ObjectsRecorder == null){
				m_ObjectsRecorder = new List<int[]>();
				m_ObjectsRecorder.Add(Selection.instanceIDs);
			}

			return m_ObjectsRecorder;
		}
	}
	public static int current;
	static PreviousNextSelection m_window;
	static PreviousNextSelection window {
		get {
			if (m_window == null){
				// Get existing open window or if none, make a new one:
				PreviousNextSelection.m_window = (PreviousNextSelection)EditorWindow.GetWindow(typeof(PreviousNextSelection));
				m_window.titleContent = new GUIContent("Previous Next Selection");
			}
			return m_window;
		}
	}
	static bool disableRecordSelected = false;
	static bool enablePreviouse { get { return ObjectsRecorder.Count > 0 && current > 0; } }
	static bool enableNext { get { return ObjectsRecorder.Count > 0 && current < ObjectsRecorder.Count-1; } }
	static bool ShowWindow = false;
	private Vector2 scrollerPos = Vector2.zero;
	[MenuItem("Window/Previous Next Selection")]
	static void Init()
	{
		ShowWindow = true;
		window.Show();
	}
	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal ();
		EditorGUI.BeginDisabledGroup(!enablePreviouse);
		if (GUILayout.Button ("<")) {
			PreviouseSelected();
		}
		EditorGUI.EndDisabledGroup();
		EditorGUI.BeginDisabledGroup(!enableNext);
		if (GUILayout.Button (">")) {
			NextSelected();
		}
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndHorizontal ();

		if (ObjectsRecorder != null){
//			scrollerPos = EditorGUILayout.BeginScrollView(scrollerPos);
			for (int i = 0; i < ObjectsRecorder.Count; i++) {
				var ele = ObjectsRecorder[i];
				string countInfo = "";
				if (ele.Length > 1){
					countInfo = " Count : "+ele.Length.ToString();
				}else if (ele.Length != 0){
					countInfo = " \tInstanceID : " + ele[0].ToString();
				}
				GUIStyle s = new GUIStyle("label");
				if (current == i){
					
					s.richText = true;
					s.normal.textColor = Color.yellow;
					GUILayout.Label("<b>"+("Element."+i.ToString()+countInfo)+"</b>",s);
				}else{
					if (GUILayout.Button("Element."+i.ToString()+countInfo,s)){
						current = i;
						changeSelectionObjectsFromList();
					}
				}

//				if(ele.Length > 1){
//					EditorGUI.indentLevel = 2;
//					foreach (var item in ele) {
//						EditorGUILayout.LabelField(item.ToString());
//					}
//				}

			}
//			EditorGUILayout.EndScrollView();
		}
		Rect clearPos = new Rect(0,window.position.height-20,window.position.width,20);

		if (GUI.Button (clearPos,"Clear")) {
			current = 0;
			ObjectsRecorder.Clear();
			ObjectsRecorder.Add(Selection.instanceIDs);
		}

	}
	void OnDisable(){
		ShowWindow = false;
	}
	static void SelectionChangeEvent()
	{
		if (disableRecordSelected){
			disableRecordSelected = false;
		}else{
			if (current != ObjectsRecorder.Count-1){
				ObjectsRecorder.RemoveRange(current+1,ObjectsRecorder.Count-(current+1));
			}
			ObjectsRecorder.Add(Selection.instanceIDs);
			if (ObjectsRecorder.Count > 21){	ObjectsRecorder.RemoveAt(0);	}
			current = ObjectsRecorder.Count-1;
		}
		if (ShowWindow){
			window.Repaint();
		}
	}
	[MenuItem("Window/Previouse Selected &LEFT",true)]
	static bool CheckPreviouseSelected(){return enablePreviouse;}
	[MenuItem("Window/Previouse Selected &LEFT")]
	static void PreviouseSelected(){
		current-=1;
		changeSelectionObjectsFromList();

	}
	[MenuItem("Window/Next Selected &RIGHT",true)]
	static bool CheckNextSelected(){return enableNext;}
	[MenuItem("Window/Next Selected &RIGHT")]
	static void NextSelected(){
		current+=1;
		changeSelectionObjectsFromList();

	}
	static void changeSelectionObjectsFromList(){
		disableRecordSelected = true;
		Object[] objs = new Object[ObjectsRecorder[current].Length];
		for (int i = 0; i < ObjectsRecorder[current].Length; i++) {
			objs[i] = EditorUtility.InstanceIDToObject(ObjectsRecorder[current][i]);
		}
		Selection.objects = objs;
	}
	[UnityEditor.Callbacks.DidReloadScripts]
	private static void OnScriptsReloaded() { 
		var result = Selection.selectionChanged.GetInvocationList().ToList().Find(d => d.Method.Name == "OnSelectionChangeAction");
		if (result == null){
			Selection.selectionChanged += () => PreviousNextSelection.SelectionChangeEvent();
		}
	}
}
