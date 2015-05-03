﻿using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Infusion
{
    public class CompInfusion : ThingComp
    {
	    public bool IsTried
	    {
		    get { return isTried; }
	    }

	    private bool isTried;
	    private bool isInfused;
		private InfusionPrefix prefix;
	    private InfusionSuffix suffix;

	    public Pair<InfusionPrefix, InfusionSuffix> Infusion
	    {
		    get
		    {
			    return new Pair<InfusionPrefix, InfusionSuffix>
			    {
				    First = prefix,
				    Second = suffix
			    };
		    }
	    }

		public bool SetInfusion()
		{
			if (isTried)
				return false;
			var compQuality = parent.GetComp<CompQuality>();
			if (compQuality == null)
			{
				return false;
			}

			var qc = compQuality.Quality;
			if (qc > QualityCategory.Normal) return GenerateInfusion(qc);

			prefix = InfusionPrefix.None;
			suffix = InfusionSuffix.None;
			isTried = true;
			return false;
		}

	    private bool GenerateInfusion(QualityCategory qc)
		{
			bool passPrefix = false, passSuffix = false;

			/** PrefixTable
			 * Legendary	8 75
			 * Masterwork	7 58
			 * Excellent	6 41
			 * Superior		5 24
			 * Good			4 07
			 */
		    var chance = GetChance(qc, true);
			//Lower chance with ranged weapons
			if (parent.def.IsRangedWeapon)
				chance *= 0.88f;

			var rand = Rand.Range(0, 100);

			if (rand >= chance)
			{
				prefix = InfusionPrefix.None;
				passPrefix = true;
			}

			/**	SuffixTable
			 * Legendary	8 95
			 * Masterwork	7 72
			 * Excellent	6 49
			 * Superior		5 27
			 * Good			4 05
			 */
		    chance = GetChance(qc, false);
			//Lower chance with ranged weapons
			if (parent.def.IsRangedWeapon)
				chance *= 0.70f;

			rand = Rand.Range(0, 100);
			if (rand >= chance)
			{
				suffix = InfusionSuffix.None;
				passSuffix = true;
			}

			if (passPrefix && passSuffix)
			{
				isTried = true;
				return false;
			}

		    var tierMod = 1f;
			if(parent.Stuff != null)
			{
				List<StuffCategoryDef> stuffCategory = parent.Stuff.stuffProps.categories;
				if (stuffCategory.Exists(s => s == StuffCategoryDefOf.Metallic))
					tierMod *= parent.Stuff.stuffProps.statFactors.Find(s => s.stat == StatDefOf.Beauty).value;
			}
		    if (tierMod > 3)
		    {
			    tierMod -= (tierMod - 3)/2;
		    }

			if (!passPrefix)
			{
				/** PrefixTable
				 * Tier 1		45 - (qc - 4)
				 * Tier 2		32 - (qc - 4)
				 * Tier 3		23 + 2 * (qc - 4)
				 */
				rand = (int)(tierMod * Rand.Range(0, 100));
				if (rand >= 23 + 2 * ((int)qc - 4))
					rand = MathInfusion.Rand(InfusionPrefix.Tier3, InfusionPrefix.End);
				else if (rand >= 32 - (int)qc + 4)
					rand = MathInfusion.Rand(InfusionPrefix.Tier2, InfusionPrefix.Tier3);
				else
					rand = MathInfusion.Rand(InfusionPrefix.Tier1, InfusionPrefix.Tier2);

				prefix = (InfusionPrefix)rand;
			}

			if (!passSuffix)
			{
				/** SuffixTable
				 * Tier 1		50 - (qc - 4)
				 * Tier 2		38 - (qc - 4)
				 * Tier 3		12 + 2 * (qc - 4)
				 */
				rand = (int)(tierMod * Rand.Range(0, 100));
				if (rand >= 12 + 2 * ((int)qc - 4))
					rand = MathInfusion.Rand(InfusionSuffix.Tier3, InfusionSuffix.End);
				else if (rand >= 38 - (int)qc + 4)
					rand = MathInfusion.Rand(InfusionSuffix.Tier2, InfusionSuffix.Tier3);
				else
					rand = MathInfusion.Rand(InfusionSuffix.Tier1, InfusionSuffix.Tier2);

				suffix = (InfusionSuffix)rand;
			}

			//For added hit points
			parent.HitPoints = parent.MaxHitPoints;

			MoteThrower.ThrowText(parent.Position.ToVector3Shifted(), StaticSet.StringInfused, new Color(1f, 0.4f, 0f));
			isTried = true;
			isInfused = true;
			return true;
	    }

	    public override void PostSpawnSetup()
	    {
		    base.PostSpawnSetup();
		    SetInfusion();

		    if (isInfused)
			    InfusionLabelManager.Register(this);
	    }

	    public override void PostExposeData()
	    {
		    base.PostExposeData();
			Scribe_Values.LookValue(ref isTried, "isTried", true);
		    Scribe_Values.LookValue(ref isInfused, "isInfused", false);
		    Scribe_Values.LookValue(ref prefix, "prefix", InfusionPrefix.None);
		    Scribe_Values.LookValue(ref suffix, "suffix", InfusionSuffix.None);
	    }

	    public override void PostDeSpawn()
	    {
		    base.PostDeSpawn();

			if(isInfused)
				InfusionLabelManager.DeRegister(this);
	    }

	    public override bool AllowStackWith(Thing other)
	    {
			//Same weapon, same stuff, same pre&suffix? Not possible
			//Weapons are not stackable anyway
		    return false;
	    }

		public override string CompInspectStringExtra()
		{
			if (prefix == InfusionPrefix.None && suffix == InfusionSuffix.None)
				return null;

			QualityCategory qc;
			parent.TryGetQuality(out qc);
			return StaticSet.StringInfusionFullName + ": " + parent.GetInfusedLabel() + " (" + qc.GetLabel() + ")";
		}

	    public override string GetDescriptionPart()
	    {
		    var prePass = prefix == InfusionPrefix.None;
		    var sufPass = suffix == InfusionSuffix.None;

		    if (prePass && sufPass)
			    return null;

			var result = new StringBuilder(null);
		    result.AppendLine(StaticSet.StringInfusionInfo.Translate(parent.GetInfusedLabel(true, false)));
		    result.AppendLine();

		    if (!prePass)
		    {
				result.Append(StaticSet.StringInfusionInfoPrefix.Translate(prefix.GetInfusionLabel()) + " ");
			    result.AppendLine(StaticSet.StringInfusionInfoPrefixBonus.Translate(prefix.GetInfusionDescription()));
			}
		    if (!prePass && !sufPass)
		    {
			    result.AppendLine();
			    result.Append(StaticSet.StringInfusionInfoPreSuffix.Translate(suffix.GetInfusionLabel()) + " ");
		    }
		    else if(!sufPass)
			    result.Append(StaticSet.StringInfusionInfoSuffix.Translate(suffix.GetInfusionLabel()) + " ");

		    if (!sufPass)
				result.AppendLine(StaticSet.StringInfusionInfoSuffixBonus.Translate(suffix.GetInfusionDescription()));

		    return base.GetDescriptionPart() + result;
	    }

	    private static float GetChance(QualityCategory qc, bool isPrefix)
	    {
			return isPrefix ? 
				(int)qc * 20 - 95 : (int)qc * 23 - 89;
	    }
    }
}
