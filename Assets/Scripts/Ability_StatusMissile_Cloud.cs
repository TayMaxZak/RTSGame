using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_StatusMissile_Cloud : MonoBehaviour
{
	[SerializeField]
	private float lifetime = 3;
	private float curLifetime;

	[SerializeField]
	private float minRadius = 2;
	[SerializeField]
	private float maxRadius = 4;
	private float curRadius;

	[SerializeField]
	private float fallSpeed = 1;

	private Unit parentUnit;
	private int team = 0;

	private List<Unit> alreadyDamaged;

	private GameRules gameRules;

	void Awake()
	{
		alreadyDamaged = new List<Unit>();

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		curLifetime = lifetime;

		UpdateRadius();
	}

	public void SetParentUnit(Unit u)
	{
		parentUnit = u;
		team = u.team;
	}

	void Update()
	{
		curLifetime -= Time.deltaTime;
		UpdateRadius();
		transform.position += Vector3.down * fallSpeed * Time.deltaTime;

		if (curLifetime > 0)
		{
			Collider[] cols = Physics.OverlapSphere(transform.position, curRadius, gameRules.entityLayerMask);
			List<Unit> units = new List<Unit>();
			for (int i = 0; i < cols.Length; i++)
			{
				Unit unit = GetUnitFromCol(cols[i]);

				if (!unit) // Only works on units
					continue;

				if (alreadyDamaged.Contains(unit)) // Already damaged this unit
					continue;

				if (units.Contains(unit)) // Ignore multiple colliders for one unit
					continue;

				//if (unit == parentUnit) // Don't add ourselves
				//	continue;

				//if (unit.Type == EntityType.Flagship) // Can't damage Flagships
				//	continue;

				units.Add(unit);
				alreadyDamaged.Add(unit);
			}

			foreach (Unit u in units)
			{
				Vector4 hp = u.GetHP();
				// Scaling damage is ignored against Flagships
				float dmg = u.Type != EntityType.Flagship ? gameRules.ABLY_statusMissileDamage + gameRules.ABLY_statusMissileDamageBonusMult * (hp.y + hp.w) : gameRules.ABLY_statusMissileDamage;
				if (u.team != team)
					u.Damage(dmg, 0, DamageType.Chemical);
				else // Reduced damage to allies
					u.Damage(dmg * gameRules.DMG_ffDamageMultSplash, 0, DamageType.Chemical);

				u.AddStatus(new Status(parentUnit.gameObject, StatusType.ArmorMelt));
			}
		}
		else
			End();
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}

	void UpdateRadius()
	{
		curRadius = Mathf.Lerp(minRadius, maxRadius, 1 - (curLifetime / lifetime));
		transform.localScale = new Vector3(curRadius * 2, curRadius * 2, curRadius * 2);
	}

	Unit GetUnitFromCol(Collider col)
	{
		Entity ent = col.GetComponentInParent<Entity>();
		if (ent)
		{
			if (ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit)))
				return (Unit)ent;
			else
				return null;
		}
		else
		{
			return null;
		}
	}

	void End()
	{
		Destroy(gameObject);
	}
}
