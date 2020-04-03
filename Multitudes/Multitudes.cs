using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
 
namespace Multitudes
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("dev.wildbook.multitudes", "Multitudes", "1.4.0")]
    public class Multitudes : BaseUnityPlugin
    {
        private static ConfigEntry<int> MultiplierConfig { get; set; }
 
        private delegate int RunInstanceReturnInt(Run self);
 
        private static RunInstanceReturnInt origLivingPlayerCountGetter;
        private static RunInstanceReturnInt origParticipatingPlayerCountGetter;
 
        public static int Multiplier
        {
            get => MultiplierConfig.Value;
            protected set => MultiplierConfig.Value = value;
        }
 
        public void Awake()
        {
            MultiplierConfig = Config.Bind(
                "Game",
                "Multiplier",
                4,
                "Sets the multiplier for Multitudes.");
 
            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.AddToConsoleWhenReady();
                orig(self);
            };
 
            var getLivingPlayerCountHook = new Hook(typeof(Run).GetMethodCached("get_livingPlayerCount"),
                typeof(Multitudes).GetMethodCached(nameof(GetLivingPlayerCountHook)));
            origLivingPlayerCountGetter = getLivingPlayerCountHook.GenerateTrampoline<RunInstanceReturnInt>();
 
            var getParticipatingPlayerCount = new Hook(typeof(Run).GetMethodCached("get_participatingPlayerCount"),
                typeof(Multitudes).GetMethodCached(nameof(GetParticipatingPlayerCountHook)));
            origParticipatingPlayerCountGetter = getParticipatingPlayerCount.GenerateTrampoline<RunInstanceReturnInt>();
 
            Run.onRunStartGlobal += run => { SendMultiplierChat(); };
 
            On.RoR2.HoldoutZoneController.CountPlayersInRadius += (orig, origin, chargingRadiusSqr, teamIndex) => orig(origin, chargingRadiusSqr, teamIndex) * Multiplier;
        }
 
        private static int GetLivingPlayerCountHook(Run self) => origLivingPlayerCountGetter(self) * Multiplier;
        private static int GetParticipatingPlayerCountHook(Run self) => origParticipatingPlayerCountGetter(self) * Multiplier;
 
        // Random example command to set multiplier with
        [ConCommand(commandName = "mod_wb_set_multiplier", flags = ConVarFlags.None, helpText = "Lets you pretend to have more friends than you actually do.")]
        private static void CCSetMultiplier(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
 
            if (!int.TryParse(args[0], out var multiplier))
            {
                Debug.Log("Invalid argument.");
            }
            else
            {
                MultiplierConfig.Value = multiplier;
                Debug.Log($"Multiplier set to {MultiplierConfig.Value}. Good luck!");
                SendMultiplierChat();
            }
        }
 
        private static void SendMultiplierChat()
        {
            // If we're not host, we're not setting it for the current lobby
            // That also means no one cares what our Multitudes is set to
            if (!NetworkServer.active)
                return;
 
            Chat.SendBroadcastChat(
                new Chat.SimpleChatMessage
                {
                    baseToken = "<color=lightblue>Multitudes set to: </color> {0}",
                    paramTokens = new[]
                    {
                        MultiplierConfig.Value.ToString()
                    }
                });
        }
 
        // Random example command to set multiplier with
        [ConCommand(commandName = "mod_wb_get_multiplier", flags = ConVarFlags.None, helpText = "Lets you know what Multitudes' multiplier is set to.")]
        private static void CCGetMultiplier(ConCommandArgs args)
        {
            Debug.Log(args.Count != 0
                ? "Invalid arguments. Did you mean mod_wb_set_multiplier?"
                : $"Your multiplier is currently {MultiplierConfig.Value}. Good luck!");
        }
    }
}
