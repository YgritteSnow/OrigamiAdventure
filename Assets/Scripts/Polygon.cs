using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{
	public Vector3[] m_vertices;
	public Vector2[] m_uvs;
	public Vector2[] m_uv2s;
	public int[] m_triangles;
	public Vector3[] m_normals; // 缓存法线
}

#region Struct for Polygon points and edges
public class PolygonPoint
{
	public PolygonPoint(float x, float y)
	{
		position = new Vector2(x, y);
		bFrozen = false;
	}
	public PolygonPoint(float x, float y, bool bf)
	{
		position = new Vector2(x, y);
		bFrozen = bf;
	}

	public Vector2 position; // 位置

	public bool bFrozen; // 是否已经固定
	public float DistanceTo(PolygonPoint other)
	{
		return (position - other.position).magnitude;
	}
	public PolygonPoint Clone()
	{
		return new PolygonPoint(position.x, position.y, bFrozen);
	}
}

public class PolygonEdge
{
	private static int s_id_accumulator = 0;
	public static int GetNextID() { return ++s_id_accumulator; }
	public static int GetEdgeID() { return 0; }

	public PolygonEdge(int id, int h, int t)
	{
		ID = id;
		idx_head = h;
		idx_toe = t;

		depth = 0;
	}

	public PolygonEdge(int id, int h, int t, int d)
	{
		ID = id;
		idx_head = h;
		idx_toe = t;
		depth = d;
	}
	public int ID; // 边的id
	public int idx_head; // 顶点索引
	public int idx_toe; // 顶点索引

	public float distance; // 边的长度
	public int depth; // 边的深度：初始化时为0，在折叠时，每次折叠，深度+1

	public static PolygonEdge invalid = new PolygonEdge(0, -1, -1);

	public bool bInside { get { return ID <= 0; } }
}

/// <summary>
/// 中间计算过程用的数据结构，存储一条边的两个顶点及其相关信息
/// </summary>
public struct PointPair
{
	public PointPair(PolygonPoint h, PolygonPoint t, PolygonEdge e)
	{
		head_point = h; toe_point = t; old_edge = e;
	}
	public PolygonPoint head_point;
	public PolygonPoint toe_point;
	public PolygonEdge old_edge;
}
#endregion

#region Basic PolygonData
public class PolygonData
{
	public List<PolygonPoint> m_points; // 多边形的点，逆时针方向的顺序
	public Vector2 center_position; // 多边形的中心
	private List<PolygonEdge> m_edges; // 多边形的边
	public List<PolygonEdge> Edges { get { return m_edges; } }
	public Vector4 m_originBounds; // 最初的边界，用以计算uv

	[System.Obsolete("Use OrigamiPaper instead", true)]
	public PolygonLayer m_parentLayer = null; // 所属的 PolygonLayer

	public void InitAll()
	{
		CalEdgeDistance();
		CalPoints();
		InitEdgeParent();
	}

	#region 计算基本的顶点和边的数据
	public void CalEdgeDistance()
	{
		for (int idx = 0; idx != m_edges.Count; ++idx)
		{
			PolygonEdge edge = m_edges[idx];
			PolygonPoint point_h = m_points[edge.idx_head];
			PolygonPoint point_t = m_points[edge.idx_toe];
			m_edges[idx].distance = point_t.DistanceTo(point_h);
		}
	}
	public void SetEdgeByIndexPair(List<PolygonEdge> index_pairs)
	{
		m_edges = index_pairs;
	}
	private int _FindPointIndex(PolygonPoint p)
	{
		for (int idx = 0; idx != m_points.Count; ++idx)
		{
			if (m_points[idx] == p)
			{
				return idx;
			}
		}
		Debug.LogError("Cannot find point!!!");
		return -1;
	}
	public void SetEdgeByPointPair(List<PointPair> point_pairs)
	{
		m_edges = new List<PolygonEdge>();
		for (int idx = 0; idx < point_pairs.Count; ++idx)
		{
			PointPair point_pair = point_pairs[idx];
			m_edges.Add(new PolygonEdge(point_pair.old_edge.ID, _FindPointIndex(point_pair.head_point), _FindPointIndex(point_pair.toe_point), point_pair.old_edge.depth));
		}
	}
	void CalPoints()
	{
		// 首先建立沿着周长的所有点的列表
		List<int> point_line;
		CalPointLine(out point_line);
		SortPoints(ref point_line);
	}

	public void InitEdgeParent()
	{
		foreach (PolygonEdge edge in m_edges)
		{
			PolygonEdgeMapping.AddEdgePolygon(edge.ID, this);
		}
	}
	#endregion

