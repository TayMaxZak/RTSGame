using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Particles_AlphaByAngle : MonoBehaviour
{
	[SerializeField]
	float bias = 0;

	[SerializeField]
	private Transform parent;

	[SerializeField]
	private Camera cam;
	private Transform camTrans;

	private ParticleSystem pS;
	private Particle[] particles;
	private ParticleSystem.MainModule main;
	private float prevVal = 0;

	// Use this for initialization
	void Start ()
	{
		cam = Camera.main;
		camTrans = cam.transform;
		pS = GetComponent<ParticleSystem>();
		particles = new Particle[pS.main.maxParticles];
		main = pS.main;
	}
	
	// Update is called once per frame
	void Update ()
	{
		float val = Vector3.Dot(parent.forward, camTrans.forward);
		val = Mathf.Max(val, 0);
		val -= bias;

		if (val == prevVal)
			return;
		else
			prevVal = val;

		int numAlive = pS.GetParticles(particles);

		//Debug.Log(cam.WorldToScreenPoint(transform.position));
		//transform.localScale = new Vector3(val, val, val);

		for (int i = 0; i < numAlive; i++)
		{
			main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, val);
			//pS.Clear();
			//particles[i].startSize= val;
		}
	}
}
