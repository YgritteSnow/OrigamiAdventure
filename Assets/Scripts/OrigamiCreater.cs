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
			if(Vector2.Dot(res, touch_dir) < 0)
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
		edges.Add(new PolygonEdge(0, 1, true));
		edges.Add(new PolygonEdge(1, 2, true));
		edges.Add(new PolygonEdge(2, 3, true));
		edges.Add(new PolygonEdge(3, 0, true));
		p.SetEdgeByIndexPair(edges);
		m_curEdgeCount = p.Edges.Count;

		p.InitAll();
		res.Add(p);

		m_paper.InitOriginalPolygons(res);
	}

	public void CalOrigamiPaper()
	{
		for(int i = 0; i != m_operators.Count; ++i)
		{
			OrigamiOperator op = m_operators[i];
			if (op.is_valid)
			{
				op.is_valid = FoldPaperByLine(m_paper, op);
			}
		}
	}

	private bool FoldPaperByLine(OrigamiPaper paper, OrigamiOperator op)
	{
		int cur_edge_id = m_curEdgeCount;
		PolygonLayer main_pl = null;
		foreach(PolygonLayer pl in paper.m_polygonLayers)
		{
			if(pl.AddNewEdgeInWorld(cur_edge_id, true, op.head_pos, op.toe_pos))
			{
				main_pl = pl;
			}
		}
		if(main_pl == null)
		{
			return false;
		}

		if(op.need_fold)
		{
			paper.FoldByEdgeInWorldByEdge(main_pl, op.normalised_touch_dir, op.head_pos, op.toe_pos);
		}
		return true;
	}
}