	#region 建立沿着周长的所有点的列表
	/// <summary>
	/// 建立沿着周长的所有点的列表
	/// </summary>
	/// <param name="point_line"></param>
	void CalPointLine(out List<int> point_line)
	{
		point_line = new List<int>();
		// 随便找一个点，找到它的张角最大的两条边
		int head_idx, toe_idx;
		FindMaxAngle(0, out head_idx, out toe_idx);

		// head - cur - toe 应当为逆时针顺序
		if( Vector3.Cross(m_points[0].position - m_points[head_idx].position, m_points[toe_idx].position - m_points[0].position).z < 0)
		{
			int tmp = head_idx;
			head_idx = toe_idx;
			toe_idx = tmp;
		}

		point_line.Add(head_idx);
		point_line.Add(0);
		point_line.Add(toe_idx);
		// 根据上边已经建立了的列表，顺次寻找张角最大的下一个点
		while (point_line.Count < m_points.Count)
		{
			int next_idx = FindOtherEnd(point_line[point_line.Count - 1], point_line[point_line.Count - 2]);
			point_line.Add(next_idx);
		}
	}

	/// <summary>
	/// 整理点，使之沿着周长方向依次排列
	/// </summary>
	/// <param name="point_line"></param>
	void SortPoints(ref List<int> point_line)
	{
		List<PolygonPoint> new_points = new List<PolygonPoint>();
		for (int i = 0; i != point_line.Count; ++i)
		{
			new_points.Add(m_points[point_line[i]]);
			point_line[i] = i;
		}
		m_points = new_points;
	}

	/// <summary>
	/// 给定一个点，寻找其相邻的两个点
	/// </summary>
	/// <param name="cur_idx">给定的点的index</param>
	/// <param name="head_idx">相邻两点中的其中一点的index</param>
	/// <param name="toe_idx">相邻两点中的其中一点的index</param>
	/// <returns></returns>
	bool FindMaxAngle(int cur_idx, out int head_idx, out int toe_idx)
	{
		int final_head_idx = 0;
		float final_head_dist = 0; // 当角度相同的时候，要取距离较小者
		int final_toe_idx = 0;
		float final_toe_dist = 0;
		float min_dot_angle = 2; // 最大张角，即为最小dot值，最大为1，故而初始化为2
		for (head_idx = 0; head_idx < m_points.Count - 1; ++head_idx)
		{
			for (toe_idx = head_idx + 1; toe_idx < m_points.Count; ++toe_idx)
			{
				if (head_idx != cur_idx && toe_idx != cur_idx)
				{
					Vector2 head_dir = m_points[head_idx].position - m_points[cur_idx].position;
					Vector2 toe_dir = m_points[toe_idx].position - m_points[cur_idx].position;
					float head_dist = head_dir.sqrMagnitude;
					float toe_dist = toe_dir.sqrMagnitude;
					head_dir.Normalize();
					toe_dir.Normalize();
					float angle = Vector2.Dot(head_dir, toe_dir);
					if (angle < min_dot_angle - JUtility.Epsilon) // 小于
					{
						min_dot_angle = angle;
						final_head_idx = head_idx;
						final_toe_idx = toe_idx;
						final_head_dist = head_dist;
						final_toe_dist = toe_dist;
					}
					else if (angle < min_dot_angle + JUtility.Epsilon) // 等于
					{
						if (head_dist <= final_head_dist && toe_dist <= final_toe_dist)
						{
							min_dot_angle = angle;
							final_head_idx = head_idx;
							final_toe_idx = toe_idx;
							final_head_dist = head_dist;
							final_toe_dist = toe_dist;
						}
					}
				}
			}
		}
		head_idx = final_head_idx;
		toe_idx = final_toe_idx;
		return true;
	}

	/// <summary>
	/// 根据给定的连续的两个点，查找其顺次的下一个点
	/// </summary>
	/// <param name="cur_idx">第二个点</param>
	/// <param name="last_idx">第一个点</param>
	/// <returns>第三个点</returns>
	int FindOtherEnd(int cur_idx, int last_idx)
	{
		Vector2 last_point = m_points[last_idx].position;
		Vector2 cur_point = m_points[cur_idx].position;
		Vector2 last_dir = last_point - cur_point;
		last_dir.Normalize();

		int final_idx = 0;
		float min_dot_angle = 2; // 最大张角，即为最小dot值，故而初始化为1
		float min_dist = 0; // 最小距离
		for (int idx = 0; idx < m_points.Count; ++idx)
		{
			if (idx == cur_idx || idx == last_idx)
			{
				continue;
			}
			Vector2 next_dir = m_points[idx].position - cur_point;
			float next_dist = next_dir.sqrMagnitude;
			next_dir.Normalize();
			float next_dot = Vector2.Dot(last_dir, next_dir);
			if (next_dot < min_dot_angle - JUtility.Epsilon)
			{
				min_dot_angle = next_dot;
				final_idx = idx;
				min_dist = next_dist;
			}
			else if (next_dot < min_dot_angle + JUtility.Epsilon)
			{
				if (next_dist < min_dist)
				{
					min_dot_angle = next_dot;
					final_idx = idx;
					min_dist = next_dist;
				}
			}
		}
		return final_idx;
	}
	#endregion

