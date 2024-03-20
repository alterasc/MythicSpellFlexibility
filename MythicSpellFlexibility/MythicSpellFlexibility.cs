using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using System.Collections.Generic;
using System.Linq;

namespace MythicSpellFlexibility;

[AllowedOn(typeof(BlueprintUnitFact), false)]
[AllowMultipleComponents]
[TypeId("687cc9e01175449f8a656642a203ec7d")]
public class MythicSpellFlexibility : UnitFactComponentDelegate,
    ILevelUpCompleteUIHandler,
    IUnitSubscriber,
    ISubscriber
{
    public override void OnTurnOn()
    {
        RefreshKnownSpells();
    }

    public override void OnTurnOff()
    {
        RefreshKnownSpells();
    }

    /// <summary>
    /// Takes all special mythic spells and places as (IsTemporary + IsFromMythicSpellList) spells into normal spellbooks
    /// Then removes all (IsTemporary + IsFromMythicSpellList) spells from normal spellbooks that do not have counterpart in mythic spellbook
    /// </summary>
    private void RefreshKnownSpells()
    {
        List<AbilityData> temporarilyAddedMythicSpells = [];

        // collect all known mythic spells
        Dictionary<BlueprintAbility, int> knownMythicSpellDict = [];
        var mythicSpellbooks = this.Owner.Spellbooks.Where(x => x.IsMythic && x.IsStandaloneMythic).ToList();
        foreach (var mythicSpellbook in mythicSpellbooks)
        {
            var maxKnownLvl = mythicSpellbook.GetMaxSpellLevel();
            Main.LogTrace($"Adding spells from {mythicSpellbook.Blueprint.Name} up to level {maxKnownLvl}");
            for (int i = 0; i <= maxKnownLvl; i++)
            {
                Main.LogTrace($"Adding spells at level {i}");
                foreach (AbilityData knownSpell in mythicSpellbook.GetKnownSpells(i))
                {
                    if (knownSpell.IsFromMythicSpellList)
                    {
                        if (knownSpell.Blueprint.AssetGuid.m_Guid.ToString() != "a6a86db7-5c6a-f6d4-1aa4-80f05adae693") //Heroes Never Surrender                           
                        {
                            Main.LogTrace($"Found special spell: {knownSpell.Name} at lvl {i}");
                            knownMythicSpellDict[knownSpell.Blueprint] = i;
                        }
                    }
                }
            }
        }

        // add them to normal spellbooks
        foreach (var (ability, level) in knownMythicSpellDict.AsEnumerable())
        {
            foreach (ClassData classData in this.Owner.Progression.Classes)
            {
                if (classData.Spellbook != null && !classData.Spellbook.IsMythic)
                {
                    Spellbook spellbook = this.Owner.Descriptor.GetSpellbook(classData.Spellbook);
                    if (spellbook != null)
                    {
                        var sblvl = spellbook.GetMaxSpellLevel();
                        if (sblvl < level)
                        {
                            Main.LogTrace($"{ability} is too high level ({sblvl} vs {level} )for spellbook {spellbook}, skipping");
                        }
                        else
                        {
                            if (!temporarilyAddedMythicSpells.Any(x => x.Blueprint == ability && x.Spellbook == spellbook))
                            {
                                AbilityData abilityData = AddKnownTemporaryMythic(spellbook, level, ability);
                                temporarilyAddedMythicSpells.Add(abilityData);
                                Main.LogTrace($"Added {ability} to spellbook {spellbook} at {level}");
                            }
                            else
                            {
                                Main.LogTrace($"{ability} already present in spellbook {spellbook}, skipping");
                            }
                        }
                    }
                }
            }
        }

        // remove all tmp+mythic spells that were not present
        foreach (ClassData classData in this.Owner.Progression.Classes)
        {
            if (classData.Spellbook != null && !classData.Spellbook.IsMythic)
            {
                Spellbook spellbook = this.Owner.Descriptor.GetSpellbook(classData.Spellbook);
                if (spellbook != null)
                {
                    var allTmpMythicSpells = spellbook.GetAllKnownSpells().Where(x => x.IsTemporary && x.IsFromMythicSpellList)
                        .Where(x => !temporarilyAddedMythicSpells.Contains(x))
                        .ToList();
                    foreach (var spell in allTmpMythicSpells)
                    {
                        spell.Spellbook?.RemoveTemporarySpell(spell);
                    }
                }
            }
        }
    }

    private static AbilityData AddKnownTemporaryMythic(Spellbook sb, int spellLevel, BlueprintAbility blueprint)
    {
        AbilityData abilityData = sb.SureKnownSpells(spellLevel).FirstItem((AbilityData s) => s.Blueprint == blueprint);
        if (abilityData == null)
        {
            abilityData = new AbilityData(blueprint, sb, spellLevel)
            {
                IsTemporary = true,
                IsFromMythicSpellList = true
            };
            sb.SureKnownSpells(spellLevel).Add(abilityData);
            sb.AddKnownSpellLevel(blueprint, spellLevel);
        }

        return abilityData;
    }

    public void HandleLevelUpComplete(UnitEntityData unit, bool isChargen)
    {
        if (unit == Owner)
        {
            RefreshKnownSpells();
        }
    }
}
