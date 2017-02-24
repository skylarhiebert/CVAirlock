using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        public void Main(string argument)
        {
            // The main entry point of the script, invoked every time 
            // one of the programmable block's Run actions are invoked. 
            //  
            // The method itself is required, but the argument above 
            // can be removed if not needed.

            if (argument == null || argument == "")
            {
                // Run Update
                // MUST HAVE AIRLOCK
            }
            else
            {
                string[] arguments = argument.Split(':');
                var airlock = arguments.ElementAtOrDefault(0);
                var command = arguments.ElementAtOrDefault(1);

                CVAirlock al = new CVAirlock(GridTerminalSystem, airlock);

                Echo("Running " + command + " on " + airlock);
                switch (command.ToLower())
                {
                    case "pressurize":
                        al.Pressurize();
                        break;
                    case "depressurize":
                        al.Depressurize();
                        break;
                    case "pressurized":
                        al.Pressurized();
                        break;
                    case "depressurized":
                        al.Depressurized();
                        break;
                    default:
                        al.Update();
                        break;
                }
            }
        }

        public class CVAirlock
        {
            public static int DOOR_DELAY = 10;
            public static int VENT_DELAY = 5;
            public static string PRESSURIZED = "pressurized";
            public static string DEPRESSURIZED = "depressurized";
            public static string PRESSURIZING = "pressurizing";
            public static string DEPRESSURIZING = "depressurizing";

            private IMyGridTerminalSystem _grid;
            string Airlock;
            List<IMyAirVent> ControlAirVents = new List<IMyAirVent>();
            IMyTimerBlock timer = null;

            List<IMyDoor> InnerDoors = new List<IMyDoor>();
            List<IMySoundBlock> InnerSoundBlocks = new List<IMySoundBlock>();
            List<IMyTextPanel> InnerTextPanels = new List<IMyTextPanel>();
            List<IMyLightingBlock> InnerLights = new List<IMyLightingBlock>();

            List<IMyDoor> OuterDoors = new List<IMyDoor>();
            List<IMySoundBlock> OuterSoundBlocks = new List<IMySoundBlock>();
            List<IMyTextPanel> OuterTextPanels = new List<IMyTextPanel>();
            List<IMyLightingBlock> OuterLights = new List<IMyLightingBlock>();

            public CVAirlock(IMyGridTerminalSystem GridTerminalSystem, string airlock)
            {
                _grid = GridTerminalSystem;
                Airlock = airlock;
                IMyBlockGroup controlBg = _grid.GetBlockGroupWithName("Airlock Control: " + Airlock);
                IMyBlockGroup innerBg = _grid.GetBlockGroupWithName("Airlock Inner: " + Airlock);
                IMyBlockGroup outerBg = _grid.GetBlockGroupWithName("Airlock Outer: " + Airlock);

                controlBg.GetBlocksOfType<IMyAirVent>(ControlAirVents);

                List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
                controlBg.GetBlocksOfType<IMyTimerBlock>(timers);
                timer = timers.FirstOrDefault();

                innerBg.GetBlocksOfType<IMyDoor>(InnerDoors);
                innerBg.GetBlocksOfType<IMySoundBlock>(InnerSoundBlocks);
                innerBg.GetBlocksOfType<IMyTextPanel>(InnerTextPanels);
                innerBg.GetBlocksOfType<IMyLightingBlock>(InnerLights);

                outerBg.GetBlocksOfType<IMyDoor>(OuterDoors);
                outerBg.GetBlocksOfType<IMySoundBlock>(OuterSoundBlocks);
                outerBg.GetBlocksOfType<IMyTextPanel>(OuterTextPanels);
                outerBg.GetBlocksOfType<IMyLightingBlock>(OuterLights);
            }

            public void Update()
            {
                if (ControlAirVents.Any(vent => vent.CustomData == PRESSURIZING && vent.GetOxygenLevel() > 0.9))
                {
                    Pressurized();
                } else if (ControlAirVents.Any(vent => vent.CustomData == PRESSURIZED && vent.GetOxygenLevel() > 0.9))
                {
                    Depressurize();
                } else if (ControlAirVents.Any(vent => vent.CustomData == DEPRESSURIZING && vent.GetOxygenLevel() < 0.1))
                {
                    Depressurized();
                } else if (ControlAirVents.Any(vent => vent.CustomData == DEPRESSURIZED && vent.GetOxygenLevel() < 0.1))
                {
                    Pressurize();
                }
            }

            public void Pressurize()
            {
                if (ControlAirVents.All(vent => vent.CustomData == PRESSURIZED))
                {
                    Pressurized();
                    return;
                }

                UnlockDoors(InnerDoors);
                PrepareAirlock();

                // Set AirVent to Pressurize, set CustomData to Pressurizing
                DepressurizeAirVents(ControlAirVents, false);

                timer.TriggerDelay = VENT_DELAY;
                timer.StartCountdown();
            }

            public void Pressurized()
            {
                foreach (IMyAirVent vent in ControlAirVents)
                {
                    vent.CustomData = PRESSURIZED;
                }

                DisableSiren(OuterSoundBlocks.Concat(InnerSoundBlocks));
                SetLightProperties(OuterLights, Color.Green, 1f, 0f, 0f, 0f);
                SetLightProperties(InnerLights, Color.Orange, 1f, 0f, 0f, 0f);

                LockDoors(OuterDoors);
                OpenDoors(InnerDoors);

                timer.TriggerDelay = DOOR_DELAY;
                timer.StartCountdown();
            }

            public void Depressurize()
            {
                if (ControlAirVents.All(vent => vent.CustomData == DEPRESSURIZED))
                {
                    Depressurized();
                    return;
                }

                UnlockDoors(OuterDoors);
                PrepareAirlock();

                // Set AirVent to Pressurize, set CustomData to Pressurizing
                DepressurizeAirVents(ControlAirVents, true);

                timer.TriggerDelay = VENT_DELAY;
                timer.StartCountdown();
            }

            public void Depressurized()
            {
                foreach (IMyAirVent vent in ControlAirVents)
                {
                    vent.CustomData = DEPRESSURIZED;
                }

                DisableSiren(OuterSoundBlocks.Concat(InnerSoundBlocks));
                SetLightProperties(OuterLights, Color.Green, 1f, 0f, 0f, 0f);
                SetLightProperties(InnerLights, Color.Orange, 1f, 0f, 0f, 0f);

                LockDoors(InnerDoors);
                OpenDoors(OuterDoors);

                timer.TriggerDelay = DOOR_DELAY;
                timer.StartCountdown();
            }

            public void ToggleAirlock()
            {
                //if (AirVents.Any(vent => vent.Depressurize) && AirVents.Any(vent => vent.CustomData.ToLower() == "depressurized"))
                //{
                //    var bg = GridTerminalSystem.GetBlockGroupWithName("foo");
                //    bg.GetBlocksOfType<IMyDoor>(InnerDoors, delegate (IMyTerminalBlock block) { return (block.)})

                //}
            }

            public void PrepareAirlock()
            {
                // Close all doors                
                CloseDoors(OuterDoors.Concat(InnerDoors));

                // Enable Siren
                EnableSiren(OuterSoundBlocks);

                // Set Warning Lights to COLOR
                SetLightProperties(OuterLights.Concat(InnerLights), Color.Red, 1f, 1f, 0f, 0f);
            }

            public void UnlockDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    door.ApplyAction("OnOff_On");
                }
            }

            public void LockDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    //Echo("Closing door: " + door.Name);
                    door.ApplyAction("OnOff_Off");
                }
            }

            public void OpenDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    door.ApplyAction("Open_On");
                }
            }

            public void CloseDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    door.ApplyAction("Open_Off");
                }
            }

            public void EnableSiren(IEnumerable<IMySoundBlock> soundBlocks)
            {
                foreach (IMySoundBlock soundBlock in soundBlocks)
                {
                    if (!soundBlock.IsSoundSelected)
                    {
                        soundBlock.SelectedSound = "warning 2";
                    }

                    soundBlock.ApplyAction("PlaySound");
                }
            }

            public void DisableSiren(IEnumerable<IMySoundBlock> soundBlocks)
            {
                foreach (IMySoundBlock soundBlock in soundBlocks)
                {
                    soundBlock.ApplyAction("PlaySound");
                }
            }

            public void SetLightProperties(IEnumerable<IMyLightingBlock> lights, Color color, float intensity, float blinkInterval, float blinkLength, float blinkOffset)
            {
                foreach (IMyLightingBlock light in lights)
                {
                    light.Color = color;
                    light.Intensity = intensity;
                    light.BlinkIntervalSeconds = blinkInterval;
                    light.BlinkLength = blinkLength;
                    light.BlinkOffset = blinkOffset;
                }
            }
            public void SetLightColor(IEnumerable<IMyLightingBlock> lights, Color color)
            {
                foreach (IMyLightingBlock light in lights)
                {
                    light.SetValue("Color", color);
                }
            }

            public void DepressurizeAirVents(IEnumerable<IMyAirVent> vents, bool depressurize)
            {
                foreach (IMyAirVent vent in vents)
                {
                    vent.Depressurize = depressurize;
                    vent.CustomData = depressurize ? DEPRESSURIZING : PRESSURIZING;
                }
            }
        }

        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}