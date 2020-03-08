using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.UI.Menus
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Extreme
    }

    public class OptionDifficulty : OptionDropdown<Difficulty>
    {

    }

}
