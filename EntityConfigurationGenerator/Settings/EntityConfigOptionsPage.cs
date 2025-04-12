using Microsoft.VisualStudio.Shell;

using System;
using System.ComponentModel;

namespace EntityConfigurationGenerator
{
    public class EntityConfigOptionsPage : DialogPage
    {
        public event EventHandler<bool> GenerateAsPartialChanged;

        private bool _generateAsPartial;
        [Category("Generation Settings")]
        [DisplayName("Generate as Partial")]
        [Description("Generate configuration class as a partial and update the original class.")]
        public bool GenerateAsPartial
        {
            get => _generateAsPartial;
            set
            {
                if (_generateAsPartial != value)
                {
                    _generateAsPartial = value;
                    GenerateAsPartialChanged?.Invoke(this, _generateAsPartial);
                }
            }
        }

    }
}
