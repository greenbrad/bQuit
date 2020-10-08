using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace bQuit
{
    class bQuitSettings : ISettings
    {
        [Menu("Debug")]
        public ToggleNode Debug { get; set; }
        [Menu("Enable")]
        public ToggleNode Enable { get; set; }
        [Menu("Force Quit: ")]
        public HotkeyNode forceQuit { get; set; }
        public bQuitSettings()
        {
            Debug = new ToggleNode(false);
            Enable = new ToggleNode(false);
            forceQuit = new HotkeyNode(Keys.F1);
        }
    }
}
