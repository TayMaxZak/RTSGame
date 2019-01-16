using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Particle = UnityEngine.ParticleSystem.Particle;
//using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;

public class Manager_VFX : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem[] pS;

	private float directionMult = 0.01f;

	private int newProjectilesThisFrame;

	public void SpawnEffect(VFXType vfxType, Vector3 position)
	{
		SpawnEffect(vfxType, position, Vector3.up, -1);
	}

	public void SpawnEffect(VFXType vfxType, Vector3 position, Vector3 direction, int team)
	{
		EmitParams param = new EmitParams()
		{
			position = position,
			velocity = direction * directionMult,
			startLifetime = 0 // Particle will immediately die, spawning its sub emitters
		};
		pS[IndexFromVFXType(vfxType)].Emit(param, 1);
	}

	int IndexFromVFXType(VFXType vfxType)
	{
		switch (vfxType)
		{
			case VFXType.Hit_Normal:
				return 0;
			case VFXType.Hit_Absorbed:
				return 1;
			case VFXType.Hit_Near:
				return 2;
			case VFXType.Fighter_Die_Explode:
				return 3;
			default:
				return 0;
		}
	}
}

public enum VFXType
{
	Default, // Test VFX
	Hit_Normal, // Created when a projectile hits a Unit
	Hit_Absorbed, // Created when a projectile hits a Unit but does no damage to HP
	Hit_Near, // Created when a scan damages a target that does not have real collision
	Fighter_Die_Explode, // Created when a fighter dies
}
