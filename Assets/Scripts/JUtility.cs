class JUtility
{
	public const float Epsilon = 0.00001f;
}

/**
 * 树结构
 */
public abstract class ITreeNode<TData>
{
	public TData data;
	public ITreeNode<TData>[] child_node;

	/// <summary>
	/// 只对某一深度进行遍历
	/// </summary>
	public virtual void TraverseOneDepth(int depth)
	{ }
}

/// <summary>
/// 这个类用不到。。先设为 abstract
/// </summary>
/// <typeparam name="TData"></typeparam>
public abstract class JTree<TData> : ITreeNode<TData>
{
	public JTree()
	{
		data = default(TData);
	}
	public JTree(TData d)
	{
		data = d;
	}
}

public class JBinaryTree<TData> : JTree<TData>
{
	public JBinaryTree() : base()
	{
		child_node = new JBinaryTree<TData>[2];
	}
	public JBinaryTree(TData d) : base(d)
	{
		child_node = new JBinaryTree<TData>[2];
	}
	
	void SetLeftChild(TData data)
	{
		child_node[0] = new JBinaryTree<TData>(data);
	}
	void SetRightChild(TData data)
	{
		child_node[1] = new JBinaryTree<TData>(data);
	}
}