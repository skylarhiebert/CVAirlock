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
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================

        public Program()
        {

            // The constructor, called only once every session and 
            // always before any other method is called. Use it to 
            // initialize your script.  
            //      
            // The constructor is optional and can be removed if not 
            // needed.

        }

        public void Save()
        {

            // Called when the program needs to save its state. Use 
            // this method to save your state to the Storage field 
            // or some other means.  
            //  
            // This method is optional and can be removed if not 
            // needed.

        }

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
                        AirLockPressurized(airlock);
                        break;
                    case "depressurize":
                        AirLockDepressurized(airlock);
                        break;
                    case "pressurized":
                        PressurizeAirlock(airlock);
                        break;
                    case "depressurized":
                        DepressurizeAirlock(airlock);
                        break;
                    default:
                        // Do the auto-detect
                        break;
                }
            }
        }

        public class CVAirlock
        {
            private IMyGridTerminalSystem _grid;
            string Airlock;
            List<IMyAirVent> ControlAirVents = new List<IMyAirVent>();

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
                IMyBlockGroup controlBg = _grid.GetBlockGroupWithName("Airlock Control: " + airlock);
                IMyBlockGroup innerBg = _grid.GetBlockGroupWithName("Airlock Inner: " + airlock);
                IMyBlockGroup outerBg = _grid.GetBlockGroupWithName("Airlock Outer: " + airlock);

                controlBg.GetBlocksOfType<IMyAirVent>(ControlAirVents);

                innerBg.GetBlocksOfType<IMyDoor>(InnerDoors);
                innerBg.GetBlocksOfType<IMySoundBlock>(InnerSoundBlocks);
                innerBg.GetBlocksOfType<IMyTextPanel>(InnerTextPanels);
                innerBg.GetBlocksOfType<IMyLightingBlock>(InnerLights);

                outerBg.GetBlocksOfType<IMyDoor>(OuterDoors);
                outerBg.GetBlocksOfType<IMySoundBlock>(OuterSoundBlocks);
                outerBg.GetBlocksOfType<IMyTextPanel>(OuterTextPanels);
                outerBg.GetBlocksOfType<IMyLightingBlock>(OuterLights);
            }

            public void Pressurize()
            {
                if(ControlAirVents.All(vent => vent.CustomData == "pressurized"))
                {
                    Pressurized();
                    return;
                }

                UnlockDoors(InnerDoors);
                PrepareAirlock();

                // Set AirVent to Pressurize, set CustomData to Pressurizing
                DepressurizeAirVents(ControlAirVents, false);

            }

            public void Pressurized()
            {
                foreach (IMyAirVent vent in ControlAirVents)
                {
                    vent.CustomData = "pressurized";
                }

                DisableSiren(OuterSoundBlocks.Concat(InnerSoundBlocks));
                SetLightProperties(OuterLights, Color.Green, 1f, 0f, 0f, 0f);
                SetLightProperties(InnerLights, Color.Orange, 1f, 0f, 0f, 0f);

                LockDoors(OuterDoors);
                OpenDoors(InnerDoors);
                // Start Timer
            }

            public void Depressurize()
            {
                if (ControlAirVents.All(vent => vent.CustomData == "depressurized"))
                {
                    Pressurized();
                    return;
                }

                UnlockDoors(OuterDoors);
                PrepareAirlock();

                // Set AirVent to Pressurize, set CustomData to Pressurizing
                DepressurizeAirVents(ControlAirVents, true);
            }

            public void Depressurized()
            {
                foreach(IMyAirVent vent in ControlAirVents)
                {
                    vent.CustomData = "depressurized";
                }

                DisableSiren(OuterSoundBlocks.Concat(InnerSoundBlocks));
                SetLightProperties(OuterLights, Color.Green, 1f, 0f, 0f, 0f);
                SetLightProperties(InnerLights, Color.Orange, 1f, 0f, 0f, 0f);

                LockDoors(InnerDoors);
                OpenDoors(OuterDoors);
                // Start Timer
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
                    vent.CustomData = depressurize ? "depressurizing" : "pressurizing";
                }
            }
        }

        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}