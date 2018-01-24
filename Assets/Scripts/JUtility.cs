using System;
using System.Collections.Generic;

class JUtility
{
	public const float Epsilon = 0.00001f;
	public const int PaperRenderQueue = 2000; // 纸张的基础的折叠顺序

	#region 获取一个无其他折痕的长方形
	public static PolygonData GetRectPolygon(float width, float height)
	{
		return GetRectPolygon(0, 0, width, height);
	}
	public static PolygonData GetRectPolygon(float posx, float posy, float width, float height)
	{
		PolygonData p = new PolygonData();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(width, height));
		p.m_points.Add(new PolygonPoint(width, -height));
		p.m_points.Add(new PolygonPoint(-width, -height));
		p.m_points.Add(new PolygonPoint(-width, height));

		List<PolygonEdge> edges = new List<PolygonEdge>();
		edges.Add(new PolygonEdge(PolygonEdge.GetEdgeID(), 0, 1));
		edges.Add(new PolygonEdge(PolygonEdge.GetEdgeID(), 1, 2));
		edges.Add(new PolygonEdge(PolygonEdge.GetEdgeID(), 2, 3));
		edges.Add(new PolygonEdge(PolygonEdge.GetEdgeID(), 3, 0));
		p.SetEdgeByIndexPair(edges);
		p.InitAll();
		p.SetOriginalBounds(p.CalculateBoundRect());

		return p;
	}
	#endregion
}

#region 二叉树结构
/// <summary>
/// 二叉树结构，仅实现了需要的部分
/// </summary>
/// <typeparam name="TData"></typeparam>
public class JBinaryTree<TData>
{
	public TData Data { get; set; }

	// 是否是左结点：只在初始化的时候设置
	private bool m_isLeft = false;
	public bool IsLeft { get { return m_isLeft; } }
	public bool IsRight { get { return !m_isLeft; } }

	public JBinaryTree<TData>[] child_node;
	public JBinaryTree<TData> parent_node;

	private JBinaryTree(bool isLeft)
	{
		Data = default(TData);
		m_isLeft = isLeft;
		parent_node = null;
		child_node = new JBinaryTree<TData>[2];
	}

	public JBinaryTree(bool isLeft, TData d)
	{
		Data = d;
		m_isLeft = isLeft;
		parent_node = null;
		child_node = new JBinaryTree<TData>[2];
	}

	#region 遍历
	public delegate bool CheckAndTraverseFunc(JBinaryTree<TData> data);
	/// <summary>
	/// 只对某一深度进行遍历
	/// </summary>
	/// <param name="depth">深度值，root为0</param>
	/// <param name="func">遍历用的委托</param>
	/// <returns>没有运行到函数，或者运行时从未出错</returns>
	public bool TraverseOneDepthWithCheck(int depth, CheckAndTraverseFunc func)
	{
		if (depth == 0)
		{
			return func(this); // 返回运行结果
		}
		else if (depth > 0)
		{
			foreach (JBinaryTree<TData> node in child_node)
			{
				if (node != null && !node.TraverseOneDepthWithCheck(depth - 1, func))
				{
					return false; // 一旦错误，不再继续遍历
				}
			}
			return true; // child_node.Length > 0; // 如果没有孩子，说明本分支运行失败，但是其他的还是要继续遍历
		}
		else
		{
			return true; // 除非一开始depth就小于0，否则不可能出现这种情况
		}
	}
	#endregion

	#region 整理某一层的所有点，并排序，然后按照排序后的次序遍历
	public delegate int GetOrder(TData data);
	public bool TraverseOneDepthByOrder(int depth, GetOrder order_func, CheckAndTraverseFunc check_func)
	{
		List<JBinaryTree<TData>> result = new List<JBinaryTree<TData>>();
		TraverseOneDepthWithCheck(depth, delegate (JBinaryTree<TData> data)
			{
				result.Add(data);
				return true;
			});
		
		result.Sort( delegate (JBinaryTree<TData> lh, JBinaryTree<TData> rh)
			{
				return order_func(lh.Data) - order_func(rh.Data);
			});

		foreach(JBinaryTree<TData> data in result)
		{
			if(!check_func(data))
			{
				return false;
			}
		}

		return true;
	}
	#endregion

	#region 一些接口
	public bool HasLeftChild()
	{
		return child_node[0] != null;
	}
	public TData GetLeftChild()
	{
		return child_node[0].Data;
	}
	public bool HasRightChild()
	{
		return child_node[1] != null;
	}
	public TData GetRightChild()
	{
		return child_node[1].Data;
	}

	public void SetLeftChildNull()
	{
		child_node[0] = null;
	}
	public void SetRightChildNull()
	{
		child_node[1] = null;
	}

	public void SetLeftChild(TData data)
	{
		if(child_node[0] == null)
		{
			child_node[0] = new JBinaryTree<TData>(true);
			child_node[0].parent_node = this;
		}
		child_node[0].Data = data;
	}
	public void SetRightChild(TData data)
	{
		if (child_node[1] == null)
		{
			child_node[1] = new JBinaryTree<TData>(false);
			child_node[1].parent_node = this;
		}
		child_node[1].Data = data;
	}
	#endregion
}
#endregion