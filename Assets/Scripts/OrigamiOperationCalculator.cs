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
		head_pos = new Vector3(-9999, 1);
		toe_pos = new Vector3(-9999, -1);
		_touch_dir = Vector3.right;
		is_forward = true;
	}
	public Vector2 head_pos;
	public Vector2 toe_pos;
	public bool is_forward;

	private Vector2 _touch_dir;
	public Vector2 touch_dir
	{
		get { return _touch_dir; }
		set
		{
			Vector2 res = head_pos - toe_pos;
			res = new Vector2(res.y, -res.x);
			if (Vector2.Dot(res, value) < 0)
			{
				res = -res;
			}
			res.Normalize();
			_touch_dir = res;
		}
	}

	public OrigamiOperator TransformToLocal(Transform trans)
	{
		OrigamiOperator result = new OrigamiOperator();
		result.head_pos = trans.InverseTransformPoint(head_pos);
		result.toe_pos = trans.InverseTransformPoint(toe_pos);
		result.touch_dir = trans.InverseTransformVector(touch_dir);
		result.is_forward = is_forward;
		return result;
	}

	public OrigamiOperator TransformToWorld(Transform trans)
	{
		OrigamiOperator result = new OrigamiOperator();
		result.head_pos = trans.TransformPoint(head_pos);
		result.toe_pos = trans.TransformPoint(toe_pos);
		result.touch_dir = trans.TransformVector(touch_dir);
		result.is_forward = is_forward;
		return result;
	}

	public static OrigamiOperator empty = new OrigamiOperator();
}

/// <summary>
/// 折纸操作的每一个节点
/// </summary>
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
		fold_op = null;
	}

	public OrigamiOperationNode(Transform t)
	{
		trans = t;
		fold_angle = 0;
		polygon = default(PolygonData);

		fold_order = 0;
		fold_forward = true;
		fold_edgeid = 0;
		fold_op = null;
	}

	public Transform trans;
	public float fold_angle; // 折叠的角度

	public PolygonData polygon; // 每个结点仅有1个polygon
	public int fold_order; // 对于本层而言，其折叠层级
	public bool fold_forward; // 当时折叠时的正反状态
	public int fold_edgeid; // 折叠时新增的edge_id
	public OrigamiOperator fold_op; // 折叠时的操作（在父亲坐标系下）
}

/// <summary>
/// 计算和保存操作结果
/// </summary>
public class OrigamiOperationCalculator : MonoBehaviour {
	public GameObject m_sample;

	JBinaryTree<OrigamiOperationNode> m_operatorTree;
	int m_cur_depth = 0;

	JBinaryTree<OrigamiOperationNode> m_addingOperationRoot = null; // 当revert的时候，所使用的最高结点

	JBinaryTree<OrigamiOperationNode> m_revertingOperationNode = null; // 当部分修改时的时候，所使用的最高节点

	// Use this for initialization
	void Start()
	{
		OrigamiOperationNode root = GenerateObjAndNode("root", transform, JUtility.GetRectPolygon(1, 1));
		m_operatorTree = new JBinaryTree<OrigamiOperationNode>(true, root);
	}

	#region 添加操作
	public void AddOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		TraverseOneDepthToChangeOperator(m_operatorTree, m_cur_depth, op);

