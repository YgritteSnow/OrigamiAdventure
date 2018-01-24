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
		is_forward = true;
	}
	public Vector2 head_pos;
	public Vector2 toe_pos;
	public Vector2 touch_dir;
	public bool is_forward;

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
}

public class OrigamiOperationNode
{
	private OrigamiOperationNode()
	{
		trans = null;
		fold_angle = 0;
		polygon = default(PolygonData);

		fold_order = 0;
		fold_forward = true;
		fold_edgeid = 0;
	}

	public OrigamiOperationNode(Transform t)
	{
		trans = t;
		fold_angle = 0;
		polygon = default(PolygonData);

		fold_order = 0;
		fold_forward = true;
		fold_edgeid = 0;
	}

	public Transform trans;
	public float fold_angle; // 折叠的角度

	public PolygonData polygon; // 每个结点仅有1个polygon
	public int fold_order; // 对于本层而言，其折叠层级
	public bool fold_forward; // 当时折叠时的正反状态
	public int fold_edgeid; // 折叠时新增的edge_id
}

/// <summary>
/// 计算和保存操作结果
/// </summary>
public class OrigamiOperationCalculator : MonoBehaviour {
	public GameObject m_sample;

	List<OrigamiOperator> m_operators = new List<OrigamiOperator>();
	JBinaryTree<OrigamiOperationNode> m_operatorTree;

	JBinaryTree<OrigamiOperationNode> m_revertingNode = null; // 当revert的时候，所使用的最高结点
	int m_revertingDepth; // revert时的深度

	// Use this for initialization
	void Start ()
	{
		OrigamiOperationNode root = GenerateObjAndNode("root", transform, JUtility.GetRectPolygon(1,1));
		m_operatorTree = new JBinaryTree<OrigamiOperationNode>(true, root);
	}

	#region 对外的添加、修改、删除函数
	// 添加一个操作
	public bool AddOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;
		m_operators.Add(op);

		TraverseSetLastOperator(op);

