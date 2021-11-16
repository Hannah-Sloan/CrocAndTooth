// Using Inscryption
using UnityEngine;
using DiskCardGame;

// Modding Inscryption
using BepInEx;
using APIPlugin;

using System.Collections;
using System.Collections.Generic;
using System.IO; // Loading Sigil and Card art.

namespace CrocAndTooth
{
    [BepInPlugin("hannah.inscryption.CrocAndTooth", "CrocAndTooth", "1.0.0")]
    [BepInDependency("cyantist.inscryption.api", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {"hannah.inscryption.crocandtooth"} is loaded!");

            AddToothyAbility();
            AddCrocodile();
        }

        private void AddCrocodile()
        {
            // Load crocodile image.
            byte[] imgBytes = System.IO.File.ReadAllBytes(Path.Combine(this.Info.Location.Replace("CrocAndTooth.dll", ""), "Artwork/crocodile.png"));
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);

            NewCard.Add("Crocodile", new List<CardMetaCategory>() { CardMetaCategory.ChoiceNode }, CardComplexity.Simple, CardTemple.Nature, "Crocodile", 1, 2, description: "Needs to see a dentist.", cost: 1, tribes: new List<Tribe>() { Tribe.Reptile }, abilities: new List<Ability>() { Toothy._ability }, tex: tex);
        }

        private NewAbility AddToothyAbility()
        {
            AbilityInfo info = ScriptableObject.CreateInstance<AbilityInfo>();
            info.powerLevel = 0; // Don't know what this really should be. Either way it's not high.
            info.rulebookName = "Toothy";
            info.rulebookDescription = "When [creature] takes damage, it will award teeth equal to the damage taken.";
            info.metaCategories = new List<AbilityMetaCategory> { AbilityMetaCategory.Part1Rulebook, AbilityMetaCategory.Part1Modular };
            info.opponentUsable = false; // I don't think AI can gain teeth so this probably isn't a good ability for the opponent.

            List<DialogueEvent.Line> lines = new List<DialogueEvent.Line>();
            DialogueEvent.Line line = new DialogueEvent.Line();
            line.text = "When this creature takes damage, it will award teeth equal to the damage taken.";
            lines.Add(line);
            info.abilityLearnedDialogue = new DialogueEvent.LineSet(lines);

            // Load Toothy sigal image.
            byte[] imgBytes = System.IO.File.ReadAllBytes(Path.Combine(this.Info.Location.Replace("CrocAndTooth.dll", ""), "Artwork/toothy.png"));
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);

            NewAbility ability = new NewAbility(info, typeof(Toothy), tex, AbilityIdentifier.GetAbilityIdentifier("hannah.inscryption.crocandtooth", info.rulebookName));
            Toothy._ability = ability.ability;
            return ability;
        }

        public class Toothy : AbilityBehaviour
        {
            public override Ability Ability { get { return _ability; } }
            public static Ability _ability;

            // Ability activates whenever this card takes at least 1 damage.
            public override bool RespondsToTakeDamage(PlayableCard source)
            {
                return System.Math.Min(source.lastFrameAttack, base.Card.lastFrameHealth) > 0;
            }

            // Caching for good code practice, wouldn't make a difference either way.
            WaitForSeconds waitShort = new WaitForSeconds(.25f);
            WaitForSeconds waitLong = new WaitForSeconds(.5f);

            public override IEnumerator OnTakeDamage(PlayableCard source)
            {
                // Pretty sure I have to do this calculation before PreSuccessfulTriggerSequence. IDK why.

                // I'm afraid if you get hit with 100 atk and only have 1 health lastFrameAttack will be 100 not 1.
                // So I just take the minimum of your remaining health and the attack in case.
                int damaged = System.Math.Min(source.lastFrameAttack, base.Card.lastFrameHealth);
                yield return base.PreSuccessfulTriggerSequence();
                yield return waitShort;

                // Teach this sigils gimmick if the player hasn't learned it yet.
                if (BoardManager.Instance is BoardManager3D)
                {
                    yield return waitLong;
                    yield return base.LearnAbility(0f);
                }

                var saveView = ViewManager.Instance.CurrentView;

                // The whole gain teeth thing.
                ViewManager.Instance.SwitchToView(View.Scales, false, false);
                yield return waitShort;
                yield return CurrencyBowl.Instance.ShowGain(damaged, true);
                RunState.Run.currency += damaged;
                yield return waitShort;

                // Put the view back to wherever the player had it.
                ViewManager.Instance.SwitchToView(saveView, false, false);
                yield return waitShort;

                yield break;
            }
        }
    }
}
