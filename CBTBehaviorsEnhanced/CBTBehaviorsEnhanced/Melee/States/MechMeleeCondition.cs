﻿using BattleTech;
using CustomComponents;
using IRBTModUtils.Extension;

namespace CBTBehaviorsEnhanced
{
	public class ActorMeleeCondition
	{
		// Damage multipliers and effects
		private bool leftHipIsFunctional = false;
		private bool rightHipIsFunctional = false;

		public int LeftLegActuatorsCount { get => leftLegActuatorsCount; }
		private int leftLegActuatorsCount = 0;
		public int RightLegActuatorsCount { get => rightLegActuatorsCount; }
		private int rightLegActuatorsCount = 0;

		public bool LeftFootIsFunctional { get => leftFootIsFunctional; }
		private bool leftFootIsFunctional = false;
		public bool RightFootIsFunctional { get => rightFootIsFunctional; }
		private bool rightFootIsFunctional = false;

		private bool leftShoulderIsFunctional = false;
		private bool rightShoulderIsFunctional = false;

		public int LeftArmActuatorsCount { get => leftArmActuatorsCount; }
		private int leftArmActuatorsCount = 0;
		public int RightArmActuatorsCount { get => rightArmActuatorsCount; }
		private int rightArmActuatorsCount = 0;

		public bool LeftHandIsFunctional { get => leftHandIsFunctional; }
		private bool leftHandIsFunctional = false;
		public bool RightHandIsFunctional { get => rightHandIsFunctional; }
		private bool rightHandIsFunctional = false;

		private bool canMelee = false;
		private bool hasPhysicalAttack = false;

		AbstractActor actor;

		public ActorMeleeCondition(AbstractActor actor)
        {
			this.actor = actor;
			Mod.MeleeLog.Info?.Write($"Calculating melee condition for actor: {actor.DistinctId()}");
			if (actor is Mech mech)
            {
				foreach (MechComponent mc in mech.allComponents)
				{
					switch (mc.Location)
					{
						case (int)ChassisLocations.LeftArm:
						case (int)ChassisLocations.RightArm:
							EvaluateArmComponent(mc);
							break;
						case (int)ChassisLocations.LeftLeg:
						case (int)ChassisLocations.RightLeg:
							EvaluateLegComponent(mc);
							break;
						default:
							break;
					}
				}

				// Check that unit has a physical attack
				if (mech.StatCollection.ContainsStatistic(ModStats.PunchIsPhysicalWeapon) &&
					mech.StatCollection.GetValue<bool>(ModStats.PunchIsPhysicalWeapon))
				{
					hasPhysicalAttack = true;
				}

				// Check various general melee states
				if (Mod.Config.Developer.ForceInvalidateAllMeleeAttacks)
				{
					Mod.MeleeLog.Info?.Write("Invalidated by developer flag.");
					canMelee = false;
				} 
				else if (mech.IsOrWillBeProne || mech.StoodUpThisRound || mech.IsFlaggedForKnockdown)
                {
					Mod.MeleeLog.Info?.Write("Cannot melee when you stand up or are being knocked down");
					canMelee = false;
				} 
				else if (mech.IsDead || mech.IsFlaggedForDeath)
                {
					Mod.MeleeLog.Info?.Write("Cannot melee when dead");
					canMelee = false;
				}
                else
                {
					canMelee = true;
                }

			}
			else
            {
				Mod.MeleeLog.Info?.Write($"  - actor is not a mech, cannot use melee attacks.");
			}
			
        }

		// Only used for testing... yeah, yeah I know
		public ActorMeleeCondition(AbstractActor actor, 
			bool leftHip, bool rightHip, int leftLeg, int rightLeg,
			bool leftFoot, bool rightFoot, bool leftShoulder, bool rightShoulder, 
			int leftArm, int rightArm, bool leftHand, bool rightHand, bool canMelee, bool hasPhysical)
        {
			this.actor = actor;
			this.leftHipIsFunctional = leftHip;
			this.rightHipIsFunctional = rightHip;
			this.leftLegActuatorsCount = leftLeg;
			this.rightLegActuatorsCount = rightLeg;
			this.leftFootIsFunctional = leftFoot;
			this.rightFootIsFunctional = rightFoot;
			this.leftShoulderIsFunctional = leftShoulder;
			this.rightShoulderIsFunctional = rightShoulder;
			this.leftArmActuatorsCount = leftArm;
			this.rightArmActuatorsCount = rightArm;
			this.leftHandIsFunctional = leftHand;
			this.rightHandIsFunctional = rightHand;
			this.canMelee = canMelee;
			this.hasPhysicalAttack = hasPhysical;
        }

