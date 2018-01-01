using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	public PolygonEdge(int id, int h, int t, bool inside)
	{
		ID = id;
		idx_head = h;
		idx_toe = t;
		bInside = inside;
		parent_layers = new List<PolygonLayer>();
	}
	public int ID; // 边的id
	public int idx_head; // 顶点索引
	public int idx_toe; // 顶点索引
	public bool bInside; // 是否是内部的折痕边
	public List<PolygonLayer> parent_layers;

	public float distance; // 边的长度

	public static PolygonEdge zero { get{ return new PolygonEdge(0, -1, -1, false); } }
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
	PolygonPoint head_point;
	PolygonPoint toe_point;
	PolygonEdge old_edge;
}
#endregion

#region Basic Polygon and mesh building
public class Polygon
{
	public List<PolygonPoint> m_points; // 多边形的点
	public List<PolygonEdge> m_edges; // 多边形的边

	public List<Matrix4x4> m_transHistory; // 进行过的所有位置变换
	public Matrix4x4 m_curTrans; // 当前位置

	public MeshData m_meshData;
	public void InitTrans()
	{
		m_curTrans = Matrix4x4.identity;
		m_transHistory = new List<Matrix4x4>();
	}

	public void InitAll()
	{
		InitTrans();
		CalEdgeDistance();
		CalculateMesh();
	}

	#region 计算基本的顶点和边的数据
	public void CalEdgeDistance()
	{
		for(int idx = 0; idx != m_edges.Count; ++idx)
		{
			PolygonEdge edge = m_edges[idx];
			PolygonPoint point_h = m_points[edge.idx_head];
			PolygonPoint point_t = m_points[edge.idx_toe];
			m_edges[idx].distance = point_t.DistanceTo(point_h);
		}
	}
	public void SetEdgeByIndexPair(List<int> index_pairs)
	{ } // todo 该这里了！！！！！！！！！
	public void SetEdgeByPointPair(List<PointPair> point_pairs)
	{ }
	public void CalculateMesh()
	{
		// 创建mesh数据
		m_meshData = new MeshData();

		// 顶点直接拷贝多边形的点
		m_meshData.m_vertices = new Vector3[m_points.Count];
		for(int idx = 0; idx != m_meshData.m_vertices.Length; ++idx)
		{
			m_meshData.m_vertices[idx] = m_points[idx].position;
		}

		// 首先建立沿着周长的所有点的列表
		List<int> point_line;
		CalPointLine(out point_line);
		SortPoints(ref point_line);

		// 从一个点出发，构建所有的三角形
		SetTriangleList(ref point_line, ref m_meshData.m_triangles);

		// 计算包围盒长方形，并把坐标投射到包围盒长方形中，作为uv坐标
		CalUVByProjectToBound(CalculateBoundRect(), ref m_meshData.m_uvs);
	}

