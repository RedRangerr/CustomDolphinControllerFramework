﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using CustomDolphinController.Enums;
using CustomDolphinController.Structs;

namespace CustomDolphinController.Example
{
    public class ArduinoController : ControllerBase
    {
        
        private ArduinoInputData _lastArduinoInputData;


        public override bool Initialize()
        {
            new Thread(() =>
            {
                SerialPort port = new SerialPort("COM3", 9600); // replace COM3 with the port name of your Arduino and 9600 with the baud rate you've set on the Arduino

                if (port.IsOpen)
                {
                    Console.WriteLine("Serial Port is Busy.");
                    return;
                }
                
                port.Open();

                Console.WriteLine("Arduino Controller started listening for inputs.");
                try
                {
                    while (true)
                    {
                        string data = port.ReadLine(); // read a line of data from the serial port
                        ArduinoInputData arduinoInputData = ArduinoInputData.ParseInput(data);
                        _lastArduinoInputData = arduinoInputData;
                        Console.WriteLine(_lastArduinoInputData);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    port.Close();
                }
            }).Start();
            return true;
        }


        protected override BatteryStatus GetBatteryStatus()
        {
            return BatteryStatus.Charged;
        }

        protected override ConnectionType GetConnectionType()
        {
            return ConnectionType.USB;
        }

        protected override DeviceModel GetDeviceModel()
        {
            return DeviceModel.NotApplicable;
        }

        protected override SlotState GetSlotState()
        {
            return SlotState.Connected;
        }

        protected override bool IsConnected()
        {
            return GetSlotState() == SlotState.Connected;
        }
        
        public override ActualControllerDataInfo GetActualControllerInfo(uint packetNumber)
        {
            /*
            if (_inputs.TryDequeue(out InputData data))
            {
                Console.WriteLine($"Recieving: {data}");
                _lastInputData = data;
                return GetControllerData(packetNumber, data);
            }
            Console.WriteLine("Could not find any new inputs, returning the last input.");
            */
            return GetControllerData(packetNumber, _lastArduinoInputData);
        }


        private ActualControllerDataInfo GetControllerData(uint packetNumber, ArduinoInputData data)
        {
            return new ActualControllerDataInfo()
            {
                IsConnected = IsConnected(),
                PacketNumber = packetNumber,
                LeftStickX = (byte) ((float)data.x/4),
                LeftStickY = (byte) ((float)data.y/4),
                AnalogA = (byte) (data.buttonAState == 1 ? 255 : 0),
                AnalogB = (byte) (data.buttonBState == 1 ? 255 : 0),
                AnalogL1 = (byte) (data.buttonJState == 1 ? 255 : 0)
            };
        }
    }
    
    public struct ArduinoInputData : IEquatable<ArduinoInputData>
    {
        public int x;
        public int y;
        public int buttonAState;
        public int buttonBState;
        public int buttonJState;
        
        public override string ToString()
        {
            return $"|x = {x}, y = {y}, button_a_state = {buttonAState}, button_b_state = {buttonBState}, button_j_state = {buttonJState}|";
        }
        
        public static ArduinoInputData ParseInput(string input)
        {
            try
            {
                string[] parts = input.Split(',');

                Dictionary<string, int> variables = new Dictionary<string, int>();

                foreach (string part in parts)
                {
                    string[] keyValue = part.Split('=');
                    string variableName = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    if (int.TryParse(value, out int parsedValue))
                    {
                        variables[variableName] = parsedValue;
                    }
                }

                int x = variables["X"];
                int y = variables["Y"];
                int buttonAState = variables["button_A_state"];
                int buttonBState = variables["button_B_state"];
                int buttonJState = variables["button_Joy_state"];

                return new ArduinoInputData { x = x, y = y, buttonAState = buttonAState, buttonBState = buttonBState, buttonJState = buttonJState};
            }
            catch (Exception e)
            {
                //in case the serial port doesn't read every part of the string
                Console.WriteLine($"Arduino input parsing error: {e}, caused by {input}");
                return new ArduinoInputData();
            }
        }

        public bool Equals(ArduinoInputData other)
        {
            return x == other.x && y == other.y && buttonAState == other.buttonAState && buttonJState == other.buttonJState;
        }

        public override bool Equals(object obj)
        {
            return obj is ArduinoInputData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, buttonAState, buttonJState);
        }
    }

}