		public bool CanCharge()
        {
			if (!canMelee) return false;

			// Cannot charge while unsteady
			if (actor.IsUnsteady) return false;

			return true;
		}

		public bool CanDFA()
        {
			if (!canMelee) return false;

			if (actor is Mech mech && !mech.CanDFA) return false;

			return true;

		}

		// Public functions
		public bool CanKick()
		{
			if (!canMelee) return false;

			// Can't kick with damaged hip actuators
			if (!leftHipIsFunctional || !rightHipIsFunctional) return false;

			return true;
		}

		public bool CanUsePhysicalAttack()
		{
			if (!canMelee) return false;

			if (!hasPhysicalAttack) return false;

			// If the ignore actuators stat is set, allow the attack regardless of actuator damage
			Mech mech = actor as Mech;

			Statistic ignoreActuatorsStat = mech.StatCollection.GetStatistic(ModStats.PhysicalWeaponIgnoreActuators);
			if (ignoreActuatorsStat != null && ignoreActuatorsStat.Value<bool>())
				return true;

			// Damage check - shoulder and hand
			bool leftArmIsFunctional = leftShoulderIsFunctional && leftHandIsFunctional;
			bool rightArmIsFunctional = rightShoulderIsFunctional && rightHandIsFunctional;
			if (!leftArmIsFunctional && !rightArmIsFunctional)
			{
				return false;
			}

			return true;
		}

		public bool CanPunch()
		{
			if (!canMelee) return false;

			// Can't punch with damaged shoulders
			if (!leftShoulderIsFunctional && !rightShoulderIsFunctional) return false;

			return true;
		}

		// Private helper
		private void EvaluateLegComponent(MechComponent mc)
		{
			Mod.MeleeLog.Info?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");

			foreach (string categoryId in Mod.Config.CustomCategories.HipActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftHipIsFunctional = mc.IsFunctional;
					else this.rightHipIsFunctional = mc.IsFunctional;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.UpperLegActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftLegActuatorsCount += mod;
					else this.rightLegActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.LowerLegActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftLegActuatorsCount += mod;
					else this.rightLegActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.FootActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftLeg) this.leftFootIsFunctional = mc.IsFunctional;
					else this.rightFootIsFunctional = mc.IsFunctional;
					break;
				}
			}

		}

		private void EvaluateArmComponent(MechComponent mc)
		{
			Mod.MeleeLog.Debug?.Write($"  - Actuator: {mc.Description.UIName} is functional: {mc.IsFunctional}");

			foreach (string categoryId in Mod.Config.CustomCategories.ShoulderActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftArm) this.leftShoulderIsFunctional = mc.IsFunctional;
					else this.rightShoulderIsFunctional = mc.IsFunctional;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.UpperArmActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftArm) this.leftArmActuatorsCount += mod;
					else this.rightArmActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.LowerArmActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					int mod = mc.IsFunctional ? 1 : 0;
					if (mc.Location == (int)ChassisLocations.LeftArm) this.leftArmActuatorsCount += mod;
					else this.rightArmActuatorsCount += mod;
					break;
				}
			}

			foreach (string categoryId in Mod.Config.CustomCategories.HandActuatorCategoryId)
			{
				if (mc.mechComponentRef.IsCategory(categoryId))
				{
					if (mc.Location == (int)ChassisLocations.LeftArm) this.leftHandIsFunctional = mc.IsFunctional;
					else this.rightHandIsFunctional = mc.IsFunctional;
					break;
				}
			}
		}

	}

}