	#region 计算uv坐标
	/// <summary>
	/// 计算包围盒长方形
	/// </summary>
	public Vector4 CalculateBoundRect()
	{
		float left, right, top, bottom;
		if (m_points.Count == 0)
		{
			return new Vector4(0, 0, 0, 0);
		}
		left = m_points[0].position.x;
		right = m_points[0].position.x;
		top = m_points[0].position.y;
		bottom = m_points[0].position.y;

		foreach (PolygonPoint p in m_points)
		{
			left = Mathf.Min(p.position.x, left);
			right = Mathf.Max(p.position.x, right);
			top = Mathf.Max(p.position.y, top);
			bottom = Mathf.Min(p.position.y, bottom);
		}
		return new Vector4(left, right, top, bottom);
	}

	public void SetOriginalBounds(Vector4 bounds)
	{
		m_originBounds = bounds;
	}
	#endregion

	#region 检查点是否在多边形内
	public bool IsPointInside(Vector2 point)
	{
		int total_count = m_points.Count;
		for (int i = 0; i != m_points.Count; ++i)
		{
			Vector2 cur_pos = m_points[i].position;
			Vector2 last_dir = m_points[(i - 1 + total_count) % total_count].position - cur_pos;
			Vector2 next_dir = m_points[(i + 1) % total_count].position - cur_pos;
			Vector2 cur_dir = point - cur_pos;
			float cross_result = Vector3.Dot(Vector3.Cross(cur_dir, last_dir), Vector3.Cross(next_dir, cur_dir));
			if (cross_result < JUtility.Epsilon)
			{
				return false;
			}
		}
		return true;
	}
	#endregion

	#region 找到某个射线和最近的边的交点
	public bool FindCollideEdge(Vector2 ray_direct, Vector2 start_point, ref PolygonEdge fold_edge, ref float ray_dist)
	{
		bool has_found = false;
		float min_ray_dist = 0;
		for (int i = 0; i != m_edges.Count; ++i)
		{
			PolygonEdge edge = m_edges[i];
			Vector2 head_pos = m_points[edge.idx_head].position;
			Vector2 toe_pos = m_points[edge.idx_toe].position;
			Vector2 edge_dir = toe_pos - head_pos;
			Vector2 ray_dir_ver = new Vector2(ray_direct.y, -ray_direct.x);
			float edge_dot = Vector2.Dot(ray_dir_ver, edge_dir);
			if (Mathf.Abs(edge_dot) < JUtility.Epsilon)
			{
				continue; // 射线与边平行
			}

			float edge_param = Vector2.Dot(start_point - head_pos, ray_dir_ver) / edge_dot;
			if (edge_param < 0 || edge_param > 1)
			{
				continue; // 交点不在边上
			}

			Vector2 collide_point = head_pos + edge_param * edge_dir;
			if (Vector2.Dot(ray_direct, collide_point - start_point) <= 0)
			{
				continue; // 交点在射线的反方向
			}

			float new_ray_dist = (collide_point - start_point).magnitude;
			if (!has_found || new_ray_dist < min_ray_dist)
			{
				fold_edge = edge;
				ray_dist = new_ray_dist;
				min_ray_dist = new_ray_dist;
				has_found = true;
			}
		}
		return has_found;
	}
	#endregion

	#region 折叠要求多边形的所有顶点都在需要折叠的一侧
	/// <summary>
	/// 
	/// </summary>
	/// <returns> 1(全在不需要被翻折的一侧); -1(全在需要翻折的一侧); 0(其他) </returns>
	public int CheckAllOneSide(Vector2 local_head_pos, Vector2 local_toe_pos, Vector2 local_fold_dir)
	{
		int cur_side = 0;
		foreach (PolygonPoint p in m_points)
		{
			Vector2 point_dir = p.position - local_head_pos;
			float cur_param = Vector2.Dot(point_dir, local_fold_dir);
			if (cur_param > -JUtility.Epsilon && cur_param < JUtility.Epsilon) // 当前点在直线上
			{
				continue;
			}
			else // 当前点不在直线上，那么一定指示了一个方向
			{
				int sign = System.Math.Sign(cur_param);
				if (cur_side == 0)
				{
					cur_side = sign;
				}
				else if (cur_side != sign)
				{
					return 0;
				}
			}
		}
		return cur_side;
	}
	#endregion

