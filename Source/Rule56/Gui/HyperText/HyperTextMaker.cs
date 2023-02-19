namespace CombatAI.Gui
{
    public static class HyperTextMaker
    {
        public static HyperText Make(HyperTextDef def, bool allowScrolling = true)
        {
            HyperText text = new HyperText(def, allowScrolling);
            return text;
        }
    }
}
