namespace Melee
{
    [RotationDesc("Burst Description here", ActionID.None /* Some actions you used in burst. */)]
    [LinkDescription("$Your link description here, it is better to link to a png! this attribute can be multiple! $")]
    [SourceCode("$If your rotation is open source, please write the link to this attribute! $")]

    // Change this base class to your job's base class. It is named like XXX_Base.
    internal class DRG_Highwind : DRG_Base
    {
        //Change this to the game version right now.
        public override string GameVersion => "6.4";
        public override string RotationName => "Riot's Highwind";
        public override string Description => "A rotation with a dynamic opener following The Balance conventions.";

        private bool IsOpenerAvailable { get; set; }
        private bool IsCurrentlyInOpener { get; set; }
        private bool ShouldEndOpener { get; set; }
        private bool GoThroughFirstPath { get; set; } = true;

        //Extra configurations you want to show on your rotation config.
        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                .SetBool("DRG_DynamicOpeners", false, "Use a dynamic opener from Lvl 50 to 90 instead of off-cd casts. Note: This is controlled with burst! Macro it for full control.")
                .SetBool("DRG_DragonFireInBurst", false, "Only use Dragonfire Dive when in burst");
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
            try
            {
                if (RecordActions.Length <= 1 ||
                    (ActionCate)RecordActions[0].Action.ActionCategory.Value.RowId != ActionCate.Ability ||
                    (ActionCate)RecordActions[1].Action.ActionCategory.Value.RowId != ActionCate.Ability)
                {
                    // Last two actions were not just 0GCDs, safe to use ability
                    return true;
                }

                if (RecordActions[1].UsedTime.AddSeconds(2) < DateTime.Now)
                {
                    // Ability usage safe because 2 seconds have elapsed since second-to-last action. 
                    // Pause in combat most likely.
                    return true;
                }
                else
                {
                    // Ability usage unsafe. Not enough time elapsed.
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                // We don't mind this error as it is a threading issue where the collection will be changed.
                // Does not cause any issues, and we don't want to spam the log.
                // We should always properly throw any exceptions, except this time.
                // For now, return false.
                return false;
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
                bool dragonFireInBurst = Configs.GetBool("DRG_DragonFireInBurst");

                if (LazyJump(out action)) return true;
                if (!dragonFireInBurst
                    || dragonFireInBurst && InBurst && Player.HasStatus(true, StatusID.LanceCharge))
                {
                    if (DragonFireDive.CanUse(out action, CanUseOption.MustUse)) return true;
                }
                if (SpineShatterDive.CanUse(out action, CanUseOption.MustUseEmpty)) return true;
            }

            action = null;
            return false;
        }

        #region GCD actions
        protected override bool GeneralGCD(out IAction act)
        {
            // Remove this?
            if (!InCombat)
            {
                GoThroughFirstPath = true;
            }

            // These will only run if AoE is enabled.
            // Also, Dragoon AOEs only run if there are 3 or more mobs nearby.
            if (!IsCurrentlyInOpener)
            {
                if (CoerthanTorment.CanUse(out act)) return true;
                if (SonicThrust.CanUse(out act)) return true;
                if (DoomSpike.CanUse(out act)) return true;
            }

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

            if (FullThrust.CanUse(out act)) return true;
            if (ChaosThrust.CanUse(out act)) return true;
            if (TrueThrust.CanUse(out act)) return true;

            if (!IsCurrentlyInOpener)
            {
                if (PiercingTalon.CanUse(out act)) return true;
            }

            act = null;
            return false;
        }

        //For some gcds very important, even more than healing, defense, interrupt, etc.
        protected override bool EmergencyGCD(out IAction act)
        {
            return base.EmergencyGCD(out act);
        }
        #endregion

        #region 0GCD actions
        protected override bool AttackAbility(out IAction act)
        {
            if (IsCurrentlyInOpener)
            {
                return AttackAbilityOpener(out act);
            }

            // Use Star Diver when possible
            if (CanUseAbilitySafely() && RecordActions[0].Action.ActionCategory.Value.Name != "Ability"
                && StarDiver.CanUse(out act, CanUseOption.MustUse)) return true;

            if (WyrmwindThrust.CanUse(out act, CanUseOption.MustUse)) return true;
            if (HandleJumps(out act)) return true;
            if (!IsLastAction(false, HighJump) && MirageDive.CanUse(out act, CanUseOption.MustUse)) return true;

            act = null;
            return false;
        }

        private bool AttackAbilityOpener(out IAction act)
        {
            if (Level == 90 && IsLastGCD(ActionID.RaidenThrust) && WyrmwindThrust.CanUse(out act, CanUseOption.MustUse))
            {
                ShouldEndOpener = true;
                return true;
            }

            if (Geirskogul.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, WheelingThrust)) return true;
            }

