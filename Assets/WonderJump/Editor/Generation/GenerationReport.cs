using System.Collections.Generic;
using System.Text;

namespace WonderJump.EditorTools.Generation
{
    /// <summary>What a generator run did. Returned by the API, printed to the console, asserted by tests.</summary>
    public sealed class GenerationReport
    {
        public readonly List<string> Created = new List<string>();
        public readonly List<string> Regenerated = new List<string>();
        public readonly List<string> UpToDate = new List<string>();
        /// <summary>Generated assets edited by hand since the last run. Left untouched.</summary>
        public readonly List<string> SkippedModified = new List<string>();
        /// <summary>Assets at a generator path that the manifest does not know. Left untouched.</summary>
        public readonly List<string> SkippedUntracked = new List<string>();
        public readonly List<string> VariantsCreated = new List<string>();
        public readonly List<string> MaterialsCreated = new List<string>();
        public readonly List<string> MaterialsPreserved = new List<string>();
        public readonly List<string> MaterialsRepaired = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool Aborted;

        public bool Succeeded { get { return !Aborted && Errors.Count == 0; } }

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Succeeded ? "Wonder Jump generation finished." : "Wonder Jump generation FAILED.");
            Line(sb, "Assets created", Created);
            Line(sb, "Assets regenerated (definition changed)", Regenerated);
            Line(sb, "Assets up to date", UpToDate);
            Line(sb, "Skipped: modified by hand", SkippedModified);
            Line(sb, "Skipped: not tracked by manifest", SkippedUntracked);
            Line(sb, "Content variants created", VariantsCreated);
            Line(sb, "Materials created", MaterialsCreated);
            Line(sb, "Materials preserved", MaterialsPreserved);
            Line(sb, "Materials repaired (bad shader)", MaterialsRepaired);
            Line(sb, "Warnings", Warnings);
            Line(sb, "Errors", Errors);
            return sb.ToString();
        }

        private static void Line(StringBuilder sb, string label, List<string> items)
        {
            sb.Append("  ").Append(label).Append(": ").Append(items.Count).AppendLine();
            bool detailed = items.Count > 0 && items.Count <= 12 && (label.StartsWith("Skipped") || label == "Warnings" || label == "Errors" || label.StartsWith("Materials repaired"));
            if (!detailed) return;
            for (int i = 0; i < items.Count; i++) sb.Append("    - ").AppendLine(items[i]);
        }
    }
}
