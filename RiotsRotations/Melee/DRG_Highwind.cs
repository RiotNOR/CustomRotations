using Dalamud.Game.ClientState.JobGauge.Types;

namespace Melee
{
    [RotationDesc("Holds back buffs, and will not use GSK if 2 eyes are open so as to not enter Life. Might have to increase burst window timer.", ActionID.LanceCharge, ActionID.DragonSight, ActionID.BattleLitany, ActionID.Geirskogul /* Some actions you used in burst. */)]
    [SourceCode("https://github.com/RiotNOR/CustomRotations/blob/main/RiotsRotations/Melee/DRG_Highwind.cs")]
    internal class DRG_Highwind : DRG_Base
    {
        public override string GameVersion => "6.38";

        public override string RotationName => "Riot's Highwind";

        public override string Description => "A rotation with an opener following The Balance, and more.";

        private static bool GoThroughFirstPath { get; set; }

        private static bool IsOpenerAvailable { get; set; }

        private static bool IsCurrentlyInOpener { get; set; }

        private static bool ShouldEndOpener { get; set; }

        private static DRGGauge JobGauge => Service.JobGauges.Get<DRGGauge>();

        private void HandleOpenerAvailability(out IAction act)
        {
            if (Configs.GetBool("DRG_OpenerAt90")
                && DragonSight.CanUse(out act, CanUseOption.MustUse)
                && BattleLitany.CanUse(out act, CanUseOption.MustUse)
                && LanceCharge.CanUse(out act, CanUseOption.MustUse)
                && DragonFireDive.CanUse(out act, CanUseOption.MustUse)
                && Player.Level >= 90)
            {
                IsOpenerAvailable = true;
                ShouldEndOpener = false;
            }
            else
            {
                IsOpenerAvailable = false;
            }

            act = null;
        }

        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                .SetBool("DRG_OpenerAt90", false, "Use Lvl. 90 opener (ignores other settings during the opener itself)")
                .SetBool("DRG_KeepBuffsAligned", false, "Try to keep buffs aligned with Geirskogul in case of drifting. Statically 6 second remaining on GSK for alignment.")
                .SetBool("DRG_LanceChargeFirst", false, "Move Lance Charge to in front of True Thrust for opener. Not entirely compatible with the above setting.");
        }

        #region GCD actions
        protected override bool GeneralGCD(out IAction act)
        {
            // Always start with Chaotic Spring combo upon
            // entering combat.
            if (!InCombat)
            {
                GoThroughFirstPath = true;
            }

            if (CoerthanTorment.CanUse(out act)) return true;
            if (SonicThrust.CanUse(out act)) return true;
            if (DoomSpike.CanUse(out act)) return true;

            // Make sure we alternate between Chaotic Spring combo
            // and Heavens' Thrust combo
            if (GoThroughFirstPath)
            {
                if (IsLastGCD(ActionID.Disembowel)) GoThroughFirstPath = false;
                if (Disembowel.CanUse(out act)) return true;
            }
            else
            {
                if (IsLastGCD(false, VorpalThrust)) GoThroughFirstPath = true;
                if (VorpalThrust.CanUse(out act)) return true;
            }

            // Regardless of path, cannot use out of order anyway
            if (WheelingThrust.CanUse(out act)) return true;
            if (FangandClaw.CanUse(out act)) return true;

            // Heavens' Thrust
            if (HeavensThrust.CanUse(out act)) return true;
            if (FullThrust.CanUse(out act)) return true;

            // Chaotic Spring
            if (ChaosThrust.CanUse(out act)) return true;
            if (TrueThrust.CanUse(out act)) return true;

            if (PiercingTalon.CanUse(out act)) return true;

            act = null;
            return false;
        }

        //For some gcds very important, even more than healing, defense, interrupt, etc.
        protected override bool EmergencyGCD(out IAction act)
        {
            HandleOpenerAvailability(out act);

            // Ehh?
            if (IsOpenerAvailable
                && Configs.GetBool("DRG_LanceChargeFirst")
                && LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;

            //We are now going to run opener rotation.
            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsOpenerAvailable)
            {
                IsCurrentlyInOpener = true;
            }

            return base.EmergencyGCD(out act);
        }
        #endregion

        #region 0GCD actions
        protected override bool AttackAbility(out IAction act)
        {
            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsCurrentlyInOpener)
            {
                return AttackAbilityOpener(out act);
            }

            //if (JobGauge.EyeCount == 2
            //    && Player.HasStatus(true, StatusID.LanceCharge)
            //    && Geirskogul.CanUse(out act, CanUseOption.MustUse)
            //    || JobGauge.EyeCount < 2
            //    && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

            if (!IsLastAction(false, StarDiver))
            {
                if (Nastrond.CanUse(out act, CanUseOption.MustUse)) return true;

                if (DragonFireDive.CanUse(out act, CanUseOption.MustUse)) return true;
                if (SpineShatterDive.CanUse(out act, CanUseOption.MustUseEmpty)) return true;

                if (WyrmwindThrust.CanUse(out act, CanUseOption.MustUse)) return true;
                if (!IsLastAction(false, HighJump) && MirageDive.CanUse(out act)) return true;

                if (HighJump.EnoughLevel)
                {
                    if (HighJump.CanUse(out act, CanUseOption.MustUse)) return true;
                }
                else
                {
                    if (Jump.CanUse(out act)) return true;
                }
            }

            if (!IsLastAction(false, LifeSurge)
                && StarDiver.CanUse(out act, CanUseOption.MustUse)) return true;