	#region 检查多边形沿着一条线切成两个的可行性
	[System.Obsolete("Be sure to set the right depth for new edge in order to support animation in PolygonJitter", true)]
	public bool CutPolygon(Vector2 head_pos, Vector2 toe_pos, out PolygonData left_p, out PolygonData right_p, out int cut_edge_id)
	{
		return CutPolygon(0, head_pos, toe_pos, Vector2.up, out left_p, out right_p, out cut_edge_id);
	}

	/// <summary>
	/// 分割多边形
	/// </summary>
	/// <param name="edge_depth"></param>
	/// <param name="head_pos"></param>
	/// <param name="toe_pos"></param>
	/// <param name="right_p"> 不需要翻转的一边、沿着折叠方向的那边 </param>
	/// <param name="left_p"> 需要翻转的一边、靠近折叠方向起始位置的那边 </param>
	/// <param name="cut_edge_id"></param>
	/// <returns></returns>
	public bool CutPolygon(int edge_depth, Vector2 head_pos, Vector2 toe_pos, Vector2 fold_dir, out PolygonData left_p, out PolygonData right_p, out int cut_edge_id)
	{
		if (Vector2.Dot(head_pos - toe_pos, new Vector2(fold_dir.y, -fold_dir.x)) < 0)
		{
			Vector2 tmp = head_pos;
			head_pos = toe_pos;
			toe_pos = tmp;
		}

		right_p = new PolygonData();
		left_p = new PolygonData();
		cut_edge_id = -1;

		Vector2 head_toe = toe_pos - head_pos;
		Vector2 ver_dir = new Vector2(head_toe.y, -head_toe.x);
		ver_dir.Normalize();
		List<PolygonPoint> left_point = new List<PolygonPoint>();
		List<PolygonPoint> right_point = new List<PolygonPoint>();
		List<PolygonPoint> equal_point = new List<PolygonPoint>();
		List<PointPair> left_edge = new List<PointPair>();
		List<PointPair> right_edge = new List<PointPair>();
		PolygonPoint left2right_point = null;
		PolygonPoint right2left_point = null;

		// 依次计算每条边和切割线的交点，插入多边形点和边的列表
		for (int i = 0; i != m_points.Count; ++i)
		{
			PolygonPoint p = m_points[i];
			PolygonPoint last_p = m_points[(i - 1 + m_points.Count) % m_points.Count];
			PolygonEdge e = m_edges[i]; // 是当前点和前一个点组成的边

			float last_dir_param = Vector2.Dot(last_p.position - head_pos, ver_dir);
			float dir_param = Vector2.Dot(p.position - head_pos, ver_dir);
			if (dir_param <= -JUtility.Epsilon && last_dir_param > JUtility.Epsilon)
			{// 右 - 交 - 左
				Vector2 new_point = GetCrossPoint(last_p.position, p.position, head_pos, toe_pos);
				right2left_point = new PolygonPoint(new_point.x, new_point.y);

				left_point.Add(p);
				//right_point.Add(right2left_point);

				left_edge.Add(new PointPair(right2left_point, p, e));
				right_edge.Add(new PointPair(last_p, right2left_point, e));
			}
			else if (dir_param > JUtility.Epsilon && last_dir_param <= -JUtility.Epsilon)
			{// 左 - 交 - 右
				Vector2 new_point = GetCrossPoint(last_p.position, p.position, head_pos, toe_pos);
				left2right_point = new PolygonPoint(new_point.x, new_point.y);

				//left_point.Add(left2right_point);
				right_point.Add(p);

				right_edge.Add(new PointPair(left2right_point, p, e));
				left_edge.Add(new PointPair(last_p, left2right_point, e));
			}
			else if (dir_param <= -JUtility.Epsilon && last_dir_param <= -JUtility.Epsilon)
			{// 左 - 左
				left_point.Add(p);
				left_edge.Add(new PointPair(last_p, p, e));
			}
			else if (dir_param > JUtility.Epsilon && last_dir_param > JUtility.Epsilon)
			{// 右 - 右
				right_point.Add(p);
				right_edge.Add(new PointPair(last_p, p, e));
			}
			else if (dir_param > -JUtility.Epsilon && dir_param <= JUtility.Epsilon)
			{// 中
				if (last_dir_param <= -JUtility.Epsilon)
				{// 左 - 中
					left_edge.Add(new PointPair(last_p, p, e));

					left2right_point = p;
				}
				else if (last_dir_param > JUtility.Epsilon)
				{// 右 - 中
					right_edge.Add(new PointPair(last_p, p, e));

					right2left_point = p;
				}
				else
				{
					return false;
				}
			}
			else if (last_dir_param > -JUtility.Epsilon && last_dir_param <= JUtility.Epsilon)
			{// ?
				if (dir_param <= -JUtility.Epsilon)
				{// 中 - 左
					left_point.Add(p);
					left_edge.Add(new PointPair(last_p, p, e));
				}
				else if (dir_param > JUtility.Epsilon)
				{// 中 - 右
					right_point.Add(p);
					right_edge.Add(new PointPair(last_p, p, e));
				}
			}
		}

		if (left2right_point != null && right2left_point != null)
		{
			left_point.Add(right2left_point);
			right_point.Add(right2left_point);
			left_point.Add(left2right_point);
			right_point.Add(left2right_point);
			int new_id = PolygonEdge.GetNextID();
			cut_edge_id = new_id;
			left_edge.Add(new PointPair(left2right_point, right2left_point, new PolygonEdge(new_id, 0, 0, edge_depth)));
			right_edge.Add(new PointPair(right2left_point, left2right_point, new PolygonEdge(new_id, 0, 0, edge_depth)));
		}
		else
		{
			return false;
		}

		right_p.m_points = left_point;
		right_p.SetEdgeByPointPair(left_edge);
		right_p.InitAll();
		right_p.SetOriginalBounds(m_originBounds);

		left_p.m_points = right_point;
		left_p.SetEdgeByPointPair(right_edge);
		left_p.InitAll();
		left_p.SetOriginalBounds(m_originBounds);

		return true;
	}

