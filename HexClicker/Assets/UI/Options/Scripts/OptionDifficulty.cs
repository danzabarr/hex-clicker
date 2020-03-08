using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Extreme,
        Fucking_Insane
    }

    public class OptionDifficulty : OptionDropdown<Difficulty>
    {

    }

}
