namespace Melee
{
    [SourceCode("https://github.com/RiotNOR/CustomRotations/blob/main/RiotsRotations/Melee/DRG_Highwind.cs")]
    internal class DRG_Highwind : DRG_Base
    {
        public override string GameVersion => "6.38";

        public override string RotationName => "Riot's Highwind";

        public override string Description => "A rotation with an opener following The Balance, and more.";

        protected override IRotationConfigSet CreateConfiguration()
        {
            return base.CreateConfiguration()
                .SetBool("DRG_OpenerAt90", false, "Use Lvl. 90 opener (ignores other settings during the opener itself)")
                .SetBool("DRG_KeepBuffsAligned", false, "Try to keep buffs aligned with Geirskogul in case of drifting. Statically 6 second remaining on GSK for alignment.")
                .SetBool("DRG_LanceChargeFirst", false, "Move Lance Charge to in front of True Thrust for opener. Not entirely compatible with the above setting.");
        }

        protected override bool AttackAbility(byte abilitiesRemaining, out IAction act)
        {
            act = null;
            return false;
        }

        protected override bool GeneralGCD(out IAction act)
        {
            act = null;
            return false;
        }
    }
}