	Vector2 GetCrossPoint(Vector2 l_head, Vector2 l_toe, Vector2 r_head, Vector2 r_toe)
	{
		Vector2 l_dir = l_toe - l_head;
		Vector2 l_ver_dir = new Vector2(l_dir.y, -l_dir.x);
		float param_t = Vector2.Dot(l_head - r_head, l_ver_dir) / Vector2.Dot(r_toe - r_head, l_ver_dir);
		return param_t * r_toe + (1 - param_t) * r_head;
	}
	#endregion

	#region 获取和直线相交的的两个边的id
	public void GetCrossingEdge(Vector2 local_head_pos, Vector2 local_toe_pos, out int left_cross_edge, out int right_cross_edge)
	{
		left_cross_edge = 0;
		right_cross_edge = 0;
		Vector2 line_dir = local_toe_pos - local_head_pos;
		Vector2 line_normal = new Vector2(line_dir.y, -line_dir.x);
		foreach (PolygonEdge e in m_edges)
		{
			Vector2 left_pos = m_points[e.idx_head].position;
			Vector2 right_pos = m_points[e.idx_toe].position;
			int left_sign = System.Math.Sign(Vector2.Dot(left_pos - local_head_pos, line_normal));
			int right_sign = System.Math.Sign(Vector2.Dot(left_pos - local_head_pos, line_normal));
			if(left_sign != 0 && right_sign != 0 && left_sign != right_sign)
			{
				if(left_sign > 0)
				{
					left_cross_edge = e.ID;
				}
				else
				{
					right_cross_edge = e.ID;
				}
			}
			
		}
	}
	#endregion
}
#endregion

#region Polygon behaviour
public class Polygon : MonoBehaviour
{
	private PolygonData m_polygon;
	public PolygonRenderer m_polygonRenderer = null;
	public PolygonOuterRenderer m_polygonOuterRenderer = null;
	public PolygonJitter m_polygonJitter = null;

	void Start()
	{
		if (!m_polygonRenderer)
		{
			m_polygonRenderer = GetComponent<PolygonRenderer>();
			m_polygonOuterRenderer = GetComponent<PolygonOuterRenderer>();
		}
	}

	public void SetPolygon(PolygonData p)
	{
		m_polygon = p;
		m_polygonRenderer.SetPolygon(p);
		m_polygonOuterRenderer.SetPolygon(p);
	}

	public void SetPolygonDepth(int depth)
	{
		m_polygonJitter.SetPolygonDepth(depth);
	}

	public void ShowPolygon(bool bVisible)
	{
		m_polygonJitter.ShowPolygon(bVisible);
		m_polygonRenderer.ShowPolygon(bVisible);
		m_polygonOuterRenderer.ShowPolygon(bVisible);
	}
}
#endregion