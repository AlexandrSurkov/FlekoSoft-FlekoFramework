﻿using System;
using System.Collections.Generic;

namespace Flekosoft.Common.Plugins
{
    public abstract class Plugin : PropertyChangedErrorNotifyDisposableBase, IPlugin
    {
        readonly List<object> _instancesList = new List<object>();
        private bool _isEnabled;

        protected Plugin(Guid guid, Type type, string name, string description, bool isSingleInstance, Version version) : base($"{name} ({guid})")
        {
            Guid = guid;
            Type = type;
            Name = name;
            Description = description;
            IsSingleInstance = isSingleInstance;
            Version = version;
        }

        public Guid Guid { get; }

        public string Name { get; }

        public string Description { get; }

        public Type Type { get; }

        public Version Version { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    AppendDebugLogMessage($"{this}.{nameof(IsEnabled)} was changed from {_isEnabled} to {value}");
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public object GetInstance()
        {
            return GetInstance(null);
        }

        public object GetInstance(object param)
        {
            if (!IsSingleInstance)
            {
                var instance = InternalGetInstance(param);
                _instancesList.Add(instance);
                return instance;
            }
            if (_instancesList.Count == 0)
            {
                var instance = InternalGetInstance(param);
                _instancesList.Add(instance);
                return instance;
            }
            return _instancesList[0];
        }

        public bool IsSingleInstance { get; }

        protected abstract object InternalGetInstance(object param);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (object o in _instancesList)
                {
                    if (o is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
