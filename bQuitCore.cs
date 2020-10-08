using System;
using System.Linq;
using System.Runtime.InteropServices;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;

namespace bQuit
{
    class bQuiteCore : BaseSettingsPlugin<bQuitSettings>
    {
        internal Entity localPlayer;
        internal Life player;

        #region Command Handler
        // Taken from ->
        // https://www.reddit.com/r/pathofexiledev/comments/787yq7/c_logout_app_same_method_as_lutbot/
        public static partial class CommandHandler
        {
            public static void KillTCPConnectionForProcess(int ProcessId)
            {
                MibTcprowOwnerPid[] table;
                var afInet = 2;
                var buffSize = 0;
                var ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
                var buffTable = Marshal.AllocHGlobal(buffSize);
                try
                {
                    ret = GetExtendedTcpTable(buffTable, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
                    if (ret != 0)
                        return;
                    var tab = (MibTcptableOwnerPid)Marshal.PtrToStructure(buffTable, typeof(MibTcptableOwnerPid));
                    var rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));
                    table = new MibTcprowOwnerPid[tab.dwNumEntries];
                    for (var i = 0; i < tab.dwNumEntries; i++)
                    {
                        var tcpRow = (MibTcprowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibTcprowOwnerPid));
                        table[i] = tcpRow;
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));

                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffTable);
                }

                //Kill Path Connection
                var PathConnection = table.FirstOrDefault(t => t.owningPid == ProcessId);
                PathConnection.state = 12;
                var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(PathConnection));
                Marshal.StructureToPtr(PathConnection, ptr, false);
                SetTcpEntry(ptr);


            }

            [DllImport("iphlpapi.dll", SetLastError = true)]
            private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TcpTableClass tblClass, uint reserved = 0);

            [DllImport("iphlpapi.dll")]
            private static extern int SetTcpEntry(IntPtr pTcprow);

            [StructLayout(LayoutKind.Sequential)]
            public struct MibTcprowOwnerPid
            {
                public uint state;
                public uint localAddr;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
                public uint remoteAddr;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
                public uint owningPid;

            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MibTcptableOwnerPid
            {
                public uint dwNumEntries;
                private readonly MibTcprowOwnerPid table;
            }

            private enum TcpTableClass
            {
                TcpTableBasicListener,
                TcpTableBasicConnections,
                TcpTableBasicAll,
                TcpTableOwnerPidListener,
                TcpTableOwnerPidConnections,
                TcpTableOwnerPidAll,
                TcpTableOwnerModuleListener,
                TcpTableOwnerModuleConnections,
                TcpTableOwnerModuleAll
            }
        }
        #endregion

        public void Quit()
        {
            try
            {
                CommandHandler.KillTCPConnectionForProcess(GameController.Window.Process.Id);
            }
            catch (Exception e)
            {
                LogError($"{e}");
            }
        }

        public override void Render()
        {

            if (Settings.Enable && (WinApi.GetAsyncKeyState(Settings.forceQuit) & 0x8000) != 0)
            {
                Quit();
            }

            /* Thought flow
             * Check for Chaos Inoculation
             * Check for Low Life Build
             * Otherwise Auto Quit at 40%
             * 
             */

            //if (!GameController.Area.CurrentArea.IsHideout && !GameController.Area.CurrentArea.IsTown && !GameController.IngameState.IngameUi.StashElement.IsVisible &&
                //!GameController.IngameState.IngameUi.NpcDialog.IsVisible && !GameController.IngameState.IngameUi.SellWindow.IsVisible)
            //{

                localPlayer = GameController.Game.IngameState.Data.LocalPlayer;
                player = localPlayer.GetComponent<Life>();

                #region Auto Quit
                if (Settings.Enable)
                {
                    try
                    {
                        GameController.Player.Stats.TryGetValue(GameStat.KeystoneChaosInoculation, out int chaosInoc);
                        GameController.Player.Stats.TryGetValue(GameStat.ChaosDamageDoesNotBypassEnergyShield, out int chaosDoesNotBypass);
                        GameController.Player.Stats.TryGetValue(GameStat.ChaosDamageDoesNotBypassEnergyShieldWhileNotLowLifeOrMana, out int chaosDoesNotBypassWhileNotLowLifeOrMana);

                        float esPercent = player.ESPercentage * 100;
                        float lifePercent = player.HPPercentage * 100;
                        float life = 0;

                        if (chaosInoc == 1 || chaosDoesNotBypass == 1 || chaosDoesNotBypassWhileNotLowLifeOrMana == 1 )
                        {
                            if (Settings.Debug)
                            {
                                LogMessage("Life based on ES.");
                            }
                            life = esPercent;
                        }
                        else
                        {
                            if (Settings.Debug)
                            {
                                LogMessage("Life Based on Life.");
                            }
                            life = lifePercent;
                        }

                        if (life <= 40)
                        {
                            Quit();
                        }
                    //End Try Method
                    }
                    catch (Exception e)
                    {
                        LogError(e.ToString());
                    }
                }
                #endregion
            //}
        }
    }
}
