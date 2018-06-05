using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuildUnitType
{
	Destroyer,
	Corvette,
	Bomber,
	Frigate,
	Apollo,
	Bulkhead
}

public class BuildUnit : MonoBehaviour
{
	//[SerializeField]
	public BuildUnitType type;
	public GameObject previewObject;
	public GameObject spawnObject;
	public int cost;
	public int unitCap = 1;
	public float buildTime;

	//public BuildUnitType GetBuildUnitType()
	//{
	//	return type;
	//}
}
/*
public static class BuildUnitUtils
{
	public static void InitAbility(Ability ability)
	{
		switch (ability.type)
		{
			case AbilityType.ArmorDrain:
				{
					GameObject go = Object.Instantiate(Resources.Load("ArmorDrainEffect") as GameObject, ability.user.transform.position, Quaternion.identity);
					ability.effect = go.GetComponent<Ability_Effect>();
				}
				break;
			case AbilityType.Swarm:
				break;
			case AbilityType.SelfDamage:
				{
					
				}
				break;
			default:
				break;
		}

		if (ability.effect)
			ability.effect.SetEffectActive(ability.isActive);
	}
}
*/