﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NooSphere.Core.Primitives;

namespace NooSphere.Core.Devices
{
    public class Device:IEntity
    {
        public Device()
        {
            this.Identity = new Identity();
        }
        public DeviceType DeviceType { get; set; }
        public DeviceRole DeviceRole { get; set; }
        public Identity Identity { get; set; }
        public string Location { get; set; }
        public string BaseAddress { get; set; }
    }
    public enum DeviceType
    {
        Stationary,
        Mobile
    }
    public enum DeviceRole
    {
        Master,
        Slave
    }
}
