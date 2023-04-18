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
            return base.CreateConfiguration();
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