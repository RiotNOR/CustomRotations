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

        // Has now been opened up in DRG_Base.cs in the main RotationSolver.Basic project.
        // @REF: https://github.com/ArchiDog1998/RotationSolver/tree/main/RotationSolver.Basic/Rotations/Basic
        // @REF: https://github.com/ArchiDog1998/FFXIVRotations/tree/main/DefaultRotations 
        //private static DRGGauge JobGauge => Service.JobGauges.Get<DRGGauge>();

        // Just makes sure our opener can actually be ran and complete.
        // @TODO: Add check that we have no eyes, or handle that
        // inside the actual opener (probably best to do it that way).
        private void HandleOpenerAvailability()
        {
            // Since we run this method inside EmergencyGCD, we require the
            // CanUseOption.IgnoreClippingCheck option to be set.
            // This is because RotationSolver will try to keep 0GCDs from
            // clipping GCDs. You can, however, skip the CanUseOption if ran in
            // GeneralAbility for example. However, that has a distance threshold
            // while EmergencyGCD runs regardless of distance.
            if (Configs.GetBool("DRG_OpenerAt90")
                && DragonSight.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && BattleLitany.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && LanceCharge.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && Player.Level >= 90)
            {
                IsOpenerAvailable = true;
                ShouldEndOpener = false;
            }
            else
            {
                IsOpenerAvailable = false;
            }
        }

        // No constructor, so we use this CreateConfiguration() method
        // to setup our settings.
        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                .SetBool("DRG_OpenerAt90", false, "Use Lvl. 90 opener (ignores other settings during the opener itself)")
                .SetBool("DRG_KeepBuffsAligned", false, "Try to keep buffs aligned with Geirskogul in case of drifting. Statically 6 second remaining on GSK for alignment.")
                .SetBool("DRG_LanceChargeFirst", false, "Move Lance Charge to in front of True Thrust for opener. Not entirely compatible with the above setting.")
                .SetBool("DRG_LifeSurgeFifthHit", false, "Buff fifth combo hit with Life Surge IF you're in position or have True North on.");
        }

        // Here we can add abilities to use during countdown to
        // prepare for a fight.
        protected override IAction CountDownAction(float remainTime)
        {
            // We check that the remaining time of the countdown (do "/cd 7" in chat to test)
            // is lower than the time it takes to cast TrueNorth plus the setting in
            // Param -> Basic, and the slider for "Set time advance of using casting actions on counting down"
            //if (remainTime < TrueNorth.CastTime + Service.Config.CountDownAhead)
            //{
                // We don't have an out parameter (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/out-parameter-modifier)
                // in CountdownAction, so we use a Discard (https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards)
                // because we don't need it at all.Then we just return the IBaseAction itself.
                //if (TrueNorth.CanUse(out _, CanUseOption.IgnoreClippingCheck)) return TrueNorth;
            //}

            return base.CountDownAction(remainTime);
        }

        #region GCD actions

        // General cooldowns (Global cooldowns)
        protected override bool GeneralGCD(out IAction act)
        {
            // If we're not in combat, we reset our combo path.
            // @TODO: Make sure we have no active combo going.
            if (!InCombat)
            {
                GoThroughFirstPath = true;
            }

            // These will only run if AoE is enabled.
            // Also, Dragoon AOEs only run if there are 3 or more mobs nearby.
            if (CoerthanTorment.CanUse(out act)) return true;
            if (SonicThrust.CanUse(out act)) return true;
            if (DoomSpike.CanUse(out act)) return true;

            // Make sure we alternate between Chaotic Spring combo
            // and Heavens' Thrust combo.
            // @TODO: Add check for NumberOfHostilesInRange == 2 to apply
            // Chaotic Spring debuff on the enemies for increased DPS.
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

            // Heavens' Thrust combo
            if (FullThrust.CanUse(out act)) return true;

            // Chaotic Spring
            if (ChaosThrust.CanUse(out act)) return true;
            //if (UseBurstMedicine(out act)) return true;
            if (TrueThrust.CanUse(out act)) return true;

            // Ranged uptime. Toggleable in Actions by the player.
            if (PiercingTalon.CanUse(out act)) return true;

            act = null;
            return false;
        }

        //For some gcds very important, even more than healing, defense, interrupt, etc.
        protected override bool EmergencyGCD(out IAction act)
        {
            // EmergencyGCD is ran first, ref RotationSolver.Basic.CustomRotation, IAction GCD method
            // so we just run this here.
            HandleOpenerAvailability();

            // Ehh? Probably shouldn't have this as an option.
            if (IsOpenerAvailable
                && Configs.GetBool("DRG_LanceChargeFirst")
                && LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;

            // We are now telling every other function that the Opener
            // can and is being run.
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
        // Attack ABILITY. Not attack SKILLS. ;)
        protected override bool AttackAbility(out IAction act)
        {
            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsCurrentlyInOpener)
            {
                return AttackAbilityOpener(out act);
            }

            // Just to make sure Stardiver doesn't get doubleweaved.
            // @TODO: Do this in a better way.
            if (!IsLastAction(false, StarDiver))
            {
                if (Nastrond.CanUse(out act, CanUseOption.MustUse)) return true;

                if (DragonFireDive.CanUse(out act, CanUseOption.MustUse)) return true;
                if (SpineShatterDive.CanUse(out act, CanUseOption.MustUseEmpty)) return true;

                if (WyrmwindThrust.CanUse(out act, CanUseOption.MustUse)) return true;

                // Trying not to double weave High Jump and Mirage Dive.
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

            // Don't want to double weave with a buff either.
            if (!IsLastAction(false, LifeSurge)
                && StarDiver.CanUse(out act, CanUseOption.MustUse)) return true;

            act = null;
            return false;
        }

        private bool AttackAbilityOpener(out IAction act)
        {
            // Wyrmwind Thrust ends our opener, and thus lets another
            // function know that we can disable the opener and run things
            // off cooldown.
            // @TODO: Should make more checks in case of reopeners and/or filler rotations.
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
            if (IsLastGCD(true, FullThrust))
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
            if (Level == 90
                && Configs.GetBool("DRG_OpenerAt90")
                && IsCurrentlyInOpener)
            {
                return EmergencyAbilityOpener(nextGCD, out act);
            }



            if (nextGCD is BaseAction action && !IsCurrentlyInOpener && InCombat)
            {
                // We don't want Geirskogul to run without Lance Charge if we're
                // close to going into Life of the Dragon.
                if (EyeCount == 2
                        && Player.HasStatus(true, StatusID.LanceCharge)
                        && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // If player has disabled Automatic Burst, these will not be ran.
                // This is how you will control burst in harder content to align with your teammates.
                // Remember, Geirskogul will not run if you have 2 eyes open.
                if (InBurst)
                {
                    // Geirskogul can drift a bit. Unsure why, but to combat that until I understand
                    // more of creating these rotations we'll align it within 6 seconds of Geirskogul IF the user
                    // has ticked the checkbox for this.
                    // Ref: CreateConfiguration()
                    if (Configs.GetBool("DRG_KeepBuffsAligned")
                        && (Geirskogul.ElapsedAfter(24) || !Geirskogul.IsCoolingDown))
                    {
                        if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                    }
                    else if (!Configs.GetBool("DRG_KeepBuffsAligned"))
                    {
                        if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                        if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                    }
                }

                // If we have less than 2 eyes open (0 or 1), feel free to spam Geirskogul.
                if (EyeCount < 2
                    && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // Make sure Heavens' Thrust gets buffed
                if (nextGCD.IsTheSameTo(true, FullThrust)
                    && LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;

                if (Configs.GetBool("DRG_LifeSurgeFifthHit"))
                {
                    // We buff 5th hit only IF we're inside positional.
                    if (action.EnemyPositional != EnemyPositional.None
                        && action.Target != null)
                    {
                        // If in position for a positional
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

                        // Or if we have True North
                        if (Player.HasStatus(true, StatusID.TrueNorth))
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
            if (nextGCD.IsTheSameTo(true, FullThrust))
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
                && EyeCount == 0)
            {
                if (Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;
            }

            act = null;
            return false;
        }
        #endregion
    }
}