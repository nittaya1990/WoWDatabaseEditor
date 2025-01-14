﻿using System.ComponentModel;
using System.Windows.Input;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface IConfigurable : INotifyPropertyChanged
    {
        ICommand Save { get; }
        string Name { get; }
        string? ShortDescription { get; }
        bool IsModified { get; }
        bool IsRestartRequired { get; }
        ConfigurableGroup Group { get; }
        
        void ConfigurableOpened() { }
    }

    public enum ConfigurableGroup
    {
        Basic,
        Advanced
    }
}