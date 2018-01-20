using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OrigamiOperationCalculator))]
public class OrigamiOperationHandler : MonoBehaviour {
	private OrigamiOperationCalculator m_calculator = null;

	public bool m_isPressing = false;
	public Vector2 m_press_startPos = Vector2.zero;
	public Vector2 m_press_curPos = Vector2.zero;
	public bool m_isFolding = false;

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
				m_isPressing = true;
				OnPressDown();
			}
			OnPressing();
		}
		else if (!Input.GetMouseButton(0) && m_isPressing)
		{
			m_isPressing = false;
			OnPressUp();
		}
	}

	Vector2 GetMousePos()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float param = ray.origin.z / ray.direction.z;
		Vector3 collide_point = ray.origin - param * ray.direction;
		return collide_point;
	}

	void OnPressDown()
	{
		m_press_startPos = GetMousePos();
		m_press_curPos = m_press_startPos;
		m_isFolding = false;
	}

	void OnPressUp()
	{
		m_press_startPos = Vector2.zero;
		m_press_curPos = Vector2.zero;
		m_isFolding = false;
	}

	void OnPressing2()
	{
		if (!m_isFolding)
		{
			m_calculator.AddOperation(Vector2.right, -Vector2.right, Vector2.up);
			m_isFolding = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(Vector2.right, -Vector2.right, Vector2.up);
		}
	}

	void OnPressing()
	{
		m_press_curPos = GetMousePos();
		if((m_press_curPos - m_press_startPos).sqrMagnitude < 0.01)
		{
			return;
		}

		Vector2 mid_pos = (m_press_startPos + m_press_curPos) / 2;
		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 edge_dir = new Vector2(fold_dir.y, -fold_dir.x);

		Debug.DrawLine(m_press_startPos, m_press_curPos, Color.green);
		Debug.DrawRay(mid_pos, edge_dir * 2, Color.red);

		if (!m_isFolding)
		{
			m_calculator.AddOperation(mid_pos, mid_pos + edge_dir, fold_dir);
			m_isFolding = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(mid_pos, mid_pos + edge_dir, fold_dir);
		}
	}
}
