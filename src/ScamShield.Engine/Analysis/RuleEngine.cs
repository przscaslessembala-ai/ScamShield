using System.Text.RegularExpressions;
using ScamShield.Core.Models;

namespace ScamShield.Engine.Analysis;

/// <summary>Résultat du moteur de règles : points (0 à 100) et raisons.</summary>
internal sealed record RuleOutcome(int Score, IReadOnlyList<RiskReason> Reasons);

/// <summary>
/// Moteur de règles : chaque indicateur suspect ajoute des points, une signature valide en retire.
/// Seuls des hashs connus ou plusieurs indicateurs cumulés mènent à « dangereux » :
/// un indicateur isolé ne dépasse jamais le seuil « suspect ».
/// </summary>
internal static class RuleEngine
{
    private static readonly Regex DoubleExtensionPattern = new(
        @"\.(pdf|docx?|xlsx?|pptx?|jpe?g|png|gif|txt|mp3|mp4)\.(exe|scr|com|bat|cmd|pif|vbs|js)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static RuleOutcome Evaluate(string path, double entropy, PeMeasures pe)
    {
        var reasons = new List<RiskReason>();
        var points = 0;

        void Add(string code, string description, int value)
        {
            points += value;
            reasons.Add(new RiskReason(code, description, value / 100.0));
        }

        var name = Path.GetFileName(path);

        if (string.Equals(name, "autorun.inf", StringComparison.OrdinalIgnoreCase))
        {
            Add(EngineReasonCodes.AutorunFile, "Fichier d'exécution automatique : il peut lancer un programme dès le branchement du support.", 30);
        }

        if (DoubleExtensionPattern.IsMatch(name))
        {
            Add(EngineReasonCodes.DoubleExtension, "Le nom du fichier imite un document alors que c'est un programme.", 30);
        }

        if (name.EndsWith(".scr", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".pif", StringComparison.OrdinalIgnoreCase))
        {
            Add(EngineReasonCodes.ScreensaverExecutable, "Ce type de fichier est un programme déguisé.", 15);
        }

        if (pe.IsPe)
        {
            if (entropy >= 7.2)
            {
                Add(EngineReasonCodes.HighEntropy, "Le contenu du fichier est très compressé ou chiffré, comme le font les logiciels qui cachent leur code.", 20);
            }

            if (pe.HasPackedSection)
            {
                Add(EngineReasonCodes.PackedSection, "Une partie du programme est compressée ou chiffrée pour être difficile à examiner.", 20);
            }

            if (pe.SuspiciousImportCount >= 3)
            {
                Add(EngineReasonCodes.SuspiciousImports, "Le programme utilise plusieurs fonctions Windows souvent employées par des logiciels malveillants.", 25);
            }
            else if (pe.SuspiciousImportCount >= 1)
            {
                Add(EngineReasonCodes.SuspiciousImports, "Le programme utilise une fonction Windows parfois détournée par des logiciels malveillants.", 10);
            }

            if (!pe.IsSigned)
            {
                Add(EngineReasonCodes.UnsignedExecutable, "Le programme n'a pas de signature numérique valide : son éditeur ne peut pas être vérifié.", 10);
            }
            else
            {
                points -= 30;
            }
        }

        return new RuleOutcome(Math.Clamp(points, 0, 100), reasons);
    }
}
