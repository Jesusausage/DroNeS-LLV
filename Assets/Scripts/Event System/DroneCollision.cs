﻿namespace Drones.EventSystem
{
    using Utils.Extensions;
    using Utils;
    using System;

    public class DroneCollision : IEvent
    {
        public DroneCollision(RetiredDrone a)
        {
            Type = EventType.Collision;
            OpenWindow = a.OpenInfoWindow;
            ID = a.Name;
            Target = a.Location.ToArray();
            Time = TimeKeeper.Chronos.Get();
            if (a.OtherDroneName != null)
            {
                Message = Time + " - " + ID + " collided with " + a.OtherDroneName;
            }
            else
            {
                Message = Time + " - " + ID + " crashed";
            }
        }

        public EventType Type { get; }
        public string ID { get; }
        public float[] Target { get; }
        public Action OpenWindow { get; }
        public string Message { get; }
        public TimeKeeper.Chronos Time { get; }
    }
}
