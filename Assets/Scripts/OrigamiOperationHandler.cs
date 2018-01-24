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

	public GameObject m_mainCamera = null; // 正面摄像机
	public GameObject m_backCamera = null; // 背面摄像机
	public bool m_isBackward = false; // 是否是背面模式

	// Use this for initialization
	void Awake() {
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
		m_isFolding = false;
	}

	void OnPressUp()
	{
		m_press_startPos = Vector2.zero;
		m_press_curPos = Vector2.zero;
	}

	void OnPressingDebug()
	{
		if (!m_isFolding)
		{
			m_calculator.AddOperation(new Vector2(1, 2f), new Vector2(-1, 2f), Vector2.up, m_isBackward);
			m_isFolding = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(new Vector2(1, 2f), new Vector2(-1, 2f), Vector2.up, m_isBackward);
		}
	}

	void OnPressing()
	{
		m_press_curPos = GetMousePos();
		if ((m_press_curPos - m_press_startPos).sqrMagnitude < 0.01)
		{
			return;
		}

		//if (true) { OnPressingDebug(); return; } // debug用

		Vector2 mid_pos = (m_press_startPos + m_press_curPos) / 2;
		Vector2 fold_dir = m_press_curPos - m_press_startPos;
		fold_dir.Normalize();
		Vector2 edge_dir = new Vector2(fold_dir.y, -fold_dir.x);

#if UNITY_EDITOR
		Debug.DrawRay(m_press_startPos, fold_dir * 2, Color.green);
		Debug.DrawLine(m_press_startPos, m_press_curPos, Color.green);
#endif

		if (!m_isFolding)
		{
			m_calculator.AddOperation(mid_pos, mid_pos - edge_dir, fold_dir, m_isBackward);
			m_isFolding = true;
		}
		else
		{
			m_calculator.ChangeLastOperation(mid_pos, mid_pos - edge_dir, fold_dir, m_isBackward);
		}
	}
	#endregion

	#region 翻转控制
	void OnUpdateForBackward()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			m_isBackward = true;
			OnUpsideChange();
		}
		if (Input.GetKeyUp(KeyCode.R))
		{
			m_isBackward = false;
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
		if(m_isBackward)
		{
			m_backCamera.SetActive(m_isBackward);
			m_mainCamera.SetActive(!m_isBackward);
		}
		else
		{
			m_mainCamera.SetActive(!m_isBackward);
			m_backCamera.SetActive(m_isBackward);
		}
	}
	#endregion
}
