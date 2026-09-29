using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
// Alias pour éviter les conflits
using Drawing = System.Drawing;
using Imaging = System.Drawing.Imaging;

namespace Compress_Kami.Core
{
    // --- Noeud Huffman ---
    internal class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte? Value { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }

        public bool IsLeaf => Value.HasValue;

        public int CompareTo(HuffmanNode other)
            => Frequency.CompareTo(other.Frequency);
    }

    // --- Structure pour les entrées de dossier ---
    internal class FolderEntry
    {
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public byte[] Data { get; set; }
        public bool IsDirectory { get; set; }
        public string FileType { get; set; }
    }

    public static class HuffmanFolderCompressor
    {
        private const string SIGNATURE = "KEYCEHFM";
        private const byte VERSION = 2; // Version 2 avec support multimédia

        // Extensions de fichiers multimédias supportées
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
        private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };
        private static readonly string[] CompressibleExtensions = { ".txt", ".csv", ".xml", ".json", ".log", ".doc", ".docx", ".pdf" };

        public static void CompressFolder(string inputFolderPath, string outputFilePath)
        {
            if (!Directory.Exists(inputFolderPath))
                throw new DirectoryNotFoundException("Dossier source non trouvé: " + inputFolderPath);

            if (!outputFilePath.EndsWith(".keyce", StringComparison.OrdinalIgnoreCase))
                outputFilePath += ".keyce";

            Console.WriteLine($"Compression du dossier: {inputFolderPath}");
            Console.WriteLine($"Vers: {outputFilePath}");

            // 1. Collecter tous les fichiers et dossiers
            var allEntries = CollectAllEntries(inputFolderPath, inputFolderPath);

            if (allEntries.Count == 0)
            {
                CompressEmptyFolder(outputFilePath);
                Console.WriteLine("Dossier vide compressé.");
                return;
            }

            Console.WriteLine($"Nombre d'éléments trouvés: {allEntries.Count}");

            // 2. Concaténer toutes les données pour Huffman
            byte[] allData = ConcatenateAllData(allEntries);
            Console.WriteLine($"Données totales à compresser: {allData.Length} bytes");

            // 3. Calcul des fréquences sur TOUTES les données
            int[] frequencies = new int[256];
            foreach (byte b in allData) frequencies[b]++;

            // 4. Construction de l'arbre Huffman
            var nodes = new List<HuffmanNode>();
            for (int i = 0; i < 256; i++)
            {
                if (frequencies[i] > 0)
                {
                    nodes.Add(new HuffmanNode
                    {
                        Value = (byte)i,
                        Frequency = frequencies[i]
                    });
                }
            }

            Console.WriteLine($"Nombre de noeuds Huffman: {nodes.Count}");

            while (nodes.Count > 1)
            {
                nodes = nodes.OrderBy(n => n.Frequency).ToList();
                var left = nodes[0];
                var right = nodes[1];
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                var parent = new HuffmanNode
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };
                nodes.Add(parent);
            }

            HuffmanNode root = nodes.Count > 0 ? nodes[0] : null;

            // 5. Génération des codes Huffman
            var codes = new Dictionary<byte, List<bool>>();
            if (root != null)
                BuildCodes(root, new List<bool>(), codes);

            Console.WriteLine($"Codes Huffman générés: {codes.Count}");

            // 6. Écriture du fichier .keyce
            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                // En-tête
                bw.Write(Encoding.UTF8.GetBytes(SIGNATURE));
                bw.Write(VERSION);

                // Nombre d'entrées
                bw.Write(allEntries.Count);

                // Écrire la table des entrées
                foreach (var entry in allEntries)
                {
                    bw.Write(entry.RelativePath);
                    bw.Write(entry.IsDirectory);
                    bw.Write(entry.Size);
                    bw.Write(entry.FileType ?? "");
                }

                // Écrire la table des fréquences
                for (int i = 0; i < 256; i++)
                {
                    bw.Write(frequencies[i]);
                }

                // Compresser et écrire chaque fichier
                long totalCompressed = 0;
                long totalOriginal = 0;

                foreach (var entry in allEntries)
                {
                    if (!entry.IsDirectory && entry.Data != null)
                    {
                        byte[] dataToCompress = entry.Data;
                        string extension = Path.GetExtension(entry.RelativePath).ToLower();

                        // Optimisation pour les images (version simplifiée)
                        if (ImageExtensions.Contains(extension))
                        {
                            dataToCompress = OptimizeImageData(entry.Data, extension);
                            if (dataToCompress.Length < entry.Data.Length)
                            {
                                Console.WriteLine($"Image optimisée: {entry.RelativePath} {entry.Data.Length} -> {dataToCompress.Length} bytes");
                            }
                        }

                        byte[] compressedData = CompressData(dataToCompress, codes);
                        bw.Write(compressedData.Length);
                        bw.Write(compressedData);

                        totalCompressed += compressedData.Length;
                        totalOriginal += entry.Data.Length;

                        double ratio = entry.Data.Length > 0 ?
                            (1 - (double)compressedData.Length / entry.Data.Length) * 100 : 0;

                        Console.WriteLine($"Fichier compressé: {entry.RelativePath} " +
                                         $"{entry.Data.Length} -> {compressedData.Length} bytes " +
                                         $"(Compression: {ratio:F1}%)");
                    }
                }

                // Écrire les statistiques de compression
                bw.Write(totalOriginal);
                bw.Write(totalCompressed);

                double overallRatio = totalOriginal > 0 ?
                    (1 - (double)totalCompressed / totalOriginal) * 100 : 0;

                Console.WriteLine($"Compression terminée. " +
                                $"Taille originale: {totalOriginal} bytes, " +
                                $"Taille compressée: {totalCompressed} bytes, " +
                                $"Ratio: {overallRatio:F1}%");
            }
        }

        private static List<FolderEntry> CollectAllEntries(string path, string rootPath)
        {
            var entries = new List<FolderEntry>();

            try
            {
                // Ajouter le dossier racine lui-même
                entries.Add(new FolderEntry
                {
                    RelativePath = "",
                    IsDirectory = true,
                    Size = 0,
                    Data = null,
                    FileType = "directory"
                });

                // Ajouter les sous-dossiers
                foreach (string directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                {
                    entries.Add(new FolderEntry
                    {
                        RelativePath = GetRelativePath(directory, rootPath),
                        IsDirectory = true,
                        Size = 0,
                        Data = null,
                        FileType = "directory"
                    });
                }

                // Ajouter les fichiers
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(file);
                        string extension = Path.GetExtension(file).ToLower();
                        string fileType = GetFileType(extension);

                        entries.Add(new FolderEntry
                        {
                            RelativePath = GetRelativePath(file, rootPath),
                            IsDirectory = false,
                            Size = data.Length,
                            Data = data,
                            FileType = fileType
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lecture fichier {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur collecte entrées: {ex.Message}");
            }

            return entries;
        }

        private static string GetFileType(string extension)
        {
            if (ImageExtensions.Contains(extension)) return "image";
            if (VideoExtensions.Contains(extension)) return "video";
            if (CompressibleExtensions.Contains(extension)) return "compressible";
            return "binary";
        }

        private static byte[] OptimizeImageData(byte[] originalData, string extension)
        {
            try
            {
                using (var ms = new MemoryStream(originalData))
                using (var image = Drawing.Image.FromStream(ms))
                {
                    // Pour l'instant, on retourne les données originales
                    // L'optimisation d'image peut être ajoutée plus tard
                    return originalData;
                }
            }
            catch
            {
                // Si l'optimisation échoue, retourner les données originales
                return originalData;
            }
        }

        private static string GetRelativePath(string fullPath, string rootPath)
        {
            Uri pathUri = new Uri(fullPath);
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                rootPath += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(rootPath);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        private static byte[] ConcatenateAllData(List<FolderEntry> entries)
        {
            var allData = new List<byte>();

            foreach (var entry in entries)
            {
                if (!entry.IsDirectory && entry.Data != null)
                {
                    allData.AddRange(entry.Data);
                }
            }

            return allData.ToArray();
        }

        private static byte[] CompressData(byte[] data, Dictionary<byte, List<bool>> codes)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                byte currentByte = 0;
                int bitPosition = 0;

                foreach (byte b in data)
                {
                    if (codes.ContainsKey(b))
                    {
                        foreach (bool bit in codes[b])
                        {
                            currentByte = (byte)((currentByte << 1) | (bit ? 1 : 0));
                            bitPosition++;

                            if (bitPosition == 8)
                            {
                                bw.Write(currentByte);
                                currentByte = 0;
                                bitPosition = 0;
                            }
                        }
                    }
                }

                // Bits restants
                if (bitPosition > 0)
                {
                    currentByte <<= (8 - bitPosition);
                    bw.Write(currentByte);
                }

                return ms.ToArray();
            }
        }

        private static void CompressEmptyFolder(string outputFilePath)
        {
            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.UTF8.GetBytes(SIGNATURE));
                bw.Write(VERSION);
                bw.Write(0); // Aucune entrée
            }
        }

        private static void BuildCodes(HuffmanNode node, List<bool> current, Dictionary<byte, List<bool>> codes)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                codes[node.Value.Value] = new List<bool>(current);
                return;
            }

            current.Add(false);
            BuildCodes(node.Left, current, codes);
            current.RemoveAt(current.Count - 1);

            current.Add(true);
            BuildCodes(node.Right, current, codes);
            current.RemoveAt(current.Count - 1);
        }

        public static void DecompressFolder(string inputFilePath, string outputFolderPath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Fichier .keyce non trouvé: " + inputFilePath);

            Console.WriteLine($"Décompression de: {inputFilePath}");
            Console.WriteLine($"Vers: {outputFolderPath}");

            using (var fs = new FileStream(inputFilePath, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                // 1. Vérifier l'en-tête
                byte[] signatureBytes = br.ReadBytes(8);
                string signature = Encoding.UTF8.GetString(signatureBytes);
                byte version = br.ReadByte();

                if (signature != SIGNATURE)
                    throw new InvalidDataException("Format .keyce non reconnu");

                // 2. Lire le nombre d'entrées
                int entryCount = br.ReadInt32();

                if (entryCount == 0)
                {
                    Directory.CreateDirectory(outputFolderPath);
                    Console.WriteLine("Dossier vide décompressé.");
                    return;
                }

                Console.WriteLine($"Nombre d'éléments à décompresser: {entryCount}");

                // 3. Lire la table des entrées
                var entries = new List<FolderEntry>();
                for (int i = 0; i < entryCount; i++)
                {
                    var entry = new FolderEntry
                    {
                        RelativePath = br.ReadString(),
                        IsDirectory = br.ReadBoolean(),
                        Size = br.ReadInt64(),
                        FileType = br.ReadString()
                    };
                    entries.Add(entry);
                }

                // 4. Lire la table des fréquences
                int[] frequencies = new int[256];
                for (int i = 0; i < 256; i++)
                {
                    frequencies[i] = br.ReadInt32();
                }

                // 5. Reconstruire l'arbre Huffman
                var nodes = new List<HuffmanNode>();
                for (int i = 0; i < 256; i++)
                {
                    if (frequencies[i] > 0)
                    {
                        nodes.Add(new HuffmanNode
                        {
                            Value = (byte)i,
                            Frequency = frequencies[i]
                        });
                    }
                }

                while (nodes.Count > 1)
                {
                    nodes = nodes.OrderBy(n => n.Frequency).ToList();
                    var left = nodes[0];
                    var right = nodes[1];
                    nodes.RemoveAt(0);
                    nodes.RemoveAt(0);

                    var parent = new HuffmanNode
                    {
                        Frequency = left.Frequency + right.Frequency,
                        Left = left,
                        Right = right
                    };
                    nodes.Add(parent);
                }

                HuffmanNode root = nodes.Count > 0 ? nodes[0] : null;

                // 6. Créer la structure des dossiers
                Directory.CreateDirectory(outputFolderPath);
                foreach (var entry in entries.Where(e => e.IsDirectory))
                {
                    string fullPath = Path.Combine(outputFolderPath, entry.RelativePath);
                    Directory.CreateDirectory(fullPath);
                    Console.WriteLine($"Dossier créé: {fullPath}");
                }

                // 7. Décompresser les fichiers
                foreach (var entry in entries.Where(e => !e.IsDirectory))
                {
                    try
                    {
                        int compressedSize = br.ReadInt32();
                        byte[] compressedData = br.ReadBytes(compressedSize);

                        byte[] decompressedData = DecompressData(compressedData, root, entry.Size);

                        string fullFilePath = Path.Combine(outputFolderPath, entry.RelativePath);
                        string directory = Path.GetDirectoryName(fullFilePath);

                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        File.WriteAllBytes(fullFilePath, decompressedData);
                        Console.WriteLine($"Fichier décompressé: {fullFilePath} ({decompressedData.Length} bytes) - Type: {entry.FileType}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur décompression {entry.RelativePath}: {ex.Message}");
                    }
                }

                // Lire et afficher les statistiques
                if (fs.Position < fs.Length - 16)
                {
                    try
                    {
                        long totalOriginal = br.ReadInt64();
                        long totalCompressed = br.ReadInt64();
                        double ratio = totalOriginal > 0 ? (1 - (double)totalCompressed / totalOriginal) * 100 : 0;
                        Console.WriteLine($"Statistiques compression: {totalOriginal} -> {totalCompressed} bytes (Ratio: {ratio:F1}%)");
                    }
                    catch
                    {
                        // Ignorer si les statistiques ne sont pas présentes
                    }
                }

                Console.WriteLine("Décompression terminée avec succès!");
            }
        }

        private static byte[] DecompressData(byte[] compressedData, HuffmanNode root, long originalSize)
        {
            if (root == null || compressedData.Length == 0 || originalSize == 0)
                return new byte[0];

            var result = new List<byte>();
            HuffmanNode currentNode = root;

            foreach (byte currentByte in compressedData)
            {
                for (int i = 7; i >= 0 && result.Count < originalSize; i--)
                {
                    bool bit = (currentByte & (1 << i)) != 0;

                    currentNode = bit ? currentNode.Right : currentNode.Left;

                    if (currentNode.IsLeaf)
                    {
                        result.Add(currentNode.Value.Value);
                        currentNode = root;
                    }
                }
            }

            return result.ToArray();
        }

        public static bool IsKeyCeFile(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    byte[] signatureBytes = br.ReadBytes(8);
                    string signature = Encoding.UTF8.GetString(signatureBytes);
                    return signature == SIGNATURE;
                }
            }
            catch
            {
                return false;
            }
        }

        // Méthode pour obtenir les informations sur les types supportés
        public static string GetSupportedFormats()
        {
            return $"Images: {string.Join(", ", ImageExtensions)}\n" +
                   $"Vidéos: {string.Join(", ", VideoExtensions)}\n" +
                   $"Documents: {string.Join(", ", CompressibleExtensions)}\n" +
                   "Tous les autres types de fichiers sont aussi supportés!";
        }
    }
}