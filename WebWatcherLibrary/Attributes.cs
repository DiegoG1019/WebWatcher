﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class WatcherAttribute : Attribute
{
    public WatcherAttribute(){ }
}
