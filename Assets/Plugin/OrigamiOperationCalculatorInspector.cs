using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[CustomEditor(typeof(OrigamiOperationCalculator))]
public class OrigamiOperationCalculatorInspector : Editor
{
	OrigamiCreater targetOrigami = null;
	List<Color> m_op_colors = new List<Color>();
	void OnEnable()
	{
		targetOrigami = target as OrigamiCreater;

	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		bool bResetPolygon = GUILayout.Button("Recalculate", GUILayout.Width(100));
		targetOrigami.m_pointSample = EditorGUILayout.ObjectField("point:", targetOrigami.m_pointSample, typeof(GameObject), true) as GameObject;
		targetOrigami.m_paper = EditorGUILayout.ObjectField("paper:", targetOrigami.m_paper, typeof(OrigamiPaper), true) as OrigamiPaper;
		targetOrigami.ResetOperatorCount(EditorGUILayout.IntField("size", targetOrigami.m_operators.Count));
		GUILayout.Space(10);

		for (int i = 0; i != targetOrigami.m_operators.Count; ++i)
		{
			OrigamiOperator op = targetOrigami.m_operators[i];
			if(m_op_colors.Count < i+1)
			{
				m_op_colors.Add(Color.green);
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("index: " + i, GUILayout.Width(50));
			GUILayout.Label("valid:", GUILayout.Width(36));
			op.is_valid = EditorGUILayout.Toggle(op.is_valid, GUILayout.Width(10));
			GUILayout.Label("fold:", GUILayout.Width(30));
			op.need_fold = EditorGUILayout.Toggle(op.need_fold, GUILayout.Width(10));
			m_op_colors[i] = EditorGUILayout.ColorField(m_op_colors[i]);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("head:", GUILayout.Width(50));
			op.head_pos.x = EditorGUILayout.FloatField(op.head_pos.x);
			op.head_pos.y = EditorGUILayout.FloatField(op.head_pos.y);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("toe:", GUILayout.Width(50));
			op.toe_pos.x = EditorGUILayout.FloatField(op.toe_pos.x);
			op.toe_pos.y = EditorGUILayout.FloatField(op.toe_pos.y);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("touch:", GUILayout.Width(50));
			op.touch_dir.x = EditorGUILayout.FloatField(op.touch_dir.x);
			op.touch_dir.y = EditorGUILayout.FloatField(op.touch_dir.y);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);
		}

		if(bResetPolygon)
		{
			targetOrigami.ResetOrigamiPaper();
		}
	}

	void OnSceneGUI()
	{
		OrigamiCreater t = target as OrigamiCreater;
		if (t == null || t.gameObject == null)
			return;

		for (int i = 0; i != targetOrigami.m_operators.Count; ++i)
		{
			OrigamiOperator op = targetOrigami.m_operators[i];
			Debug.DrawLine(op.head_pos, op.toe_pos, m_op_colors[i]);
		}
	}
}
