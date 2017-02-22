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
            string[] arguments = argument.Split(':');
            var airlock = arguments.ElementAtOrDefault(0);
            var command = arguments.ElementAtOrDefault(1);

            CVAirlock al = new CVAirlock(airlock);

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

        public class CVAirlock
        {
            string Airlock;
            List<IMyAirVent> AirVents = new List<IMyAirVent>();

            List<IMyDoor> InnerDoors = new List<IMyDoor>();
            List<IMySoundBlock> InnerSoundBlocks = new List<IMySoundBlock>();
            List<IMyTextPanel> InnerTextPanels = new List<IMyTextPanel>();
            List<IMyInteriorLight> InnerLights = new List<IMyInteriorLight>();

            List<IMyDoor> OuterDoors = new List<IMyDoor>();
            List<IMySoundBlock> OuterSoundBlocks = new List<IMySoundBlock>();
            List<IMyTextPanel> OuterTextPanels = new List<IMyTextPanel>();
            List<IMyInteriorLight> OuterLights = new List<IMyInteriorLight>();

            public CVAirlock(string airlock)
            {
                Airlock = airlock;
            }

            public void ToggleAirlock()
            {
                if (AirVents.Any(vent => vent.Depressurize) && AirVents.Any(vent => vent.CustomData.ToLower() == "depressurized") )
                {

                }
            }
            
        }

        public void CloseDoors(List<IMyTerminalBlock> doors)
        {
            Echo("Closing " + doors.Count + " Doors");
            for (int i = 0; i < doors.Count; i++)
            {
                IMyDoor door = doors[i] as IMyDoor;
                door.ApplyAction("Open_Off");
            }
        }

        public void OpenDoors(List<IMyTerminalBlock> doors)
        {
            Echo("Opening " + doors.Count + " Doors");
            for (int i = 0; i < doors.Count; i++)
            {
                IMyDoor door = doors[i] as IMyDoor;
                door.GetActionWithName("Open_On").Apply(door);
            }
        }

        public void LockDoors(List<IMyTerminalBlock> doors)
        {
            Echo("Locking " + doors.Count + " Doors");
            for (int i = 0; i < doors.Count; i++)
            {
                IMyDoor door = doors[i] as IMyDoor;
                door.ApplyAction("OnOff_Off");
            }
        }

        public void UnlockDoors(List<IMyTerminalBlock> doors)
        {
            Echo("Unlocking " + doors.Count + " Doors");
            for (int i = 0; i < doors.Count; i++)
            {
                IMyDoor door = doors[i] as IMyDoor;
                door.ApplyAction("OnOff_On");
            }
        }

        public void SetLightColor(List<IMyTerminalBlock> lights, Color color)
        {
            Echo("Setting " + lights.Count + " Lights to " + color);
            for (int i = 0; i < lights.Count; i++)
            {
                IMyInteriorLight light = lights[i] as IMyInteriorLight;
                light.SetValue("Color", color);
            }
        }

        public void PressurizeAirlock(string airlock)
        {
            Echo("Pressurizing Airlock " + airlock);
            List<IMyTerminalBlock> ExternalDoors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> InternalDoors = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door Internal", InternalDoors);
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door External", ExternalDoors);

            IMyAirVent AirVent = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Air Vent") as IMyAirVent;

            UnlockDoors(InternalDoors);
            CloseDoors(ExternalDoors);
            CloseDoors(InternalDoors);

            // Sound On
            Echo("Enabling Siren");
            IMySoundBlock warningSiren;
            warningSiren = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Sound Block") as IMySoundBlock;
            if (warningSiren.IsSoundSelected) { warningSiren.ApplyAction("PlaySound"); }
            else { Echo("No Sound Selected"); }

            // Warning Light On
            Echo("Turning on Warning Light");
            IMyInteriorLight warningLight;
            warningLight = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Light Pressurizing") as IMyInteriorLight;
            warningLight.ApplyAction("OnOff_On");

            // All Lights Red
            Echo("Setting all lights to Red");
            List<IMyTerminalBlock> InteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Interior", InteriorLights);
            List<IMyTerminalBlock> ExteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Exterior", ExteriorLights);
            SetLightColor(InteriorLights, Color.Red);
            SetLightColor(ExteriorLights, Color.Red);

            Echo("Setting Airvent to Pressurize");
            AirVent.ApplyAction("Depressurize_Off");
            if (AirVent.GetOxygenLevel() >= 1)
            {
                AirLockPressurized(airlock);
            }
        }

        public void DepressurizeAirlock(string airlock)
        {
            Echo("Depressurizing " + airlock + " Airlock");
            List<IMyTerminalBlock> ExternalDoors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> InternalDoors = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door Internal", InternalDoors);
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door External", ExternalDoors);

            IMyAirVent AirVent = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Air Vent") as IMyAirVent;

            UnlockDoors(ExternalDoors);
            CloseDoors(InternalDoors);
            CloseDoors(ExternalDoors);

            // Sound On 
            Echo("Turning On Warning Siren");
            IMySoundBlock warningSiren;
            warningSiren = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Sound Block") as IMySoundBlock;
            if (warningSiren.IsSoundSelected) { warningSiren.ApplyAction("PlaySound"); }
            else { Echo("No Sound Selected"); }

            // Warning Light On 
            Echo("Turning On Warning Light");
            IMyInteriorLight warningLight;
            warningLight = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Light Pressurizing") as IMyInteriorLight;
            warningLight.ApplyAction("OnOff_On");

            // All Lights Red 
            Echo("Enabling All Lock Lights");
            List<IMyTerminalBlock> InteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Interior", InteriorLights);
            List<IMyTerminalBlock> ExteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Exterior", ExteriorLights);
            SetLightColor(InteriorLights, Color.Red);
            SetLightColor(ExteriorLights, Color.Red);

            Echo("Setting to Depressurize");
            AirVent.ApplyAction("Depressurize_On");
            if (!(AirVent.GetOxygenLevel() > 0))
            {
                AirLockDepressurized(airlock);
            }
        }

        public void AirLockPressurized(string airlock)
        {
            List<IMyTerminalBlock> ExternalDoors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> InternalDoors = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door Internal", InternalDoors);
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door External", ExternalDoors);

            // Sound Off
            IMySoundBlock warningSiren;
            warningSiren = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Sound Block") as IMySoundBlock;
            warningSiren.ApplyAction("StopSound");

            // Warning Light Off
            IMyInteriorLight warningLight;
            warningLight = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Light Pressurizing") as IMyInteriorLight;
            warningLight.ApplyAction("OnOff_Off");

            // Internal Doors Lights Green
            List<IMyTerminalBlock> InteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Interior", InteriorLights);
            SetLightColor(InteriorLights, Color.Green);

            // External Door Lights Red
            List<IMyTerminalBlock> ExteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Exterior", ExteriorLights);
            SetLightColor(ExteriorLights, Color.Red);

            LockDoors(ExternalDoors);
            OpenDoors(InternalDoors);

            IMyTimerBlock timer = GridTerminalSystem.GetBlockWithName("Airlock Timer Block Close") as IMyTimerBlock;
            timer.ApplyAction("Start");
        }

        public void AirLockDepressurized(string airlock)
        {
            List<IMyTerminalBlock> ExternalDoors = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> InternalDoors = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door Internal", InternalDoors);
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Door External", ExternalDoors);

            // Sound Off 
            IMySoundBlock warningSiren;
            warningSiren = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Sound Block") as IMySoundBlock;
            warningSiren.ApplyAction("StopSound");

            // Warning Light Off 
            IMyInteriorLight warningLight;
            warningLight = GridTerminalSystem.GetBlockWithName(airlock + " Airlock Light Pressurizing") as IMyInteriorLight;
            warningLight.ApplyAction("OnOff_Off");

            // Internal Doors Lights Red
            List<IMyTerminalBlock> InteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Interior", InteriorLights);
            SetLightColor(InteriorLights, Color.Red);

            // External Door Lights Green
            List<IMyTerminalBlock> ExteriorLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(airlock + " Airlock Light Exterior", ExteriorLights);
            SetLightColor(ExteriorLights, Color.Green);

            LockDoors(InternalDoors);
            OpenDoors(ExternalDoors);
            IMyTimerBlock timer = GridTerminalSystem.GetBlockWithName("Airlock Timer Block Close") as IMyTimerBlock;
            timer.ApplyAction("Start");
        }

        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}