		return true;
	}
	// 修改最后一个操作
	public bool ChangeLastOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		if(m_operators.Count == 0)
		{
			Debug.LogError("no operator!");
		}

		OrigamiOperator op = m_operators[m_operators.Count - 1];
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		if (m_revertingNode == null)
		{
			TraverseSetLastOperator(op);
		}
		else
		{
			TraverseOperator(m_revertingNode, m_revertingDepth, op);
		}

		return true;
	}
	// 撤销某一个操作，并将新增加的操作作为最后一个操作
	public bool RevertOperation(Vector2 world_cur_pos, bool is_forward)
	{
		// 获得所触摸的多边形
		JBinaryTree<OrigamiOperationNode> touching_node = GetTouchingNode(world_cur_pos, is_forward);
		if(touching_node == null) // 如果没有触摸到任何多边形
		{
			return false;
		}

		// 向上查找直至找到一个右侧节点（正面观察+正面折叠）或左侧节点（反+正）
		int last_searched_depth = 0;
		int cur_searched_depth;
		do
		{
			JBinaryTree<OrigamiOperationNode> reverting_parent = GetCanRevertParent(touching_node, is_forward, last_searched_depth, out cur_searched_depth);
			if (reverting_parent == null)
			{
				return false;
			}
			last_searched_depth = cur_searched_depth;

			// 检查该多边形是否可以移动
			if (CheckCanMoveNode(reverting_parent, cur_searched_depth))
			{
				RevertNode(reverting_parent, cur_searched_depth);
				return true;
			}
		} while (true);
	}
	public void ClearRevertInfo()
	{
		m_revertingNode = null;
		m_revertingDepth = 0;
	}
	#endregion

	#region 使对树的operation生效，包括遍历、遍历时的操作
	bool TraverseSetLastOperator(OrigamiOperator op)
	{
		m_operatorTree.TraverseOneDepthWithCheck(m_operators.Count - 1
			, delegate (JBinaryTree<OrigamiOperationNode> node)
				{
					return SetOperatorForNode(m_operators.Count, node, op);
				});

		return true;
	}

	bool TraverseOperator(JBinaryTree<OrigamiOperationNode> root, int depth, OrigamiOperator op)
	{
		root.TraverseOneDepthWithCheck(depth
			, delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				return SetOperatorForNode(m_operators.Count, node, op);
			});

		return true;
	}

	OrigamiOperationNode GenerateObjAndNode(string name, Transform parent, PolygonData p)
	{
		GameObject new_obj = GameObject.Instantiate(m_sample);
		new_obj.name = name;
		new_obj.transform.parent = parent;
		new_obj.GetComponent<Polygon>().SetPolygon(p);
		OrigamiOperationNode res = new OrigamiOperationNode(new_obj.transform);
		res.polygon = p;
		res.fold_angle = 0;
		return res;
	}

	int GetLeftDepth(int fold_order, int parent_order, bool is_forward)
	{
		if(is_forward)
		{
			return parent_order;
		}
		else
		{
			return (1 << (fold_order-1)) + parent_order;
		}
	}
	int GetRightDepth(int fold_order, int parent_order, bool is_forward)
	{
		if (is_forward)
		{
			return (1 << fold_order) - 1 - parent_order;
		}
		else
		{
			return (1 << (fold_order - 1)) - 1 - parent_order;
		}
	}
	#region 这个函数太羞耻了必须折叠起来折叠起来折叠起来
	/// <summary>
	/// 为node计算并添加操作。
	/// 通常情况下不应该在遍历时直接修改树，但是这里的操作内容不会影响遍历结果，所以直接这么搞了嗯嗯嗯
	/// </summary>
	bool SetOperatorForNode(int fold_order, JBinaryTree<OrigamiOperationNode> node, OrigamiOperator op)
	{
		Vector2 local_head_pos = node.Data.trans.InverseTransformPoint(op.head_pos);
		Vector2 local_toe_pos = node.Data.trans.InverseTransformPoint(op.toe_pos);
		Vector2 local_fold_dir = node.Data.trans.InverseTransformVector(op.normalised_touch_dir);
		int side = node.Data.polygon.CheckAllOneSide(local_head_pos, local_toe_pos, local_fold_dir);
		if(side > 0) // 不需要翻折的一侧
		{
			OrigamiOperationNode left;
			if (node.HasLeftChild())
			{
				left = node.GetLeftChild();
				left.polygon = node.Data.polygon;
				left.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
				left.trans.GetComponent<Polygon>().SetPolygon(left.polygon);
			}
			else
			{
				left = GenerateObjAndNode("child_all_left", node.Data.trans, node.Data.polygon);
			}
			left.fold_edgeid = 0;
			left.fold_order = GetLeftDepth(fold_order, node.Data.fold_order, op.is_forward);
			left.fold_forward = op.is_forward;
			left.trans.GetComponent<Polygon>().SetPolygonDepth(left.fold_order);

			node.SetLeftChild(left);

			if (node.HasRightChild())
			{
				GameObject.Destroy(node.GetRightChild().trans.gameObject);
				node.SetRightChildNull();
			}
		}
		else if (side < 0) // 需要翻折的一侧
		{
			OrigamiOperationNode right;
			if (node.HasRightChild())
			{
				right = node.GetRightChild();
				right.polygon = node.Data.polygon;
				right.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
				right.trans.GetComponent<Polygon>().SetPolygon(right.polygon);
			}
			else
			{
				right = GenerateObjAndNode("child_all_right", node.Data.trans, node.Data.polygon);
			}
			right.fold_edgeid = 0;
			right.fold_order = GetRightDepth(fold_order, node.Data.fold_order, op.is_forward);
			right.fold_forward = op.is_forward;
			right.trans.GetComponent<Polygon>().SetPolygonDepth(right.fold_order);

			node.SetRightChild(right);
			right.trans.RotateAround(op.head_pos, op.toe_pos - op.head_pos, 180);

			if (node.HasLeftChild())
			{
				GameObject.Destroy(node.GetLeftChild().trans.gameObject);
				node.SetLeftChildNull();
			}
		}
		else
		{
			PolygonData left_p, right_p;
			int cut_edge_id;
			if (node.Data.polygon.CutPolygon(fold_order, local_head_pos, local_toe_pos, local_fold_dir, out left_p, out right_p, out cut_edge_id))
			{
				OrigamiOperationNode right;
				if (node.HasRightChild())
				{
					right = node.GetRightChild();
					right.polygon = right_p;
					right.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
					right.trans.GetComponent<Polygon>().SetPolygon(right_p);
				}
				else
				{
					right = GenerateObjAndNode("child_cut_right", node.Data.trans, right_p);
				}
				OrigamiOperationNode left;
				if (node.HasLeftChild())
				{
					left = node.GetLeftChild();
					left.polygon = left_p;
					left.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
					left.trans.GetComponent<Polygon>().SetPolygon(left_p);
				}
				else
				{
					left = GenerateObjAndNode("child_cut_left", node.Data.trans, left_p);
				}

				left.fold_edgeid = cut_edge_id;
				left.fold_order = GetLeftDepth(fold_order, node.Data.fold_order, op.is_forward);
				left.fold_forward = op.is_forward;
				left.trans.GetComponent<Polygon>().SetPolygonDepth(left.fold_order);
				node.SetLeftChild(left);

				right.fold_edgeid = cut_edge_id;
				right.fold_order = GetRightDepth(fold_order, node.Data.fold_order, op.is_forward);
				right.trans.GetComponent<Polygon>().SetPolygonDepth(right.fold_order);
				right.fold_forward = op.is_forward;
				node.SetRightChild(right);
				
				right.trans.RotateAround(op.head_pos, op.toe_pos - op.head_pos, 180);
			}
			else
			{
				Debug.LogError("Cannot cut polygon!!!");
			}
		}

		node.Data.trans.GetComponent<Polygon>().ShowPolygon(false);
		return true;
	}
	#endregion
	#endregion

	#region 撤销所用的一些判断函数
	JBinaryTree<OrigamiOperationNode> GetTouchingNode(Vector2 world_cur_pos, bool is_forward)
	{
		JBinaryTree<OrigamiOperationNode> result = null;
		// 从上到下遍历多边形，查找触摸点所落在的结点
		m_operatorTree.TraverseOneDepthByOrder(m_operators.Count - 1,
			delegate (OrigamiOperationNode node)
			{
				return is_forward ? node.fold_order : -node.fold_order;
			},
			delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				Vector2 local_cur_pos = node.Data.trans.InverseTransformPoint(world_cur_pos);
				if (node.Data.polygon.IsPointInside(local_cur_pos))
				{
					result = node;
					return false;
				}
				return true;
			});

		return result;
	}

	JBinaryTree<OrigamiOperationNode> GetCanRevertParent(JBinaryTree<OrigamiOperationNode> origin, bool is_forward, int origin_searched_depth, out int searched_depth)
	{// 向上查找直至找到一个右侧节点
		searched_depth = origin_searched_depth;
		do {
			if (origin == m_operatorTree || origin.parent_node == null)
			{
				return null;
			}

			bool use_right = is_forward == origin.Data.fold_forward;
			if (use_right == origin.IsRight)
			{
				break;
			}
			origin = origin.parent_node;
			++searched_depth;
		} while (true);
		return origin;
	}

	bool CheckCanMoveNode(JBinaryTree<OrigamiOperationNode> origin, int origin_height)
	{// 检查节点所用的edge_id是否被超过1个叶子所使用，超过说明已经被二次折叠了
		int origin_edge_id = origin.Data.fold_edgeid;
		int used_count = 0;
		origin.TraverseOneDepthWithCheck(origin_height, delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				foreach(PolygonEdge e in node.Data.polygon.Edges)
				{
					if(e.ID == origin_edge_id)
					{
						++used_count;
						break;
					}
				}
				return used_count <= 1;
			});
		return false;
	}

	void RevertNode(JBinaryTree<OrigamiOperationNode> node, int node_height)
	{ }
	#endregion
}