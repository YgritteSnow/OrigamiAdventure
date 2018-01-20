using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OrigamiOperationCalculator))]
public class OrigamiOperationHandler : MonoBehaviour {
	private OrigamiOperationCalculator m_calculator = null;
	public bool m_isPressing = false;
	public Vector2 m_press_startPos = Vector2.zero;
	public Vector2 m_press_curPos = Vector2.zero;

	// Use this for initialization
	void Awake () {
		if(m_calculator == null)
		{
			m_calculator = GetComponent<OrigamiOperationCalculator>();
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButton(0))
		{
			if (!m_isPressing)
			{
				OnPressDown();
			}
			OnPressing();
		}
		else if (!Input.GetMouseButton(0) && m_isPressing)
		{
			OnPressUp();
		}
	}

	Vector2 GetMousePos()
	{
		return Input.mousePosition;
	}

	void OnPressDown()
	{
		m_press_startPos = GetMousePos();
		m_press_curPos = m_press_startPos;

		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 fold_edge_dir = new Vector2(fold_dir.y, -fold_dir.x);
		
		m_calculator.AddOperation(fold_edge_dir, -fold_edge_dir, fold_dir);
	}

	void OnPressUp()
	{
		// todo 决定结果
		m_press_startPos = Vector2.zero;
		m_press_curPos = Vector2.zero;
	}

	void OnPressing()
	{
		m_press_curPos = GetMousePos();

		m_calculator.ChangeLastOperation(fold_edge_dir, -fold_edge_dir, fold_dir);
	}
}
