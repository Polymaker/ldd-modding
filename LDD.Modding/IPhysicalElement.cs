﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDD.Modding
{
    public interface IPhysicalElement
    {
        ItemTransform Transform { get; set; }

        event EventHandler TranformChanged;
    }
}
