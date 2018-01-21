/**
 * （非二叉树版逐步创建折纸的类，建议使用 OrigamiOperationCalculator 相关）
 * 仅用作创建折痕的debug工具，设置操作，直接输出结果
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Use binary-tree instead", true)]
public class OrigamiCreater : MonoBehaviour {
	public List<OrigamiOperator> m_operators = new List<OrigamiOperator>();

	public GameObject m_pointSample = null;
	public OrigamiPaper m_paper = null;

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
		res.Add(JUtility.GetRectPolygon(1, 1));

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
		PolygonLayer main_pl = null;
		foreach(PolygonLayer pl in paper.m_polygonLayers)
		{
			if(pl.AddNewEdgeInWorld(true, op.head_pos, op.toe_pos))
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
