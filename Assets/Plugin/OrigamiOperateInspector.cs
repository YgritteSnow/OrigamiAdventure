using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OrigamiCreater))]
public class OrigamiOperateInspector : Editor
{
	OrigamiCreater targetOrigami = null;
	void OnEnable()
	{
		targetOrigami = target as OrigamiCreater;
	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		targetOrigami.m_pointSample = EditorGUILayout.ObjectField("point:", targetOrigami.m_pointSample, typeof(GameObject), true) as GameObject;
		targetOrigami.m_paper = EditorGUILayout.ObjectField("paper:", targetOrigami.m_paper, typeof(OrigamiPaper), true) as OrigamiPaper;
		targetOrigami.ResetOperatorCount(EditorGUILayout.IntField("size", targetOrigami.m_operators.Count));
		GUILayout.Space(10);
		foreach (OrigamiOperator op in targetOrigami.m_operators)
		{
			//EditorGUILayout.BeginHorizontal();
			op.head_pos = EditorGUILayout.Vector3Field("head_pos:", op.head_pos);
			op.toe_pos = EditorGUILayout.Vector3Field("toe_pos:", op.toe_pos);
			op.touch_dir = EditorGUILayout.Vector3Field("touch_dir:", op.touch_dir);
			EditorGUILayout.LabelField("valid:" + op.is_valid);
			//EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);
		}
	}
}
