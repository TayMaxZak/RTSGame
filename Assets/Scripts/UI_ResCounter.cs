using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ResCounter : MonoBehaviour
{
	[SerializeField]
	private Text resText;
	[SerializeField]
	private UI_TooltipSource resTooltip;
	[SerializeField]
	private Text recText;
	[SerializeField]
	private UI_TooltipSource recTooltip;

	[SerializeField]
	private Image timeFill;
	[SerializeField]
	private Vector2 minMaxFill = new Vector2(0, 1);
	private float timeCur = 0;
	[SerializeField]
	private UI_TooltipSource timeTooltip;
	[SerializeField]
	private AudioSource timeAudio;
	[SerializeField]
	private float timeAudioTime;
	private Coroutine reclaimAudioCoroutine;

	//[SerializeField]
	//private Image borderFill;

	private GameRules gameRules;
	//private UIRules uiRules;

	void Awake()
	{
		//uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	}

	public void UpdateResCounter(int res, int rec)
	{
		resText.text = res.ToString();
		recText.text = rec.ToString();
		
		// Tooltips
		resTooltip.SetText(string.Format("Resource points: {0}\nUsed primarily for calling in additional units.", res));
		recTooltip.SetText(string.Format("Raw materials: {0}\nRecovered from your destroyed units.", rec));
	}

	public void UpdateTime(float time)
	{
		timeCur = time;
		timeFill.fillAmount = minMaxFill.x + timeCur * (minMaxFill.y - minMaxFill.x);
		timeTooltip.SetText(string.Format("Next resource point in: {0:0.0}s\nEvery {1:0} seconds a raw material point is converted into a resource point.", (1 - time) * gameRules.RES_reclaimTime, gameRules.RES_reclaimTime));
	}

	public void PlayReclaimAudio(float timer)
	{
		if (timer <= timeAudioTime)
		{
			if (reclaimAudioCoroutine == null)
				reclaimAudioCoroutine = StartCoroutine(ReclaimAudioCoroutine());
		}
	}

	IEnumerator ReclaimAudioCoroutine()
	{
		timeAudio.Play();
		yield return new WaitForSeconds(gameRules.RES_reclaimTime - timeAudioTime);
		reclaimAudioCoroutine = null;
	}
}