	public void InitEdgeParent(PolygonLayer parent)
	{
		AddEdgeParent(parent);
	}
	public void AddEdgeParent(PolygonLayer parent)
	{
		foreach (PolygonEdge edge in m_edges)
		{
			edge.parent_layers.Add(parent);
		}
	}
	public void DelEdgeParent(PolygonLayer parent)
	{
		foreach (PolygonEdge edge in m_edges)
		{
			edge.parent_layers.Remove(parent);
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
		point_line.Add(head_idx);
		point_line.Add(0);
		point_line.Add(toe_idx);
		// 根据上边已经建立了的列表，顺次寻找张角最大的下一个点
		while (point_line.Count < m_points.Count)
		{
			int next_idx = FindOtherEnd(point_line.Count - 1, point_line.Count - 2);
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
		for(int i = 0; i != point_line.Count; ++i)
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
		for(head_idx = 0; head_idx < m_points.Count-1; ++head_idx)
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
					if (angle < min_dot_angle - Mathf.Epsilon) // 小于
					{
						min_dot_angle = angle;
						final_head_idx = head_idx;
						final_toe_idx = toe_idx;
						final_head_dist = head_dist;
						final_toe_dist = toe_dist;
					}
					else if (angle < min_dot_angle + Mathf.Epsilon) // 等于
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
			Vector2 next_dir = m_points[idx].position - cur_point;
			float next_dist = next_dir.sqrMagnitude;
			next_dir.Normalize();
			float next_dot = Vector2.Dot(last_dir, next_dir);
			if(next_dot < min_dot_angle - Mathf.Epsilon)
			{
				min_dot_angle = next_dot;
				final_idx = idx;
				min_dist = next_dist;
			}
			else if(next_dot < min_dot_angle + Mathf.Epsilon)
			{
				if(next_dist < min_dist)
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

	#region 建立三角形点列
	/// <summary>
	/// 根据点的围绕顺序，构建所有的三角形
	/// </summary>
	/// <param name="triangles"></param>
	void SetTriangleList(ref List<int> point_line, ref int[] triangles)
	{
		int triangle_count = (point_line.Count - 2) * 3;
		if(triangle_count < 0)
		{
			triangles = new int[0];
			return;
		}
		triangles = new int[triangle_count];
		for(int i = 2; i < point_line.Count; ++i)
		{
			int triangle_idx = (i-2) * 3;
			triangles[triangle_idx] = point_line[0];
			triangles[triangle_idx+1] = point_line[i-1];
			triangles[triangle_idx+2] = point_line[i];
		}
	}
	#endregion

	#region 计算uv坐标
	/// <summary>
	/// 计算包围盒长方形
	/// </summary>
	Vector4 CalculateBoundRect()
	{
		float left,right,top,bottom;
		if(m_points.Count == 0)
		{
			return new Vector4(0,0,0,0);
		}
		left = m_points[0].position.x;
		right = m_points[0].position.x;
		top = m_points[0].position.y;
		bottom = m_points[0].position.y;

		foreach(PolygonPoint p in m_points)
		{
			left = Mathf.Min(p.position.x, left);
			right = Mathf.Max(p.position.x, right);
			top = Mathf.Max(p.position.y, top);
			bottom = Mathf.Min(p.position.y, bottom);
		}
		return new Vector4(left,right,top,bottom);
	}
	
	/// <summary>
	/// 把坐标投射到包围盒长方形中，作为uv坐标
	/// </summary>
	void CalUVByProjectToBound(Vector4 bound, ref Vector2[] uvs)
	{
		uvs = new Vector2[m_points.Count];
		if(m_points.Count == 0)
		{
			return;
		}

		float width_inv = 1 / (bound.y - bound.x);
		float height_ivn = 1 / (bound.z - bound.w);

		for(int idx = 0; idx < m_points.Count; ++idx)
		{
			uvs[idx] = new Vector2(
				(m_points[idx].position.x - bound.x) * width_inv,
				(m_points[idx].position.y - bound.w) * height_ivn
			);
		}
	}
	#endregion

	#region 检查点是否在多边形内
	public bool IsPointInside(Vector2 point)
	{
		int total_count = m_points.Count;
		for(int i = 0; i != m_points.Count; ++i)
		{
			Vector2 cur_pos = m_points[i].position;
			Vector2 last_dir = m_points[(i - 1 + total_count) % total_count].position - cur_pos;
			Vector2 next_dir = m_points[(i + 1) % total_count].position - cur_pos;
			Vector2 cur_dir = point - cur_pos;
			float cross_result = Vector3.Dot(Vector3.Cross(cur_dir, last_dir), Vector3.Cross(next_dir, cur_dir));
			if(cross_result < Mathf.Epsilon)
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
			if (Mathf.Abs(edge_dot) < Mathf.Epsilon)
			{
				continue; // 射线与边平行
			}

			float edge_param = Vector2.Dot(start_point - head_pos, ray_dir_ver) / edge_dot;
			if(edge_param < 0 || edge_param > 1)
			{
				continue; // 交点不在边上
			}

			Vector2 collide_point = head_pos + edge_param * edge_dir;
			if(Vector2.Dot(ray_direct, collide_point - start_point) <= 0)
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
	public bool CheckAllOneSide(Vector2 head_pos, Vector2 toe_pos, Vector2 fold_dir)
	{
		foreach(PolygonPoint p in m_points)
		{
			Vector2 point_dir = head_pos - p.position;
			if(Vector2.Dot(point_dir, fold_dir) < -Mathf.Epsilon)
			{
				return false;
			}
		}
		return true;
	}
	#endregion

	#region 检查多边形沿着一条线切成两个的可行性
	public bool CutPolygon(Vector2 head_pos, Vector2 toe_pos, out Polygon left_p, out Polygon right_p)
	{
		left_p = new Polygon();
		right_p = new Polygon();

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
		for(int i = 0; i != m_points.Count; ++i)
		{
			PolygonPoint p = m_points[i];
			PolygonPoint last_p = m_points[(i-1+m_points.Count)%m_points.Count];
			PolygonEdge e = m_edges[i]; // 是当前点和前一个点组成的边

			float last_dir_param = Vector2.Dot(last_p.position - head_pos, ver_dir);
			float dir_param = Vector2.Dot(p.position - head_pos, ver_dir);
			if (dir_param <= -Mathf.Epsilon && last_dir_param > Mathf.Epsilon)
			{// 右 - 交 - 左
				Vector2 new_point = GetCrossPoint(last_p.position, p.position, head_pos, toe_pos);
				right2left_point = new PolygonPoint(new_point.x, new_point.y);

				left_point.Add(p);

				left_edge.Add(new PointPair(right2left_point, p, e));
				right_edge.Add(new PointPair(last_p, right2left_point, e));
			}
			else if (dir_param > Mathf.Epsilon && last_dir_param <= -Mathf.Epsilon)
			{// 左 - 交 - 右
				Vector2 new_point = GetCrossPoint(m_points[i - 1].position, p.position, head_pos, toe_pos);
				left2right_point = new PolygonPoint(new_point.x, new_point.y);

				right_point.Add(p);

				right_edge.Add(new PointPair(left2right_point, p, e));
				left_edge.Add(new PointPair(last_p, left2right_point, e));
			}
			else if (dir_param <= -Mathf.Epsilon && last_dir_param <= -Mathf.Epsilon)
			{// 左 - 左
				left_point.Add(p);
				left_edge.Add(new PointPair(last_p, p, e));
			}
			else if (dir_param > Mathf.Epsilon && last_dir_param > Mathf.Epsilon)
			{// 右 - 右
				right_point.Add(p);
				right_edge.Add(new PointPair(last_p, p, e));
			}
			else if (dir_param > -Mathf.Epsilon && dir_param <= Mathf.Epsilon)
			{// 中
				if (last_dir_param <= -Mathf.Epsilon)
				{// 左 - 中
					left_edge.Add(new PointPair(last_p, p, e));
				}
				else if (last_dir_param > Mathf.Epsilon)
				{// 右 - 中
					right_edge.Add(new PointPair(last_p, p, e));
				}
				else
				{
					return false;
				}
			}
			else if (last_dir_param > -Mathf.Epsilon && last_dir_param <= Mathf.Epsilon)
			{// ?
				if (dir_param <= -Mathf.Epsilon)
				{// 中 - 左
					left_point.Add(p);
					left_edge.Add(new PointPair(last_p, p, e));
				}
				else if (dir_param > Mathf.Epsilon)
				{// 中 - 右
					right_point.Add(p);
					right_edge.Add(new PointPair(last_p, p, e));
				}
			}
		}

		if(left_point.Count == 0 || right_point.Count == 0)
		{
			return false;
		}

		left_p.m_points = left_point;
		left_p.SetEdgeByPointPair(left_edge);
		left_p.InitAll();
		right_p.m_points = right_point;
		right_p.SetEdgeByPointPair(right_edge);
		right_p.InitAll();

		return false;
	}

	Vector2 GetCrossPoint(Vector2 l_head, Vector2 l_toe, Vector2 r_head, Vector2 r_toe)
	{
		Vector2 l_dir = l_toe - l_head;
		Vector2 l_ver_dir = new Vector2(l_dir.y, -l_dir.x);
		float param_t = Vector2.Dot(l_head - r_head, l_ver_dir) / Vector2.Dot(r_toe - r_head, l_ver_dir);
		return param_t * r_toe + (1 - param_t) * r_head;
	}
	#endregion
}
#endregion

public class FoldInfo
{
	public FoldInfo()
	{
		fold_polygons = new List<Polygon>();
		linked_layers = new List<PolygonLayer>();
	}
	public List<Polygon> fold_polygons;
	public List<PolygonLayer> linked_layers;
}

// 一层多边形，可以包含多个多边形
public class PolygonLayer : MonoBehaviour {
	public MeshData m_meshData;
	public List<Polygon> m_polygons; // 包含了的多边形
	public Mesh m_mesh; // 实际mesh

	public int m_layerDepth = 0; // 本层的高度

	// Use this for initialization
	void Start () {
		CalculateMesh();
	}

	#region set polygon and mesh stuff up
	public void SetLayerDepth(int layer_depth)
	{
		m_layerDepth = layer_depth;
		gameObject.name = "layer_" + layer_depth.ToString();
	}
	public void ResetColorByLayerDepth(int min_depth, int max_depth)
	{
		float depth_lerp = max_depth == min_depth ? 1 : (m_layerDepth - min_depth) / (float)(max_depth - min_depth);
		GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(depth_lerp, 0, 0));
	}
	public void SetPolygons(List<Polygon> polygons)
	{
		m_polygons = polygons;
		CalculateMesh();
	}
	public void DelPolygons(List<Polygon> polygons)
	{
		foreach (Polygon p in polygons)
		{
			m_polygons.Remove(p);
		}
		CalculateMesh();
	}

	void CalculateMesh()
	{
		if (m_polygons == null || m_polygons.Count == 0)
		{
			Debug.LogError("no polygons!");
			return;
		}

		m_meshData = new MeshData();
		// 拷贝所有polygon数据
		int vertices_count = 0;
		int uvs_count = 0;
		int triangles_count = 0;
		foreach (Polygon p in m_polygons)
		{
			vertices_count += p.m_meshData.m_vertices.Length;
			uvs_count += p.m_meshData.m_uvs.Length;
			triangles_count += p.m_meshData.m_triangles.Length;
		}
		m_meshData.m_vertices = new Vector3[vertices_count];
		m_meshData.m_uvs = new Vector2[uvs_count];
		m_meshData.m_triangles = new int[triangles_count];
		int vertices_idx = 0;
		int uvs_idx = 0;
		int triangles_idx = 0;
		foreach (Polygon p in m_polygons)
		{
			int vertice_offset = vertices_idx;
			p.m_meshData.m_vertices.CopyTo(m_meshData.m_vertices, vertices_idx);
			vertices_idx += p.m_meshData.m_vertices.Length;
			p.m_meshData.m_uvs.CopyTo(m_meshData.m_uvs, uvs_idx);
			uvs_idx += p.m_meshData.m_uvs.Length;

			p.m_meshData.m_triangles.CopyTo(m_meshData.m_triangles, triangles_idx);
			for (int idx = 0; idx != p.m_meshData.m_triangles.Length; ++idx)
			{
				m_meshData.m_triangles[idx + triangles_idx] += vertice_offset;
			}
			triangles_idx += p.m_meshData.m_triangles.Length;
		}

		// 创建mesh
		if (m_mesh == null)
		{
			m_mesh = new Mesh();
		}
		else
		{
			m_mesh.Clear();
		}
		m_mesh.vertices = m_meshData.m_vertices;
		m_mesh.uv = m_meshData.m_uvs;
		m_mesh.triangles = m_meshData.m_triangles;
		m_mesh.RecalculateNormals();
		m_meshData.m_normals = m_mesh.normals;

		GetComponent<MeshFilter>().mesh = m_mesh;
	}
	#endregion

	#region find touching point and fold edge
	public bool FindFoldEdge(Vector2 touch_point, ref Vector2 local_touch_dir, out Vector2 local_head_pos, out Vector2 local_toe_pos)
	{
		local_head_pos = Vector2.zero;
		local_toe_pos = Vector2.zero;
		if (local_touch_dir.magnitude < Mathf.Epsilon)
		{
			return false;
		}

		Polygon fold_polygon = null;
		PolygonEdge fold_edge = null;
		float fold_dist = 0;
		for (int i = 0; i != m_polygons.Count; ++i)
		{
			PolygonEdge edge = PolygonEdge.zero;
			float dist = 0;
			if(m_polygons[i].FindCollideEdge(local_touch_dir, touch_point, ref edge, ref dist) && edge.bInside)
			{
				if (fold_edge == null || dist < fold_dist)
				{
					fold_polygon = m_polygons[i];
					fold_edge = edge;
					fold_dist = dist;
				}
			}
		}
		if (fold_edge != null)
		{
			local_head_pos = fold_polygon.m_points[fold_edge.idx_head].position;
			local_toe_pos = fold_polygon.m_points[fold_edge.idx_toe].position;
			Vector2 edge_dir = local_toe_pos - local_head_pos;
			local_touch_dir -= Vector2.Dot(local_touch_dir, edge_dir) * edge_dir / edge_dir.sqrMagnitude;
			local_touch_dir.Normalize();
			return true;
		}
		return false;
	}

	public bool GetTouchPoint(bool need_inside, ref Vector2 local_touch_point)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Vector3 new_pos = transform.InverseTransformPoint(ray.origin);
		Vector3 new_dir = transform.InverseTransformDirection(ray.direction);
		if(Mathf.Abs(new_dir.z) < Mathf.Epsilon)
		{
			return false;
		}
		float param = new_pos.z / new_dir.z;
		Vector3 collide_point = new_pos - param * new_dir;

		if (need_inside)
		{
			bool is_inside = false;
			foreach (Polygon p in m_polygons)
			{
				if (p.IsPointInside(collide_point))
				{
					is_inside = true;
					break;
				}
			}
			if (!is_inside)
			{
				return false;
			}
		}
		local_touch_point.x = collide_point.x;
		local_touch_point.y = collide_point.y;

#if UNITY_EDITOR
		Debug.DrawRay(collide_point, new_dir, Color.blue);
#endif
		return true;
	}
	#endregion

	#region check whether can fold along the given edge towards the given dir
	/// <summary>
	/// the head and toe position of the edge line.
	/// </summary>
	public bool GetFoldInfoWorld(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, out FoldInfo fold_info)
	{
		return GetFoldInfoLocal(transform.InverseTransformPoint(world_head_pos)
			, transform.InverseTransformPoint(world_toe_pos)
			, transform.InverseTransformVector(world_fold_dir)
			, out fold_info);
	}
	public bool GetFoldInfoLocal(Vector2 local_head_pos, Vector2 local_toe_pos, Vector2 local_fold_dir, out FoldInfo fold_info)
	{
		fold_info = new FoldInfo();
		fold_info.fold_polygons = new List<Polygon>();
		foreach (Polygon p in m_polygons)
		{
			if(p.CheckAllOneSide(local_head_pos, local_toe_pos, local_fold_dir))
			{
				fold_info.fold_polygons.Add(p);
				foreach(PolygonEdge edge in p.m_edges)
				{
					foreach(PolygonLayer layer in edge.parent_layers)
					{
						fold_info.linked_layers.Add(layer);
					}
				}
			}
		}
		if(fold_info.fold_polygons.Count == 0)
		{
			return false;
		}
		
		return true;
	}

	#endregion

	#region transform(fold) along edge
	public void TransformAlongEdge(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_direct)
	{
		transform.RotateAround(world_head_pos, world_toe_pos - world_head_pos, 180);
	}
	#endregion

	#region cut the layer and all crossing polygon apart by a line
	public void CutLayer(Vector2 local_head_pos, Vector2 local_toe_pos)
	{
		List<Polygon> new_polygons = new List<Polygon>();
		foreach(Polygon p in m_polygons)
		{
			Polygon left_p, right_p;
			if(p.CutPolygon(local_head_pos, local_toe_pos, out left_p, out right_p))
			{
				new_polygons.Add(left_p);
				new_polygons.Add(right_p);
			}
			else
			{
				new_polygons.Add(p);
			}
		}
		m_polygons = new_polygons;
	}
	#endregion
}
