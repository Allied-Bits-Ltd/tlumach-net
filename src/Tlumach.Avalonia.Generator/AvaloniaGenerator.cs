using Microsoft.CodeAnalysis;

using Tlumach.GeneratorBase;

namespace Tlumach.Avalonia.Generator
{
    [Generator]
    public class AvaloniaGenerator : BaseGenerator
    {
        protected override string GetNamespace()
        {
            return "Tlumach.Avalonia";
        }
    }
}
