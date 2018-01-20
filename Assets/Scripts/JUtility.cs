using System;
using System.Collections.Generic;

class JUtility
{
	public const float Epsilon = 0.00001f;
}

/// <summary>
/// 二叉树结构
/// </summary>
/// <typeparam name="TData"></typeparam>
public class JBinaryTree<TData>
{
	public TData Data { get; set; }
	public JBinaryTree<TData>[] child_node;

	private JBinaryTree()
	{
		Data = default(TData);
		child_node = new JBinaryTree<TData>[2];
	}

	public JBinaryTree(TData d)
	{
		Data = d;
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

	public TData? GetLeftChild()
	{
		return child_node[0];
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
			child_node[0] = new JBinaryTree<TData>();
		}
		child_node[0].Data = data;
	}
	public void SetRightChild(TData data)
	{
		if (child_node[1] == null)
		{
			child_node[1] = new JBinaryTree<TData>();
		}
		child_node[1].Data = data;
	}
}