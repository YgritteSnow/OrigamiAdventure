using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OrigamiOperationCalculator))]
public class OrigamiOperationHandler : MonoBehaviour
{
	private OrigamiOperationCalculator m_calculator = null;

	// 鼠标拖拽
	private bool m_isPressing = false;
	private Vector2 m_press_startPos = Vector2.zero;
	private Vector2 m_press_curPos = Vector2.zero;
	private bool m_is_distance_valid = false;

	enum FoldHandlState
	{
		Idle = 0, // 空闲
		FoldAll = 1, // 整层折叠
		FoldTop = 2, // 最上层折叠
		FoldInside = 3, // 内侧某层折叠
	}
	private FoldHandlState m_cur_state = FoldHandlState.Idle;

	// 反转视角
	private GameObject m_mainCamera = null; // 正面摄像机
	private GameObject m_backCamera = null; // 背面摄像机
	private bool m_is_forward = true; // 是否是背面模式

	// 撤销折叠
	private bool m_isReverting = false; // 是否按下了撤销按键

	// 单层折叠
	private bool m_isFoldingOne = false; // 是否按下了单层折叠按键

	// Use this for initialization
	void Awake()
	{
		InitBackCamera();

		if (m_calculator == null)
		{
			m_calculator = GetComponent<OrigamiOperationCalculator>();
		}
	}

	// Update is called once per frame
	void Update()
	{
		OnUpdateForBackward();
		OnUpdateForMouseDrag();
		OnUpdateForRevertFold();
		OnUpdateForFoldOne();
	}

	#region 鼠标拖拽控制
	void OnUpdateForMouseDrag()
	{
		if (Input.GetMouseButton(0))
		{
			Vector2 a = new Vector2(1, 2);
			Vector2 b = new Vector2(-3, 5);
			b.Scale(a);
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
		if(m_isFoldingOne)
		{
			m_cur_state = FoldHandlState.FoldTop;
			OnPressDown_foldTop();
		}
		else if(m_isReverting)
		{
			m_cur_state = FoldHandlState.FoldInside;
			OnPressDown_foldInside();
		}
		else
		{
			m_cur_state = FoldHandlState.FoldAll;
			OnPressDown_foldAll();
		}
	}

	void OnPressUp()
	{
		m_press_startPos = Vector2.zero;
		m_press_curPos = Vector2.zero;
		if(m_cur_state == FoldHandlState.FoldAll)
		{
			OnPressUp_foldAll();
		}
		else if(m_cur_state == FoldHandlState.FoldInside)
		{
			OnPressUp_foldInside();
		}
		else if(m_cur_state == FoldHandlState.FoldTop)
		{
			OnPressUp_foldTop();
		}
		m_cur_state = FoldHandlState.Idle;
	}

	void OnPressing()
	{
		if(m_cur_state == FoldHandlState.FoldAll)
		{
			OnPressing_foldAll();
		}
		else if(m_cur_state == FoldHandlState.FoldInside)
		{
			OnPressing_foldInside();
		}
		else if(m_cur_state == FoldHandlState.FoldTop)
		{
			OnPressing_foldTop();
		}
	}
	#endregion

	#region FoldAll 的操作响应
	void OnPressDown_foldAll() { }

	void OnPressing_foldAll()
	{
		m_press_curPos = GetMousePos();
		if ((m_press_curPos - m_press_startPos).sqrMagnitude < 0.0001)
		{
			return;
		}

		Vector2 mid_pos = (m_press_startPos + m_press_curPos) / 2;
		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 edge_dir = new Vector2(fold_dir.y, -fold_dir.x);

#if UNITY_EDITOR
		Debug.DrawRay(m_press_startPos, fold_dir * 2, Color.green);
		Debug.DrawLine(m_press_startPos, m_press_curPos, Color.green);
#endif

		if (!m_is_distance_valid)
		{
			m_calculator.AddOperation(mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
			m_is_distance_valid = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
		}
	}

	void OnPressUp_foldAll()
	{
		m_is_distance_valid = false;
		m_calculator.ConfirmAddOperation();
	}
	#endregion
	
	#region FoldTop 操作响应
	void OnPressDown_foldTop()
	{
	}

	void OnPressing_foldTop()
	{
		m_press_curPos = GetMousePos();
		if ((m_press_curPos - m_press_startPos).sqrMagnitude < 0.0001)
		{
			return;
		}

		Vector2 mid_pos = (m_press_startPos + m_press_curPos) / 2;
		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 edge_dir = new Vector2(fold_dir.y, -fold_dir.x);

		if (!m_is_distance_valid)
		{
			m_calculator.AddOperationOnlyTop(m_press_startPos, mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
			m_is_distance_valid = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
		}
	}

	void OnPressUp_foldTop()
	{
		m_is_distance_valid = false;
		m_calculator.ConfirmAddOperation();
	}
	#endregion

	#region FoldInside 的操作响应
	void OnPressDown_foldInside() { }

	void OnPressing_foldInside()
	{
		m_press_curPos = GetMousePos();
		if ((m_press_curPos - m_press_startPos).sqrMagnitude < 0.0001)
		{
			return;
		}

		Vector2 mid_pos = (m_press_startPos + m_press_curPos) / 2;
		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 edge_dir = new Vector2(fold_dir.y, -fold_dir.x);

		if (!m_is_distance_valid)
		{
			m_calculator.AddOperationInLeastChange(m_press_startPos, mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
			m_is_distance_valid = true;
		}
		else
		{
			m_calculator.ChangeOperationInLeaseChange(mid_pos, mid_pos - edge_dir, fold_dir, m_is_forward);
		}
	}

	void OnPressUp_foldInside()
	{
		m_is_distance_valid = false;
		m_calculator.ClearLastOperationInLeaseChange();
	}
	#endregion

	#region 翻转控制
	void OnUpdateForBackward()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			m_is_forward = false;
			OnUpsideChange();
		}
		if (Input.GetKeyUp(KeyCode.R))
		{
			m_is_forward = true;
			OnUpsideChange();
		}
	}

	void InitBackCamera()
	{
		m_mainCamera = GameObject.Find("main_camera");
		m_backCamera = GameObject.Find("back_camera");

		Camera backCamera = m_backCamera.GetComponent<Camera>();
		Matrix4x4 cam_mat = backCamera.worldToCameraMatrix;
		cam_mat.m00 = -cam_mat.m00;
		backCamera.worldToCameraMatrix = cam_mat;
		m_backCamera.SetActive(false);
	}

	void OnUpsideChange()
	{
		if (!m_is_forward)
		{
			m_backCamera.SetActive(!m_is_forward);
			m_mainCamera.SetActive(m_is_forward);
		}
		else
		{
			m_mainCamera.SetActive(m_is_forward);
			m_backCamera.SetActive(!m_is_forward);
		}
	}
	#endregion

	#region 撤销折叠控制
	void OnUpdateForRevertFold()
	{
		if (Input.GetKeyDown(KeyCode.D))
		{
			m_isReverting = true;
		}
		if (Input.GetKeyUp(KeyCode.D))
		{
			m_isReverting = false;
		}
	}
	#endregion

	#region 单层折叠控制
	void OnUpdateForFoldOne()
	{
		if (Input.GetKeyDown(KeyCode.F))
		{
			m_isFoldingOne = true;
		}
		if (Input.GetKeyUp(KeyCode.F))
		{
			m_isFoldingOne = false;
		}
	}
	#endregion
}
