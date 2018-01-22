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
	public class OrigamiOperationNode
	{
		private OrigamiOperationNode()
		{
			trans = null;
			edge_id = 0;
			fold_angle = 0;
			polygon = default(Polygon);

			fold_order = 0;
		}

		public OrigamiOperationNode(Transform t)
		{
			trans = t;
			edge_id = 0;
			fold_angle = 0;
			polygon = default(Polygon);

			fold_order = 0;
		}

		public Transform trans;
		public int edge_id; // 沿着其折叠的边的id
		public float fold_angle; // 折叠的角度

		public Polygon polygon; // 每个结点仅有1个polygon
		public int fold_order; // 对于本层而言，其折叠层级
	}

	public GameObject m_sample;

	List<OrigamiOperator> m_operators = new List<OrigamiOperator>();
	JBinaryTree<OrigamiOperationNode> m_operatorTree;

	// Use this for initialization
	void Start ()
	{
		OrigamiOperationNode root = GenerateObjAndNode("root", transform, JUtility.GetRectPolygon(1,1));
		m_operatorTree = new JBinaryTree<OrigamiOperationNode>(true, root);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	#region 对外的添加、修改、删除函数
	public bool AddOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir)
	{
		OrigamiOperator op = new OrigamiOperator();
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_valid = true;
		op.need_fold = true;
		m_operators.Add(op);

		TraverseSetLastOperator(op);

		return true;
	}

	public bool ChangeLastOperation(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir)
	{
		if(m_operators.Count == 0)
		{
			Debug.LogError("no operator!");
		}

		OrigamiOperator op = m_operators[m_operators.Count - 1];
		op.head_pos = world_head_pos;
		op.toe_pos = world_toe_pos;
		op.touch_dir = world_fold_dir;
		op.is_valid = true;
		op.need_fold = true;

		TraverseSetLastOperator(op);

		return true;
	}
	#endregion

	#region 增删改所用的一些函数
	bool TraverseSetLastOperator(OrigamiOperator op)
	{
		m_operatorTree.TraverseOneDepthWithCheck(m_operators.Count - 1
			, delegate (JBinaryTree<OrigamiOperationNode> node)
				{
					return SetOperatorForNode(m_operators.Count, node, op);
				});

		return true;
	}

	OrigamiOperationNode GenerateObjAndNode(string name, Transform parent, Polygon p)
	{
		GameObject new_obj = GameObject.Instantiate(m_sample);
		new_obj.name = name;
		new_obj.transform.parent = parent;
		new_obj.GetComponent<PolygonJitter>().SetPolygon(p);
		OrigamiOperationNode res = new OrigamiOperationNode(new_obj.transform);
		res.polygon = p;
		res.fold_angle = 0;
		return res;
	}

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
		Debug.Log("SetOper:" + node.Data.trans.gameObject.name + "," + side);
		if(side > 0) // 不需要翻折的一侧
		{
			OrigamiOperationNode left;
			if (node.HasLeftChild())
			{
				left = node.GetLeftChild();
				left.polygon = node.Data.polygon;
				left.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
				left.trans.GetComponent<PolygonJitter>().SetPolygon(left.polygon);
			}
			else
			{
				left = GenerateObjAndNode("child_all_left", node.Data.trans, node.Data.polygon);
			}
			left.edge_id = 0;
			left.fold_order = node.Data.fold_order; // 左侧节点同父亲的折叠顺序
			left.trans.GetComponent<PolygonJitter>().SetPolygonDepth(left.fold_order);

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
				right.trans.GetComponent<PolygonJitter>().SetPolygon(right.polygon);
			}
			else
			{
				right = GenerateObjAndNode("child_all_right", node.Data.trans, node.Data.polygon);
			}
			right.edge_id = 0;
			right.fold_order = fold_order * 2 - 1 - node.Data.fold_order; // 左侧节点同父亲的折叠顺序
			right.trans.GetComponent<PolygonJitter>().SetPolygonDepth(right.fold_order);

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
			Polygon left_p, right_p;
			int cut_edge_id;
			if (node.Data.polygon.CutPolygon(fold_order, local_head_pos, local_toe_pos, local_fold_dir, out left_p, out right_p, out cut_edge_id))
			{
				OrigamiOperationNode right;
				if (node.HasRightChild())
				{
					right = node.GetRightChild();
					right.polygon = right_p;
					right.trans.SetPositionAndRotation(node.Data.trans.position, node.Data.trans.rotation);
					right.trans.GetComponent<PolygonJitter>().SetPolygon(right_p);
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
					left.trans.GetComponent<PolygonJitter>().SetPolygon(left_p);
				}
				else
				{
					left = GenerateObjAndNode("child_cut_left", node.Data.trans, left_p);
				}

				left.edge_id = cut_edge_id;
				left.fold_order = node.Data.fold_order; // 左侧节点同父亲的折叠顺序
				left.trans.GetComponent<PolygonJitter>().SetPolygonDepth(left.fold_order);
				node.SetLeftChild(left);

				right.edge_id = cut_edge_id;
				right.fold_order = fold_order * 2 - 1 - node.Data.fold_order; // 右侧节点为父亲的折叠顺序反之
				right.trans.GetComponent<PolygonJitter>().SetPolygonDepth(right.fold_order);
				node.SetRightChild(right);
				
				right.trans.RotateAround(op.head_pos, op.toe_pos - op.head_pos, 180);
			}
			else
			{
				Debug.LogError("Cannot cut polygon!!!");
			}
		}

		node.Data.trans.GetComponent<PolygonJitter>().ShowPolygon(false);
		return true;
	}
	#endregion


}