		++m_cur_depth;
	}

	public void AddOperationOnlyTop(Vector2 world_choose_pos, Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		// 获取触摸的多边形
		JBinaryTree<OrigamiOperationNode> touching_node = GetTouchingNode(world_choose_pos, is_forward);
		if (touching_node == null) // 如果没有触摸到任何多边形则返回
		{
			return;
		}

		// 记录当前正在变化的节点
		JBinaryTree<OrigamiOperationNode> change_parent = FindNeedChangeTallestParent(touching_node, world_head_pos, world_toe_pos, is_forward);
		AddOperationOnlyTop(change_parent, world_head_pos, world_toe_pos, world_fold_dir, is_forward);
	}

	public void AddOperationOnlyTop(JBinaryTree<OrigamiOperationNode> change_parent, Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		m_addingOperationRoot = change_parent;

		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		TraverseOneDepthToChangeOperator(m_addingOperationRoot, m_cur_depth - m_addingOperationRoot.Depth, op);
		TraverseOneDepthToChangeOperatorWithCull(m_operatorTree, m_addingOperationRoot, m_cur_depth, OrigamiOperator.empty);

		++m_cur_depth;
	}
	#endregion

	#region 决定添加。此时判断并消除最下边一层的无用的节点（不增加depth即可） todo
	public void ConfirmAddOperation()
	{
		m_addingOperationRoot = null;
	}
	#endregion

	#region 修改上次添加的操作
	// 修改最后一个操作
	public bool ChangeLastOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		TraverseOneDepthToChangeOperator(m_operatorTree, m_cur_depth - 1, op);
		return true;
	}

	public bool ChangeLastOperationAndCheckFoldTop(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		JBinaryTree<OrigamiOperationNode> change_parent = FindNeedChangeTallestParent(m_addingOperationRoot, world_head_pos, world_toe_pos, is_forward);
		if (m_addingOperationRoot != change_parent)
		{
			RemoveLastOperation(m_addingOperationRoot);
			AddOperationOnlyTop(change_parent, world_head_pos, world_toe_pos, world_fold_dir, is_forward);
			return true;
		}

		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		TraverseOneDepthToChangeOperator(m_addingOperationRoot, m_cur_depth - 1 - m_addingOperationRoot.Depth, op);
		return true;
	}
	#endregion

	#region 去掉最近的一个操作：将operation变为无效后，统一删去右节点
	public void RemoveLastOperation(JBinaryTree<OrigamiOperationNode> origin)
	{
		TraverseOneDepthToChangeOperator(origin, m_cur_depth - 1 - origin.Depth, OrigamiOperator.empty);
		RemoveLastRightNodes();
	}
	public void RemoveLastRightNodes()
	{
		if(m_cur_depth == 0)
		{
			return;
		}
		m_operatorTree.TraverseOneDepthWithCheck(m_cur_depth - 1, delegate (JBinaryTree<OrigamiOperationNode> node)
		{
			if (node.HasLeftChild())
			{
				node.GetLeftChild().trans.GetComponent<Polygon>().ShowPolygon(false);
			}
			if (node.HasRightChild())
			{
				node.GetRightChild().trans.GetComponent<Polygon>().ShowPolygon(false);
			}
			node.Data.trans.GetComponent<Polygon>().ShowPolygon(true);
			return true;
		});
		-- m_cur_depth;
	}
	#endregion

	#region 用最少的变化，添加一个操作（这个操作其实是revert）
	public bool AddOperationInLeastChange(Vector2 world_choose_pos, Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		// 获取触摸的多边形
		JBinaryTree<OrigamiOperationNode> touching_node = GetTouchingNode(world_choose_pos, is_forward);
		if (touching_node == null) // 如果没有触摸到任何多边形则返回
		{
			return false;
		}
		
		// 记录当前正在变化的节点
		JBinaryTree<OrigamiOperationNode> change_parent = FindNeedChangeTallestParent(touching_node, world_head_pos, world_toe_pos, is_forward);
		m_revertingOperationNode = change_parent;

		ChangeOperationInLeaseChange(world_head_pos, world_toe_pos, world_fold_dir, is_forward);

		return true;
	}
	public void ChangeOperationInLeaseChange(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, bool is_forward)
	{
		if(m_revertingOperationNode == null)
		{
			return;
		}
		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_forward = is_forward;

		op = op.TransformToLocal(m_revertingOperationNode.Data.trans);
		ResetNodeOperationAndChild(m_revertingOperationNode, op);
	}
	public void ClearLastOperationInLeaseChange()
	{
		m_revertingOperationNode = null;
	}
	#endregion

	#region 使对树的operation生效，包括遍历、遍历时的操作
	bool TraverseOneDepthToChangeOperator(JBinaryTree<OrigamiOperationNode> origin, int depth, OrigamiOperator op)
	{
		origin.TraverseOneDepthWithCheck(depth, delegate (JBinaryTree<OrigamiOperationNode> node)
				{
					return SetOperatorForNode(node, op);
				});

		return true;
	}

	bool TraverseOneDepthToChangeOperatorWithCull(JBinaryTree<OrigamiOperationNode> origin, JBinaryTree<OrigamiOperationNode> cull_node,
		int depth, OrigamiOperator op)
	{
		int real_depth = origin.Depth + depth;
		origin.TraverseAllWithCheck(delegate (JBinaryTree<OrigamiOperationNode> node)
		{
			if(node == cull_node)
			{
				return false; // 停止本分支的遍历
			}
			if(node.Depth != real_depth)
			{
				return true; // 继续遍历
			}
			return SetOperatorForNode(node, op);
		});

		return true;
	}

	OrigamiOperationNode GenerateObjAndNode(string name, Transform parent, PolygonData p)
	{
		GameObject new_obj = GameObject.Instantiate(m_sample);
		new_obj.name = name;
		new_obj.transform.parent = parent;
		new_obj.transform.SetPositionAndRotation(parent.position, parent.rotation);
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

	// 根据折叠产生的edgeid来查找所折叠的父亲
	JBinaryTree<OrigamiOperationNode> FindNodeByEdgeId(int edge_id)
	{
		JBinaryTree<OrigamiOperationNode> result = null;
		m_operatorTree.TraverseAllWithCheck(
			delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				if(node.Data.fold_edgeid == edge_id)
				{
					result = node.parent_node;
					return false;
				}
				return true;
			});
		return result;
	}
	#endregion

	#region 对单个节点进行 OrigamiOperation
	/// <summary>
	/// 为node计算并添加操作。
	/// 通常情况下不应该在遍历时直接修改树，但是这里的操作内容不会影响遍历结果，所以直接这么搞了嗯嗯嗯
	/// </summary>
	bool SetOperatorForNode(JBinaryTree<OrigamiOperationNode> node, OrigamiOperator op)
	{
		node.Data.fold_op = op.TransformToLocal(node.Data.trans);
		int fold_order = node.Depth + 1;

		Vector2 local_head_pos = node.Data.trans.InverseTransformPoint(op.head_pos);
		Vector2 local_toe_pos = node.Data.trans.InverseTransformPoint(op.toe_pos);
		Vector2 local_fold_dir = node.Data.trans.InverseTransformVector(op.touch_dir);
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
				left.trans.GetComponent<Polygon>().ShowPolygon(true);
			}
			else
			{
				left = GenerateObjAndNode("child_left", node.Data.trans, node.Data.polygon);
			}
			left.fold_order = GetLeftDepth(fold_order, node.Data.fold_order, op.is_forward);
			left.fold_forward = op.is_forward;
			left.trans.GetComponent<Polygon>().SetPolygonDepth(left.fold_order);

			node.SetLeftChild(left);

			if (node.HasRightChild())
			{
				node.GetRightChild().trans.GetComponent<Polygon>().ShowPolygon(false);
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
				right.trans.GetComponent<Polygon>().ShowPolygon(true);
			}
			else
			{
				right = GenerateObjAndNode("child_right", node.Data.trans, node.Data.polygon);
			}
			right.fold_edgeid = 0;
			right.fold_order = GetRightDepth(fold_order, node.Data.fold_order, op.is_forward);
			right.fold_forward = op.is_forward;
			right.trans.GetComponent<Polygon>().SetPolygonDepth(right.fold_order);

			node.SetRightChild(right);
			right.trans.RotateAround(op.head_pos, op.toe_pos - op.head_pos, 180);

			if (node.HasLeftChild())
			{
				node.GetLeftChild().trans.GetComponent<Polygon>().ShowPolygon(false);
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
					right.trans.GetComponent<Polygon>().ShowPolygon(true);
				}
				else
				{
					right = GenerateObjAndNode("child_right", node.Data.trans, right_p);
				}
				OrigamiOperationNode left;
				if (node.HasLeftChild())
				{
					left = node.GetLeftChild();
					left.polygon = left_p;
					left.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
					left.trans.GetComponent<Polygon>().SetPolygon(left_p);
					left.trans.GetComponent<Polygon>().ShowPolygon(true);
				}
				else
				{
					left = GenerateObjAndNode("child_left", node.Data.trans, left_p);
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

	#region 对节点及其所有孩子，重新计算op
	void ResetNodeOperationAndChild(JBinaryTree<OrigamiOperationNode> origin, OrigamiOperator op_local)
	{
		SetOperatorForNode(origin, op_local);

		if (origin.HasLeftChild() && origin.GetLeftChild().fold_op != null)
		{
			ResetNodeOperationAndChild(origin.GetLeftNode(), origin.GetLeftChild().fold_op);
		}
		if(origin.HasRightChild() && origin.GetRightChild().fold_op != null)
		{
			ResetNodeOperationAndChild(origin.GetRightNode(), origin.GetRightChild().fold_op);
		}
	}
	#endregion

	#region 获取触摸到的节点
	JBinaryTree<OrigamiOperationNode> GetTouchingNode(Vector2 world_touching_pos, bool is_forward)
	{
		JBinaryTree<OrigamiOperationNode> result = null;
		// 从上到下遍历多边形，查找触摸点所落在的结点
		m_operatorTree.TraverseOneDepthByOrder(m_cur_depth,
			delegate (OrigamiOperationNode node)
			{
				return is_forward ? -node.fold_order : node.fold_order;
			},
			delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				Vector2 local_cur_pos = node.Data.trans.InverseTransformPoint(world_touching_pos);
				if (node.Data.polygon.IsPointInside(local_cur_pos))
				{
					result = node;
					return false;
				}
				return true;
			});

		return result;
	}
	#endregion

	#region 获取父亲中的第一个右侧节点 [Obsolete]
	[System.Obsolete("obsolete by now.", true)]
	JBinaryTree<OrigamiOperationNode> GetCanRevertParent(JBinaryTree<OrigamiOperationNode> origin, bool is_forward, int origin_searched_depth, out int searched_depth)
	{
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

	[System.Obsolete("obsolete by now.", true)]
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
		return used_count <= 1;
	}

	[System.Obsolete("obsolete by now.", true)]
	void RevertNode(JBinaryTree<OrigamiOperationNode> node, int node_height)
	{
		// todo 撤销折叠某一层：做成折叠单层的扩展。所以先写单层的折叠
	}
	#endregion

	#region 查找需要修改的最高层的节点
	JBinaryTree<OrigamiOperationNode> FindNeedChangeTallestParent(JBinaryTree<OrigamiOperationNode> origin, 
		Vector2 world_head_pos, Vector2 world_toe_pos, bool is_forward)
	{
		int left_cross_edge, right_cross_edge;
		Vector2 local_head_pos = origin.Data.trans.InverseTransformPoint(world_head_pos);
		Vector2 local_toe_pos = origin.Data.trans.InverseTransformPoint(world_toe_pos);
		origin.Data.polygon.GetCrossingEdge(local_head_pos, local_toe_pos, out left_cross_edge, out right_cross_edge);

		List<int> need_check_edges = new List<int>();
		if(left_cross_edge != 0)
		{
			need_check_edges.Add(left_cross_edge);
		}
		if(right_cross_edge != 0 && right_cross_edge != left_cross_edge)
		{
			need_check_edges.Add(right_cross_edge);
		}

		List<JBinaryTree<OrigamiOperationNode>> checked_nodes = new List<JBinaryTree<OrigamiOperationNode>>(); // 已经检查过的节点

		JBinaryTree<OrigamiOperationNode> final_root = origin; // 初始化为当前点
		checked_nodes.Add(origin);

		int min_depth = m_cur_depth;
		int checking_idx = -1;
		while(++checking_idx < need_check_edges.Count)
		{
			JBinaryTree<OrigamiOperationNode> checking_node = FindNodeByEdgeId(need_check_edges[checking_idx]);
			if(min_depth > checking_node.Depth)
			{
				final_root = checking_node;
				min_depth = checking_node.Depth;
			}

			List<int> need_add_edge = SearchAllNeedChangeEdgeIdsInChild(checking_node, world_head_pos, world_toe_pos, checked_nodes);
			checked_nodes.Add(checking_node);

			foreach(int edge_id in need_add_edge)
			{
				bool can_add = true; // 是否和以前的重复了，不重复才可以添加进去
				foreach(int checked_edge_id in need_check_edges)
				{
					if(checked_edge_id == edge_id)
					{
						can_add = false;
						break;
					}
				}
				if(can_add)
				{
					need_check_edges.Add(edge_id);
				}
			}
		}

		return final_root;
	}
	#endregion
	#region 查找孩子中有交点的所有edge_id
	List<int> SearchAllNeedChangeEdgeIdsInChild(JBinaryTree<OrigamiOperationNode> origin, 
		Vector2 world_head_pos, Vector2 world_toe_pos,
		List<JBinaryTree<OrigamiOperationNode>> checked_nodes)
	{
		List<int> result = new List<int>();
		origin.TraverseAllWithCheck(delegate (JBinaryTree<OrigamiOperationNode> node)
			{
				foreach (JBinaryTree<OrigamiOperationNode> checked_node in checked_nodes)
				{
					if (checked_node == node)
					{
						return false;
					}
				}
				if (node.HasLeftChild() || node.HasRightChild())
				{
					return true; // 只检查叶子节点
				}

				Vector2 local_head_pos = node.Data.trans.InverseTransformPoint(world_head_pos);
				Vector2 local_toe_pos = node.Data.trans.InverseTransformPoint(world_toe_pos);
				int left_cross_edge, right_cross_edge;
				node.Data.polygon.GetCrossingEdge(local_head_pos, local_toe_pos, out left_cross_edge, out right_cross_edge);
				
				foreach(int edge_id in result)
				{
					if (edge_id == left_cross_edge)
					{
						left_cross_edge = 0;
					}
					if(edge_id == right_cross_edge)
					{
						right_cross_edge = 0;
					}
					if(left_cross_edge == 0 && right_cross_edge == 0)
					{
						break;
					}
				}
				if(left_cross_edge != 0)
				{
					result.Add(left_cross_edge);
				}
				if(right_cross_edge != 0)
				{
					result.Add(right_cross_edge);
				}
				return true;
			});
		return result;
	}
	#endregion
}