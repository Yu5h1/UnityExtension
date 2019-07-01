using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;


public class SelectionRecorder : EditorWindow {

	public static List<int[]> m_ObjectsRecorder;
	public static List<int[]> ObjectsRecorder {
		get {
			if (m_ObjectsRecorder == null){
				m_ObjectsRecorder = new List<int[]>();
				var load = EditorPrefs.GetString("PreviousNextSelection");
				if (load == ""){
					Record();
				}else{
					var elements = load.Split('-');
					foreach (var ele in elements) {
						if (ele != ""){
							var sobjs = ele.Split(',');	
							int[] objs = new int[sobjs.Length];
							for (int i = 0; i < sobjs.Length; i++) {
								if (sobjs[i] != ""){
									objs[i] = int.Parse(sobjs[i]);
								}
							}
							if (objs.Length > 0){
								m_ObjectsRecorder.Add(objs);
							}
						}
					}
				}
			}

			return m_ObjectsRecorder;
		}
	}
	public static int current;
	static SelectionRecorder m_window;
	static SelectionRecorder window {
		get {
			if (m_window == null){
				// Get existing open window or if none, make a new one:
				SelectionRecorder.m_window = (SelectionRecorder)EditorWindow.GetWindow(typeof(SelectionRecorder));
				m_window.titleContent = new GUIContent("Previous Next Selection");
			}
			return m_window;
		}
	}
	static bool disableRecordSelected = false;
	static bool enablePreviouse { get { return ObjectsRecorder.Count > 0 && current > 0; } }
	static bool enableNext { get { return ObjectsRecorder.Count > 0 && current < ObjectsRecorder.Count-1; } }
	static bool ShowWindow = false;
	static System.Type InspectorType { get { return typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"); } }
	static System.Type DockAreaType { get { return typeof(Editor).Assembly.GetType("UnityEditor.DockArea"); } }  

	static List<MethodInfo> DockAreaMethods { get { return DockAreaType.GetMethods().ToList(); } }
	static List<Object> InspectorAreas { get { return Resources.FindObjectsOfTypeAll(DockAreaType).ToList().FindAll( d => new SerializedObject(d).FindProperty("m_Panes").GetArrayElementAtIndex(0).objectReferenceValue.GetType() == InspectorType); } }
	static Object InspectorArea {
		get {            
			var results = InspectorAreas;
			if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == InspectorType && results.Count > 1){
				foreach (var item in results) {
					var Panes = new SerializedObject(item).FindProperty("m_Panes");
					for (int i = 0; i < Panes.arraySize; i++) {
						if (Panes.GetArrayElementAtIndex(i).objectReferenceValue == EditorWindow.focusedWindow){
							return item;
						}
					}
				}
			}
			return results[0];
		} 
	}
	static SerializedProperty Panels {get{return new SerializedObject(InspectorArea).FindProperty("m_Panes");}}
	static int currentTab {get{return (int)FindDockAreaMethod("Int32 get_selected()").Invoke(InspectorArea,null);}}
//	private Vector2 scrollerPos = Vector2.zero;
	[MenuItem("Edit/Selection/Previous Next Selection List")]
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
			Record();
		}

	}
	void OnDisable(){
		ShowWindow = false;
	}
	static void Record(){
		ObjectsRecorder.Add(Selection.instanceIDs);
		string save = "";
		foreach (var item in ObjectsRecorder) {
			foreach (var obj in item) {
				save+=obj.ToString()+",";
			}
			save+="-";
		}
		EditorPrefs.SetString("PreviousNextSelection",save);
	}
	static void SelectionChangeEvent()
	{
		if (disableRecordSelected){
			disableRecordSelected = false;
		}else{
			if (current != ObjectsRecorder.Count-1){
				ObjectsRecorder.RemoveRange(current+1,ObjectsRecorder.Count-(current+1));
			}
			Record();
			if (ObjectsRecorder.Count > 21){	ObjectsRecorder.RemoveAt(0);	}
			current = ObjectsRecorder.Count-1;
		}
		if (ShowWindow){
			window.Repaint();
		}
	}
	[MenuItem("Edit/Selection/Previouse Selected &LEFT",true)]
	static bool CheckPreviouseSelected(){return enablePreviouse;}
	[MenuItem("Edit/Selection/Previouse Selected &LEFT")]
	static void PreviouseSelected(){
		current-=1;
		changeSelectionObjectsFromList();
	}
	[MenuItem("Edit/Selection/Next Selected &RIGHT",true)]
	static bool CheckNextSelected(){return enableNext;}
	[MenuItem("Edit/Selection/Next Selected &RIGHT")]
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
	[InitializeOnLoadMethod]
	private static void OnScriptsReloaded() {
		Selection.selectionChanged += () => SelectionRecorder.SelectionChangeEvent();	
	}
	[MenuItem("Edit/Selection/Add Lock Inspector %T")]
	static void AddLockInspectorTab()
	{
		if (InspectorAreas.Count > 0){
			var newInspector =  ScriptableObject.CreateInstance(InspectorType) as EditorWindow;
			InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public).GetSetMethod().Invoke(newInspector, new object[] { true });
			FindDockAreaMethod("Void AddTab(UnityEditor.EditorWindow)").Invoke(InspectorArea,new object[]{newInspector});
			newInspector.Show();
			newInspector.Focus();
		}
	}
	[MenuItem("Edit/Selection/Add Lock Inspector Window %#T")]
	static void AddLockInspectorWindowAtRight()
	{
		if (InspectorAreas.Count > 0){
			var a =  new SerializedObject(InspectorArea).FindProperty("m_Panes").GetArrayElementAtIndex(0).objectReferenceValue as EditorWindow;
			var newInspector =  ScriptableObject.CreateInstance(InspectorType) as EditorWindow;
			InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public).GetSetMethod().Invoke(newInspector, new object[] { true });
			newInspector.Show();
			SelectionRecorder.DockEditorWindow(a,newInspector);
			newInspector.Focus();
		}
	}
	[MenuItem("Edit/Selection/Close Focused Window %W")]
	static void CloseFocusedWindow()
	{
		if (focusedWindow != null){
			focusedWindow.Close();
		}
	}
	[MenuItem("Edit/Selection/Next Tab %PGUP")]
	static void ChangeNextTab()
	{
		if (InspectorAreas.Count > 0){
			if (Panels.arraySize > 1){
				int nextTab = currentTab-1 >= 0 ? currentTab-1 : Panels.arraySize-1 ;
				((EditorWindow)Panels.GetArrayElementAtIndex(nextTab).objectReferenceValue).Focus();
			}
		}
	}
	[MenuItem("Edit/Selection/Previous Tab %PGDN")]
	static void ChangePreviouseTab()
	{
		if (InspectorAreas.Count > 0){
			if (Panels.arraySize > 1){
				int nextTab = currentTab+1 < Panels.arraySize ? currentTab+1 : 0;
				((EditorWindow)Panels.GetArrayElementAtIndex(nextTab).objectReferenceValue).Focus();
			}
		}
	}
	[MenuItem("Edit/Selection/LockOrUnLock Inspector %L")]
	static void LockOrUnLockInspector()
	{
		if (InspectorAreas.Count > 0){
			var currentTabWindow = Panels.GetArrayElementAtIndex(currentTab).objectReferenceValue;
			bool result = (bool)InspectorType.GetProperty("isLocked").GetValue(currentTabWindow,null) ? false:true;
			InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public).GetSetMethod().Invoke(currentTabWindow, new object[] { result });
			InspectorType.GetMethods().ToList().Find(d => d.ToString() == "Void Repaint()").Invoke(currentTabWindow,null);
		}
	}
	static MethodInfo FindDockAreaMethod(string val){return DockAreaMethods.Find(d => d.ToString().Contains(val));}

	public static void DockEditorWindow(EditorWindow parent, EditorWindow child)
	{
		Vector2 screenPoint = parent.position.position + new Vector2(parent.position.width * .9f,parent.position.height/2);

		Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
		System.Type sv = assembly.GetType("UnityEditor.SplitView");

		var tp = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
		var opArea = tp.GetValue(parent);
		var ocArea = tp.GetValue(child);
		var tview = DockAreaType.GetProperty("parent", BindingFlags.Public | BindingFlags.Instance);
		var oview = tview.GetValue(opArea, null);
		var tDragOver = sv.GetMethod("DragOver", BindingFlags.Public | BindingFlags.Instance);
		var oDropInfo = tDragOver.Invoke(oview, new object[] { child, screenPoint });
		DockAreaType.GetField("s_OriginalDragSource", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, ocArea);

		var tPerformDrop = sv.GetMethod("PerformDrop", BindingFlags.Public | BindingFlags.Instance);
		tPerformDrop.Invoke(oview, new object[] { child, oDropInfo, null });

	}
}