            act = null;
            return false;
        }

        private bool AttackAbilityOpener(out IAction act)
        {
            if (ShouldEndOpener) IsCurrentlyInOpener = false;

            // Use Wyrmwind Thrust after Raiden Thrust #2 in 0GCD position 1
            if (IsLastGCD(ActionID.RaidenThrust))
            {
                if (WyrmwindThrust.CanUse(out act, CanUseOption.MustUse))
                {
                    ShouldEndOpener = true;
                    return true;
                }
            }

            // Use Spineshatter Dive after Fang and Claw #2 in 0GCD position 1
            if (IsLastGCD(false, FangandClaw)
                && IsLastAbility(false, SpineShatterDive))
            {
                if (SpineShatterDive.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Use Spineshatter Dive after Heavens' Thrust in 0GCD position 1
            if (IsLastGCD(false, HeavensThrust))
            {
                if (SpineShatterDive.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            /* 
             *  Moved to GeneralAbilityOpener as it doesn't require the player to be close
             */
            //// Use Mirage Dive before Heavens' Thrust, after Life Surge
            //if (IsLastGCD(false, VorpalThrust)
            //    && abilitiesRemaining == 1)
            //{
            //    if (MirageDive.CanUse(out act)) return true;
            //}

            // Use Dragonfire Dive after Raiden Thrust #1 in 0GCD position 1
            if (IsLastGCD(ActionID.RaidenThrust))
            {
                if (DragonFireDive.CanUse(out act, CanUseOption.MustUse)) return true;
            }

            // Use High Jump after Fang and Claw #1 in 0GCD position 1
            if (IsLastGCD(false, FangandClaw))
            {
                if (HighJump.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            /*
             *  Moved to GeneralAbilityOpener as it doesn't require the player to be close
             */
            //// Use Geirskogul only if no eye exists, in 0GCD position 1
            //if (IsLastGCD(false, WheelingThrust)
            //    && JobGauge.EyeCount == 0
            //    && abilitiesRemaining == 2)
            //{
            //    if (Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;
            //}

            act = null;
            return false;
        }

        //For some 0gcds very important, even more than healing, defense, interrupt, etc.
        protected override bool EmergencyAbility(IAction nextGCD, out IAction act)
        {
            //HandleOpenerAvailability(out act);

            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsCurrentlyInOpener)
            {
                return EmergencyAbilityOpener(nextGCD, out act);
            }


            if (nextGCD is BaseAction action)
            {
                if (InBurst)
                {
                    if (Configs.GetBool("DRG_KeepBuffsAligned")
                        && Geirskogul.ElapsedAfter(24))
                    {
                        if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                    }
                    else if (!Configs.GetBool("DRG_KeepBuffsAligned"))
                    {
                        if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                    }
                }

                if (JobGauge.EyeCount == 2
                    && Player.HasStatus(true, StatusID.LanceCharge)
                    && Geirskogul.CanUse(out act, CanUseOption.MustUse)
                    || JobGauge.EyeCount < 2
                    && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // Make sure Heavens' Thrust gets buffed
                if (nextGCD.IsTheSameTo(false, HeavensThrust, FullThrust)
                    && LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;

                // We buff 5th hit IF we're inside positional
                // @TODO verify this as I'm too lazy at the time of writing
                if (action.EnemyPositional != EnemyPositional.None
                    && action.Target != null)
                {
                    if (action.EnemyPositional == action.Target.FindEnemyPositional() && action.Target.HasPositional())
                    {
                        if (IsLastGCD(false, WheelingThrust)
                            && nextGCD.IsTheSameTo(false, FangandClaw)
                                || IsLastGCD(false, FangandClaw)
                                && nextGCD.IsTheSameTo(false, WheelingThrust))
                        {
                            return LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo);
                        }
                    }
                }
            }

            return base.EmergencyAbility(nextGCD, out act);
        }

        private bool EmergencyAbilityOpener(IAction nextGCD, out IAction act)
        {
            // Buff from Chaotic Spring through Wheeling Thrust #2
            if (nextGCD.IsTheSameTo(true, ChaosThrust))
            {
                if (LanceCharge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
                if (DragonSight.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buff from Wheeling Thrust #1 through Spineshatter Dive #2
            if (nextGCD.IsTheSameTo(false, WheelingThrust))
            {
                if (BattleLitany.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buff Fang and Claw in 0GCD position 2
            if (nextGCD.IsTheSameTo(false, FangandClaw))
            {
                if (LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buff Heavens' Thrust in 0GCD position 1
            if (nextGCD.IsTheSameTo(false, HeavensThrust))
            {
                if (LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            act = null;
            return false;
        }

        //Some 0gcds that don't need to a hostile target in attack range.
        protected override bool GeneralAbility(out IAction act)
        {
            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsCurrentlyInOpener)
            {
                return GeneralAbilityOpener(out act);
            }

            return base.GeneralAbility(out act);
        }

        private bool GeneralAbilityOpener(out IAction act)
        {
            // Immediately use Mirage Dive to get ready regardless of distance
            if (Player.HasStatus(true, StatusID.DiveReady)
                && MirageDive.CanUse(out act)) return true;

            // Use Geirskogul only if no eye exists, in 0GCD position 1
            if (IsLastGCD(false, WheelingThrust)
                && JobGauge.EyeCount == 0)
            {
                if (Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;
            }

            act = null;
            return false;
        }
        #endregion
    }
}