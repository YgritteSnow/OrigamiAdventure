using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrigamiOperator
{
	public OrigamiOperator()
	{
		head_pos = Vector3.zero;
		toe_pos = Vector3.zero;
		touch_dir = Vector3.zero;
		is_valid = false;
	}
	public Vector2 head_pos;
	public Vector2 toe_pos;
	public Vector2 touch_dir;

	public bool is_valid;
}

public class OrigamiCreater : MonoBehaviour {
	public List<OrigamiOperator> m_operators = new List<OrigamiOperator>();

	public GameObject m_pointSample = null;
	public OrigamiPaper m_paper = null;

	private int m_curEdgeCount = 0;

	// Use this for initialization
	void Awake () {
		//m_paper = GetComponent<OrigamiPaper>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void ResetOperatorCount(int count)
	{
		if(count == m_operators.Count)
		{
			return;
		}
		while(m_operators.Count < count)
		{
			m_operators.Add(new OrigamiOperator());
		}
		while(m_operators.Count > count)
		{
			m_operators.RemoveAt(m_operators.Count - 1);
		}

		ResetOrigamiPaper();
	}

	public void ResetOrigamiPaper()
	{
		ClearOrigamiPaper();
		CalOrigamiPaper();
	}

	public void ClearOrigamiPaper()
	{
		m_paper.ClearAllPolygons();
		InitNullPolygon();
	}

	void InitNullPolygon()
	{
		List<Polygon> res = new List<Polygon>();

		Polygon p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(1, 1));
		p.m_points.Add(new PolygonPoint(1, -1));
		p.m_points.Add(new PolygonPoint(-1, -1));
		p.m_points.Add(new PolygonPoint(-1, 1));

		List<PolygonEdge> edges = new List<PolygonEdge>();
		edges.Add(new PolygonEdge(1, 0, 1, true));
		edges.Add(new PolygonEdge(2, 1, 2, true));
		edges.Add(new PolygonEdge(3, 2, 3, true));
		edges.Add(new PolygonEdge(4, 3, 0, true));
		p.SetEdgeByIndexPair(edges);
		m_curEdgeCount = p.Edges.Count;

		p.InitAll();
		res.Add(p);

		m_paper.InitOriginalPolygons(res);
	}

	public void CalOrigamiPaper()
	{
		foreach(OrigamiOperator op in m_operators)
		{
			if (op.is_valid)
			{
				FoldPaperByLine(m_paper, op);
			}
		}
	}

	private void FoldPaperByLine(OrigamiPaper paper, OrigamiOperator op)
	{
		int cur_edge_id = m_curEdgeCount;
		foreach(PolygonLayer pl in paper.m_polygonLayers)
		{
			pl.AddNewEdge(cur_edge_id, true, op.head_pos, op.toe_pos);
		}
		paper.FoldByEdgeInLocalByEdge(paper.m_polygonLayers[0], op.touch_dir, op.head_pos, op.toe_pos);
	}
}
