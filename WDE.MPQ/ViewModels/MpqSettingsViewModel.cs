using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.MPQ.ViewModels
{
    [AutoRegister]
    public class MpqSettingsViewModel : BindableBase, IConfigurable
    {
        private string? woWPath;

        public MpqSettingsViewModel(IMpqSettings mpqSettings, 
            IWoWFilesVerifier verifier, 
            IWindowManager windowManager,
            IMessageBoxService messageBoxService)
        {
            woWPath = mpqSettings.Path;
            
            Save = new DelegateCommand(() =>
            {
                mpqSettings.Path = woWPath;
                IsModified = false;
                RaisePropertyChanged(nameof(IsModified));
            });

            PickFolder = new AsyncAutoCommand(async () =>
            {
                var folder = await windowManager.ShowFolderPickerDialog(woWPath ?? "");
                if (folder != null)
                {
                    if (verifier.VerifyFolder(folder) == WoWFilesType.Invalid)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("WoW Client Data")
                            .SetMainInstruction("Invalid WoW folder")
                            .SetContent(
                                "This doesn't look like a correct Wrath of The Lich King folder.\n\nSelect main game folder (wow.exe file must be there).\n\nOther WoW versions are not supported now.")
                            .WithOkButton(true)
                            .Build());
                    }
                    else
                        WoWPath = folder;
                }
            });
        }
        
        public ICommand PickFolder { get; }

        public string? WoWPath
        {
            get => woWPath;
            set
            {
                SetProperty(ref woWPath, value);
                IsModified = true;
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public ICommand Save { get; set; }
        public string Name => "Client data files";
        public string ShortDescription =>
            "The editor can open Wrath of the Lich King files (MPQ) for extended features.";
        public bool IsModified { get; set; }
        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Advanced;
    }
}