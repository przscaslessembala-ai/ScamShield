
using System.Globalization;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;

const string DataPath = "data/dataset.csv";
const string ModelPath = "models/scamshield-model.zip";

// Sans vrai jeu de données, on génère des données FICTIVES pour valider la chaîne.
if (!File.Exists(DataPath))
{
    SyntheticData.Generate(DataPath);
    Console.WriteLine("ATTENTION : données FICTIVES, uniquement pour valider le pipeline.");
}

var ml = new MLContext(seed: 42);

// Mêmes noms que les propriétés de ModelInput dans ScamShield.AI.
string[] featureNames =
{
    "FileSize", "Entropy", "SectionCount", "SuspiciousImportCount", "IsSigned", "HasPackedSection"
};

var data = ml.Data.LoadFromTextFile<TrainingRow>(DataPath, hasHeader: true, separatorChar: ',');

// 80 % entraînement / 20 % test (le test n'est jamais utilisé pour entraîner)
var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 42);

var pipeline = ml.Transforms.Concatenate("Features", featureNames)
    .Append(ml.Transforms.NormalizeMinMax("Features"))
    .Append(ml.BinaryClassification.Trainers.FastTree(
        labelColumnName: "Label", featureColumnName: "Features",
        numberOfLeaves: 20, numberOfTrees: 100));

var model = pipeline.Fit(split.TrainSet);

var predictions = model.Transform(split.TestSet);
var metrics = ml.BinaryClassification.EvaluateNonCalibrated(predictions, labelColumnName: "Label");

// Counts[réel][prédit] : [0][0]=vrais positifs, [0][1]=faux négatifs,
// [1][0]=faux positifs, [1][1]=vrais négatifs
var c = metrics.ConfusionMatrix.Counts;
double fpr = c[1][0] / (c[1][0] + c[1][1]);

Console.WriteLine($"Exactitude        : {metrics.Accuracy:P2}");
Console.WriteLine($"Précision         : {metrics.PositivePrecision:P2}");
Console.WriteLine($"Rappel            : {metrics.PositiveRecall:P2}");
Console.WriteLine($"AUC               : {metrics.AreaUnderRocCurve:F3}");
Console.WriteLine($"Faux positifs (%) : {fpr:P2}");
Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

Directory.CreateDirectory("models");
ml.Model.Save(model, split.TrainSet.Schema, ModelPath);
Console.WriteLine($"Modèle exporté : {ModelPath}");

public class TrainingRow
{
    [LoadColumn(0)] public float FileSize { get; set; }
    [LoadColumn(1)] public float Entropy { get; set; }
    [LoadColumn(2)] public float SectionCount { get; set; }
    [LoadColumn(3)] public float SuspiciousImportCount { get; set; }
    [LoadColumn(4)] public float IsSigned { get; set; }
    [LoadColumn(5)] public float HasPackedSection { get; set; }
    [LoadColumn(6)] public bool Label { get; set; }
}

public static class SyntheticData
{
    public static void Generate(string path, int rows = 4000)
    {
        var rnd = new Random(42);
        var sb = new StringBuilder();
        sb.AppendLine("FileSize,Entropy,SectionCount,SuspiciousImportCount,IsSigned,HasPackedSection,Label");

        for (int i = 0; i < rows; i++)
        {
            bool bad = rnd.NextDouble() < 0.5;
            double entropy = bad ? 6.5 + rnd.NextDouble() * 1.5 : 4.5 + rnd.NextDouble() * 2.0;
            int sections = bad ? rnd.Next(2, 6) : rnd.Next(4, 9);
            int suspicious = bad ? rnd.Next(2, 12) : rnd.Next(0, 4);
            int isSigned = bad ? (rnd.NextDouble() < 0.1 ? 1 : 0) : (rnd.NextDouble() < 0.7 ? 1 : 0);
            int packed = bad ? (rnd.NextDouble() < 0.6 ? 1 : 0) : (rnd.NextDouble() < 0.05 ? 1 : 0);
            int size = rnd.Next(20_000, 5_000_000);

            sb.AppendLine(string.Join(",",
                size.ToString(CultureInfo.InvariantCulture),
                entropy.ToString("F3", CultureInfo.InvariantCulture),
                sections, suspicious, isSigned, packed, bad ? 1 : 0));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sb.ToString());
    }
}