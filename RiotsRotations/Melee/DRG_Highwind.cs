namespace Melee
{
    [RotationDesc("Holds back buffs, and will not use GSK if 2 eyes are open so as to not enter Life. Might have to increase burst window timer.", ActionID.LanceCharge, ActionID.DragonSight, ActionID.BattleLitany, ActionID.Geirskogul /* Some actions you used in burst. */)]
    [SourceCode("https://github.com/RiotNOR/CustomRotations/blob/main/RiotsRotations/Melee/DRG_Highwind.cs")]
    internal class DRG_Highwind : DRG_Base
    {
        //Change this to the game version right now.
        public override string GameVersion => "6.38";

        public override string RotationName => "Riot's Highwind";

        public override string Description => "A rotation with an opener following The Balance, and more.";

        private static bool IsOpenerAvailable { get; set; }

        private static bool IsCurrentlyInOpener { get; set; }

        private static bool ShouldEndOpener { get; set; }

        /*
         * To make it shorter and faster to use, we make a shorthand 
         * for Player.HasStatus and Target.HasStatus.
         */
        private bool PStatus(StatusID statusID) => Player.HasStatus(true, statusID);
        private bool PStatusEnd(StatusID statusID, float time) => Player.WillStatusEnd(time, true, statusID);
        private bool TStatus(StatusID statusID) => Target.HasStatus(true, statusID);
        private bool TStatusEnd(StatusID statusID, float time) => Target.WillStatusEnd(time, true, statusID);

        // No constructor, so we use this CreateConfiguration() method
        // to setup our settings.
        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                //.SetBool("DRG_WeaveSafety", true, "Make extra sure we do not insert a third 0GCD after already having cast two (recommended)")
                .SetBool("DRG_OpenerAt88", false, "Use Lvl. 88+ opener (ignores other settings during the opener itself)")
                .SetBool("DRG_JumpsOnlyInDirectMelee", true, "Only use jumps if within 1yalm")
                ;
        }

        /*
         * This can be used to do things with your variables or 
         * whatever upon area change. This does, however, not run
         * if and when you die. Maybe if you die after you've done an
         * in-dungeon area change. Bit unsure.
         */
        public override void OnTerritoryChanged()
        {
            base.OnTerritoryChanged();
        }

        /*
         * Just makes sure our opener can actually be ran and complete.
         * @TODO: Add check that we have no eyes, or handle that
         * inside the actual opener (probably best to do it that way).
         */
        private void HandleOpenerAvailability()
        {
            /*
             * Since we run this method inside EmergencyGCD, we require the
             * CanUseOption.IgnoreClippingCheck option to be set.
             * This is because RotationSolver will try to keep 0GCDs from
             * clipping GCDs. You can, however, skip the CanUseOption if ran in
             * GeneralAbility for example. However, that has a distance threshold
             * while EmergencyGCD runs regardless of distance.
             */
            if (Configs.GetBool("DRG_OpenerAt88")
                && DragonSight.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && BattleLitany.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && LanceCharge.CanUse(out _, CanUseOption.IgnoreClippingCheck)
                && Player.Level >= 88)
            {
                IsOpenerAvailable = true;
                ShouldEndOpener = false;
            }
            else
            {
                IsOpenerAvailable = false;
            }
        }

        /*
         * As long as we're above level 18, which is when we first acquire
         * Disembowel, we can check for the buff "Power Surge". Before
         * that we will just return true regardless as we cannot buff
         * anything at that point.
         */
        private bool HandlePowerSurge()
        {
            if (PStatus(StatusID.PowerSurge))
            {
                return true;
            }
            else if (Level < 18)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /*
         * Oh hello there!
         * Rotation Solver will always try to ensure no abilities are ran
         * three times without a GCD. The only time it SHOULD be allowed is
         * if you have auto true north or auto provoke enabled. However,
         * some times it can still happen thanks to this game being 3 ticks
         * per second, and maybe some other factors. Welp, just to make sure 
         * this doesn't happen we check the recorded actions to make sure the
         * last two actions used were not both abilities (0GCDs). 
         * 
         * First we check if they were. If true, we then insert another check
         * to see if perhaps it's been long enough that we can surmise there's
         * been a break in combat due to mechanics (having to run away for example).
         * If true, then we can still cast it as we wouldn't technically be clipping
         * any GCDs since we weren't fighting regardless.
         * 
         * The time for this is set at 2 seconds. However, this might need more testing.
         */
        private static bool CanUseAbilitySafely()
        {
            if (RecordActions.Length > 1 && RecordActions[0].Action.ActionCategory.Value.Name == "Ability"
                && RecordActions[1].Action.ActionCategory.Value.Name == "Ability")
            {
                if (RecordActions[1].UsedTime.AddSeconds(2) < DateTime.Now)
                {
                    //Help.Log("Ability usage safe because 2s has elapsed since second-to-last action. Pause in combat most likely.");
                    return true;
                }
                else
                {
                    //Help.Log("Ability usage UNSAFE. Not enough time elapsed");
                    return false;
                }
            }
            else
            {
                //Help.Log("Ability usage SAFE. Last 2 actions were not just 0GCDs");
                return true;
            }
        }

        /*
         * We need this check to make sure the "simulate key presses" function
         * doesn't freak out as it'll keep trying to spam "High Jump" due to "Jump" 
         * not actually being on cooldown.
         */
        private static bool LazyJump(out IAction act)
        {
            if (Level >= 74)
            {
                if (HighJump.CanUse(out act, CanUseOption.MustUse)) return true;
            }
            else
            {
                if (Jump.CanUse(out act)) return true;
            }

            act = null;
            return false;
        }

        /*
         * You might not wish to use the jumps at longer range (max 3yalm for now anyway)
         * so we try to limit it for the players safety. The setting mentions 1yalm, but 
         * this could be a bit closer than most would stand so we "cheat" a little by 
         * adding 0.2y more.
         */
        private bool HandleJumps(out IAction action)
        {
            if (RecordActions[0].Action.ActionCategory.Value.Name != "Ability")
            {
                if (Configs.GetBool("DRG_JumpsOnlyInDirectMelee"))
                {
                    if (Target.DistanceToPlayer() <= 1.2)
                    {
                        if (LazyJump(out action)) return true;
                        if (DragonFireDive.CanUse(out action, CanUseOption.MustUse)) return true;
                        if (SpineShatterDive.CanUse(out action, CanUseOption.MustUseEmpty)) return true;
                    }
                }
                else
                {
                    if (LazyJump(out action)) return true;
                    if (DragonFireDive.CanUse(out action, CanUseOption.MustUse)) return true;
                    if (SpineShatterDive.CanUse(out action, CanUseOption.MustUseEmpty)) return true;
                }
            }

            action = null;
            return false;
        }

        #region GCD actions
        protected override bool GeneralGCD(out IAction act)
        {
            // These will only run if AoE is enabled.
            // Also, Dragoon AOEs only run if there are 3 or more mobs nearby.
            if (CoerthanTorment.CanUse(out act)) return true;
            if (SonicThrust.CanUse(out act)) return true;
            if (DoomSpike.CanUse(out act)) return true;

            if (PStatus(StatusID.SharperFangandClaw)
                && FangandClaw.CanUse(out act, CanUseOption.MustUse)) return true;

            if (PStatus(StatusID.EnhancedWheelingThrust)
                && WheelingThrust.CanUse(out act, CanUseOption.MustUse)) return true;

            if (FullThrust.CanUse(out act)) return true;

            /*
             * We need proper checks here to surmise whether the target has the
             * high-level debuff, or the lower one. Also instead of using a bool
             * to dictate the proper pathing between the two combos we stick
             * to checking the remaining time on "Power Surge".
             */
            if (Level >= 86 && (!TStatus(StatusID.ChaoticSpring) || TStatusEnd(StatusID.ChaoticSpring, 6))
                || Level < 86 && (!TStatus(StatusID.ChaosThrust) || TStatusEnd(StatusID.ChaosThrust, 6))
                || PStatusEnd(StatusID.PowerSurge, 10))
            {
                if (ChaosThrust.CanUse(out act, CanUseOption.MustUse)) return true;
                if (Disembowel.CanUse(out act, CanUseOption.MustUse)) return true;
            }

            if (VorpalThrust.CanUse(out act)) return true;
            if (TrueThrust.CanUse(out act)) return true;

            if (PiercingTalon.CanUse(out act)) return true;



            act = null;
            return false;
        }

        protected override bool EmergencyGCD(out IAction act)
        {
            /*
             * EmergencyGCD is ran first, ref RotationSolver.Basic.CustomRotation, IAction GCD method
             * so we just run this here.
             */
            HandleOpenerAvailability();

            /*
             * We are now telling every other function that the Opener
             * can and is being run.
             */
            if (Level >= 88
                && Configs.GetBool("DRG_OpenerAt88")
                && IsOpenerAvailable)
            {
                IsCurrentlyInOpener = true;
            }

            return base.EmergencyGCD(out act);
        }

        #region GCD Extras
        //For some gcds that moving forward.
        [RotationDesc("Optional description for Moving Forward GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool MoveForwardGCD(out IAction act)
        {
            return base.MoveForwardGCD(out act);
        }

        [RotationDesc("Optional description for Defense Area GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool DefenseAreaGCD(out IAction act)
        {
            return base.DefenseAreaGCD(out act);
        }

        [RotationDesc("Optional description for Defense Single GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool DefenseSingleGCD(out IAction act)
        {
            return base.DefenseSingleGCD(out act);
        }

        [RotationDesc("Optional description for Healing Area GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool HealAreaGCD(out IAction act)
        {
            return base.HealAreaGCD(out act);
        }

        [RotationDesc("Optional description for Healing Single GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool HealSingleGCD(out IAction act)
        {
            return base.HealSingleGCD(out act);
        }
        #endregion
        #endregion

        #region 0GCD actions
        protected override bool AttackAbility(out IAction act)
        {
            if (Level >= 88
                && Configs.GetBool("DRG_OpenerAt88")
                && IsCurrentlyInOpener)
            {
                return AttackAbilityOpener(out act);
            }

            if (Nastrond.CanUse(out act, CanUseOption.MustUse)) return true;

            if (CanUseAbilitySafely())
            {
                if (RecordActions[0].Action.ActionCategory.Value.Name != "Ability"
                    && StarDiver.CanUse(out act, CanUseOption.MustUse)) return true;
            }

            if (HandlePowerSurge()
                && CanUseAbilitySafely())
            {
                if (WyrmwindThrust.CanUse(out act, CanUseOption.MustUse)) return true;
                if (HandleJumps(out act)) return true;

                if (!IsLastAction(false, HighJump)
                    && MirageDive.CanUse(out act, CanUseOption.MustUse)) return true;
            }

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
            if (Level >= 88
                && Configs.GetBool("DRG_OpenerAt88")
                && IsCurrentlyInOpener)
            {
                return EmergencyAbilityOpener(nextGCD, out act);
            }

            if (nextGCD is BaseAction action && InCombat && CanUseAbilitySafely())
            {
                // We don't want Geirskogul to run without Lance Charge if we're
                // close to going into Life of the Dragon.
                if (EyeCount == 2
                    && PStatus(StatusID.LanceCharge)
                    && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // If we have less than 2 eyes open (0 or 1), feel free to spam Geirskogul.
                if (EyeCount < 2
                    && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                if (InBurst)
                {
                    if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                    if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                    if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                }

                // Make sure Heavens' Thrust gets buffed
                if (nextGCD.IsTheSameTo(true, FullThrust)
                    && LifeSurge.CanUse(out act, CanUseOption.MustUseEmpty)) return true;

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
            if (Level >= 88
                && Configs.GetBool("DRG_OpenerAt88")
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

        #region 0GCD Extras
        //Some 0gcds that moving forward. In general, it doesn't need to be override.
        [RotationDesc(ActionID.SpineShatterDive, ActionID.DragonFireDive)]
        protected override bool MoveForwardAbility(out IAction act)
        {
            if (SpineShatterDive.CanUse(out act)) return true;
            if (DragonFireDive.CanUse(out act, CanUseOption.MustUse)) return true;

            return false;
        }

        //Some 0gcds that moving back. In general, it doesn't need to be override.
        [RotationDesc("Optional description for Moving Back 0GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool MoveBackAbility(out IAction act)
        {
            return base.MoveBackAbility(out act);
        }

        //Some 0gcds that defense area.
        //[RotationDesc("Optional description for Defense Area 0GCD")]
        //[RotationDesc(ActionID.None)]
        //protected override bool DefenseAreaAbility(out IAction act)
        //{
        //    return base.DefenseAreaAbility(out act);
        //}

        //Some 0gcds that defense single.
        [RotationDesc("Optional description for Defense Single 0GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool DefenseSingleAbility(out IAction act)
        {
            return base.DefenseSingleAbility(out act);
        }

        //Some 0gcds that healing area.
        [RotationDesc("Optional description for Healing Area 0GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool HealAreaAbility(out IAction act)
        {
            return base.HealAreaAbility(out act);
        }

        //Some 0gcds that healing single.
        [RotationDesc("Optional description for Healing Single 0GCD")]
        [RotationDesc(ActionID.None)]
        protected override bool HealSingleAbility(out IAction act)
        {
            return base.HealSingleAbility(out act);
        }
        #endregion
        #endregion
    }
}