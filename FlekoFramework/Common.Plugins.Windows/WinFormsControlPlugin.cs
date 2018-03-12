﻿using System;
using System.Windows.Forms;

namespace Flekosoft.Common.Plugins.Windows
{
    public abstract class WinFormsControlPlugin : Plugin, IWinFormsControlPlugin
    {
        protected WinFormsControlPlugin(Guid guid, Type type, string name, string description, bool isSingleInstance) : base(guid, type, name, description, isSingleInstance)
        {
        }

        protected abstract ContainerControl InternalGetControl(object instance);


        public ContainerControl GetControl(object instance)
        {
            if (Type == instance.GetType())
            {
                return InternalGetControl(instance);
            }
            return null;
        }
    }
}