            if (Level >= 74 && HighJump.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, FangandClaw)) return true;
            }

            if (Level < 74 && Jump.CanUse(out act))
            {
                if (Level >= 56)
                {
                    if (Level >= 64 && IsLastGCD(false, FangandClaw)) return true;
                    if (Level < 64 && IsLastGCD(false, WheelingThrust)) return true;
                }
                if (Level < 56 && IsLastGCD(false, ChaosThrust)) return true;
                if (Level < 50 && IsLastGCD(false, Disembowel)) return true;
            }

            if (DragonFireDive.CanUse(out act, CanUseOption.MustUse))
            {
                if (Level >= 76 && IsLastGCD(ActionID.RaidenThrust)) return true;
                if (Level >= 50 && IsLastGCD(false, TrueThrust) && IsLastAbility(false, Jump)) return true;
            }

            if (MirageDive.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, VorpalThrust)) return true;
            }

            if (SpineShatterDive.CanUse(out act, CanUseOption.MustUse | CanUseOption.MustUseEmpty))
            {
                if (Level >= 50)
                {
                    if (Level >= 86 && IsLastGCD(true, FullThrust)) return true;
                    if (Level >= 84 && IsLastGCD(false, FangandClaw) && IsLastAbility(false, SpineShatterDive))
                    {
                        ShouldEndOpener = true;
                        return true;
                    }
                    if (IsLastGCD(false, FullThrust))
                    {
                        ShouldEndOpener = true;
                        return true;
                    }
                }
            }

            if (ShouldEndOpener)
            {
                IsCurrentlyInOpener = false;
            }

            act = null;
            return false;
        }

        /*
        private bool AttackAbilityOpener(out IAction act)
        {
            if (Level == 90 && IsLastGCD(ActionID.RaidenThrust) && WyrmwindThrust.CanUse(out act, CanUseOption.MustUse))
            {
                ShouldEndOpener = true;
                return true;
            }

            if (Geirskogul.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, WheelingThrust)) return true;
            }

            if (Level >= 74 && HighJump.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, FangandClaw)) return true;
            }

            if (Level < 74 && Jump.CanUse(out act))
            {
                if (Level >= 56)
                {
                    if (Level >= 64
                        && IsLastGCD(false, FangandClaw)) return true;

                    if (Level < 64
                        && IsLastGCD(false, WheelingThrust)) return true;
                }

                if (Level < 56
                    && IsLastGCD(false, ChaosThrust)) return true;

                if (Level < 50
                    && IsLastGCD(false, Disembowel)) return true;
            }

            if (DragonFireDive.CanUse(out act, CanUseOption.MustUse))
            {
                if (Level >= 76
                    && IsLastGCD(ActionID.RaidenThrust))
                {
                    return true;
                }

                if (Level >= 50
                    && IsLastGCD(false, TrueThrust)
                    && IsLastAbility(false, Jump))
                {
                    return true;
                }
            }

            if (MirageDive.CanUse(out act, CanUseOption.MustUse))
            {
                if (IsLastGCD(false, VorpalThrust)) return true;
            }

            if (SpineShatterDive.CanUse(out act, CanUseOption.MustUse | CanUseOption.MustUseEmpty))
            {
                if (Level >= 50)
                {
                    if (Level >= 86)
                    {
                        if (IsLastGCD(true, FullThrust)) return true;
                    }

                    if (Level >= 84)
                    {
                        if (IsLastGCD(false, FangandClaw)
                            && IsLastAbility(false, SpineShatterDive))
                        {
                            ShouldEndOpener = true;
                            return true;
                        }
                    }

                    if (IsLastGCD(false, FullThrust))
                    {
                        ShouldEndOpener = true;
                        return true;
                    }
                }
            }

            if (ShouldEndOpener)
            {
                IsCurrentlyInOpener = false;
            }

            act = null;
            return false;
        }
        */

        //For some 0gcds very important, even more than healing, defense, interrupt, etc.
        protected override bool EmergencyAbility(IAction nextGCD, out IAction act)
        {
            if (IsCurrentlyInOpener)
            {
                return EmergencyAbilityOpener(nextGCD, out act);
            }

            if (nextGCD is BaseAction action && InCombat && CanUseAbilitySafely())
            {
                // Use Nastrond if possible
                if (Nastrond.CanUse(out act, CanUseOption.MustUse)) return true;

                // Check for Geirskogul with Lance Charge
                if (EyeCount == 2 && Player.HasStatus(true, StatusID.LanceCharge) && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // Check for Geirskogul without Lance Charge
                if (EyeCount < 2 && Geirskogul.CanUse(out act, CanUseOption.MustUse)) return true;

                // Use buffs in burst phase
                if (InBurst)
                {
                    if (DragonSight.CanUse(out act, CanUseOption.MustUse)) return true;
                    if (BattleLitany.CanUse(out act, CanUseOption.MustUse)) return true;
                    if (LanceCharge.CanUse(out act, CanUseOption.MustUse)) return true;
                    if (Configs.GetBool("DRG_DragonFireInBurst") && Player.HasStatus(true, StatusID.LanceCharge))
                    {
                        if (HandleJumps(out act)) return true;
                    }
                }

                // Buff Heavens' Thrust
                if (nextGCD.IsTheSameTo(true, FullThrust) && LifeSurge.CanUse(out act, CanUseOption.MustUseEmpty)) return true;

                // Buff 5th hit with Life Surge only if inside positional or have True North
                if (action.EnemyPositional != EnemyPositional.None && action.Target != null && (Player.HasStatus(true, StatusID.TrueNorth) || action.Target.HasPositional()))
                {
                    if (IsLastGCD(false, WheelingThrust) && nextGCD.IsTheSameTo(false, FangandClaw) || IsLastGCD(false, FangandClaw) && nextGCD.IsTheSameTo(false, WheelingThrust))
                    {
                        if (LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
                    }
                }
            }

            return base.EmergencyAbility(nextGCD, out act);
        }

        private bool EmergencyAbilityOpener(IAction nextGCD, out IAction act)
        {
            if (IsLastGCD(false, TrueThrust) && UseBurstMedicine(out act)) return true;

            // Buffs from Chaotic Spring through Wheeling Thrust #2
            if (nextGCD.IsTheSameTo(true, ChaosThrust) || nextGCD.IsTheSameTo(false, ChaosThrust))
            {
                if (LanceCharge.CanUse(out act, CanUseOption.EmptyOrSkipCombo) || DragonSight.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buffs from Wheeling Thrust #1 through Spineshatter Dive #2
            if (nextGCD.IsTheSameTo(false, WheelingThrust) || (Level < 58 && nextGCD.IsTheSameTo(false, ChaosThrust)))
            {
                if (BattleLitany.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buffs Fang and Claw in 0GCD position 2
            if (Level >= 60 && nextGCD.IsTheSameTo(false, FangandClaw) && IsLastAbility(false, Geirskogul) ||
                (Level < 60 && nextGCD.IsTheSameTo(false, FangandClaw)) || (Level < 56 && nextGCD.IsTheSameTo(false, FullThrust)))
            {
                if (LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;
            }

            // Buffs Heavens' Thrust in 0GCD position 1
            if (nextGCD.IsTheSameTo(true, FullThrust) && LifeSurge.CanUse(out act, CanUseOption.EmptyOrSkipCombo)) return true;

            act = null;
            return false;
        }

        //Some 0gcds that don't need to a hostile target in attack range.
        protected override bool GeneralAbility(out IAction act)
        {
            return base.GeneralAbility(out act);
        }

        #endregion

        #region Extra
        //For counting down action when pary counting down is active.
        protected override IAction CountDownAction(float remainTime)
        {
            return base.CountDownAction(remainTime);
        }

        //This is the method to update all field you wrote, it is used first during one frame.
        protected override void UpdateInfo()
        {
            if (Configs.GetBool("DRG_DynamicOpeners") && Level >= 50 && InBurst)
            {
                HandleOpenerAvailability();

                if (IsOpenerAvailable)
                {
                    IsCurrentlyInOpener = true;
                    ShouldEndOpener = false;
                }
            }
            else
            {
                IsOpenerAvailable = false;
                IsCurrentlyInOpener = false;
                ShouldEndOpener = false;
            }

            if (Player.IsDead)
            {
                GoThroughFirstPath = true;
            }

            base.UpdateInfo();
        }

        //This method is used when player change the terriroty, such as go into one duty, you can use it to set the field.
        public override void OnTerritoryChanged()
        {
            GoThroughFirstPath = true;
            base.OnTerritoryChanged();
        }

        //This method is used to debug. If you want to show some information in Debug panel, show something here.
        public override void DisplayStatus()
        {
            base.DisplayStatus();
        }
        #endregion

        private void HandleOpenerAvailability()
        {
            bool hasDragonSight = DragonSight.CanUse(out _, CanUseOption.IgnoreClippingCheck);
            bool hasBattleLitany = BattleLitany.CanUse(out _, CanUseOption.IgnoreClippingCheck);
            bool hasLanceCharge = LanceCharge.CanUse(out _, CanUseOption.IgnoreClippingCheck);

            if (Level >= 88)
            {
                IsOpenerAvailable = hasDragonSight && hasBattleLitany && hasLanceCharge;
                return;
            }
            else if (Level >= 66)
            {
                IsOpenerAvailable = hasDragonSight && hasBattleLitany && hasLanceCharge;
                return;
            }
            else if (Level >= 52)
            {
                IsOpenerAvailable = hasBattleLitany && hasLanceCharge;
                return;
            }
            else if (Level >= 30)
            {
                IsOpenerAvailable = hasLanceCharge;
                return;
            }
            else
            {
                IsOpenerAvailable = false;
                return;
            }
        }
    }
}