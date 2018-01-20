using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每一步操作的信息
/// </summary>
public class OrigamiOperator
{
	public OrigamiOperator()
	{
		head_pos = Vector3.zero;
		toe_pos = Vector3.zero;
		touch_dir = Vector3.zero;
		is_valid = true;
		need_fold = true;
	}
	public Vector2 head_pos;
	public Vector2 toe_pos;
	public Vector2 touch_dir;

	public Vector2 normalised_touch_dir
	{
		get
		{
			Vector2 res = head_pos - toe_pos;
			res = new Vector2(res.y, -res.x);
			if (Vector2.Dot(res, touch_dir) < 0)
			{
				res = -res;
			}
			res.Normalize();
			return res;
		}
	}
	public bool is_valid;
	public bool need_fold;
}

/// <summary>
/// 计算和保存操作结果
/// </summary>
public class OrigamiOperationCalculator : MonoBehaviour {
	public struct OrigamiOperationNode
	{
		Polygon polygon; // 每个结点仅有1个polygon
	}
	public Tree<OrigamiOperationNode> m_tree;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
