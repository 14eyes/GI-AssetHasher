using CommandLine.Text;
using CommandLine;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static GI_AssetHasher.AssetIndex;

namespace GI_AssetHasher
{
    internal class Program
    {
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Path to input AssetIndex")]
            public string? inputFile { get; set; }

            [Option('o', "output", Required = true, HelpText = "Path to output AssetIndex")]
            public string? outputFile { get; set; }

            [Option('r', "rawNames", Required = false, HelpText = "Path to text file with filenames")]
            public string? rawNamesFile { get; set; }

            [Option('m', "mapped", Required = false, HelpText = "Path to json mapping the hash to filename")]
            public string? mappedFile { get; set; }
        }
        internal static AssetIndex? assetIndex { get; set; }
        internal static string[]? rawNames { get; set; }
        internal static string[]? typeList { get; set; }
        internal static Dictionary<ulong, string>? mappedDictionary { get; set; }
        internal static ulong foundCount { get; set; } = 0;
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.inputFile is not null)
                       {
                           try
                           {
                               assetIndex = JsonConvert.DeserializeObject<AssetIndex>(File.ReadAllText(o.inputFile))!;
                           }
                           catch
                           {
                               throw new ArgumentException();
                           }
                           typeList = assetIndex!.Types.Values.ToHashSet().ToArray();
                       }
                       if (o.mappedFile is not null)
                       {
                           try
                           {
                               mappedDictionary = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(File.ReadAllText(o.mappedFile))!;
                           }
                           catch
                           {
                               throw new ArgumentException();
                           }
                           Console.WriteLine("Loaded Mapped Assets: {0}", mappedDictionary.Count);
                       }
                       if (o.rawNamesFile is not null)
                       {
                           try
                           {
                               rawNames = File.ReadAllLines(o.rawNamesFile);
                           }
                           catch
                           {
                               throw new ArgumentException();
                           }

                           Console.WriteLine("Loaded Raw Names: {0}", rawNames.Length);
                           if (mappedDictionary is not null)
                           {
                               CleanUpRawNames();
                               Console.WriteLine("Raw Names After cleanup: {0}", rawNames.Length);
                           }
                           else
                           {
                               mappedDictionary = new Dictionary<ulong, string>();
                           }

                           foreach (var rawName in rawNames)
                           {
                               foreach (var type in typeList!)
                               {
                                   var flag = mappedDictionary.TryAdd(Hashing.GetPathHash(rawName + type), rawName.Replace("LuaBytes", "Lua"));
                                   if (!flag)
                                   {
                                       var hash = Hashing.GetPathHash(rawName + type);
                                       foreach (var ttype in typeList)
                                       {
                                           if (Hashing.GetPathHash(mappedDictionary[hash] + ttype) == hash)
                                           {
                                               Console.WriteLine("Overlap Found Ignoring: {0}", rawName + type);
                                           }
                                       }

                                   }
                               }
                           }
                       }
                       if (o.mappedFile is null && o.rawNamesFile is null)
                       {
                           throw new ArgumentException();
                       }
                       Start();
                       Console.WriteLine("Saving");
                       File.WriteAllText(o.outputFile!, JsonConvert.SerializeObject(assetIndex, Formatting.Indented));
                   });
        }

        private static void Start()
        {
            Console.WriteLine("Starting Hashing...");
            ulong totalCount = 0;
            foreach (var keyValuePair in assetIndex!.SubAssets.Values.ToArray())
            {
                foreach (var subAssetInfo in keyValuePair)
                {
                    totalCount++;
                    if (!string.IsNullOrEmpty(subAssetInfo.Name))
                    {
                        foundCount++;
                        Console.WriteLine("Skipping already mapped asset: {0}", subAssetInfo.Name);
                        continue;
                    }
                    FindNameInMapped(subAssetInfo);

                }
            }
            Console.WriteLine("Done found {0}/{1} {2:0.00}%", foundCount, totalCount, (double)foundCount / totalCount * 100);
        }

        private static void CleanUpRawNames()
        {
            var mapped = mappedDictionary!.Values.ToHashSet();
            var cleanrawNames = rawNames!.ToList();
            cleanrawNames.RemoveAll(r => mapped.Contains(r.Replace("LuaBytes", "Lua")));
            rawNames = cleanrawNames.ToArray();
            //File.WriteAllLines("raw_names_clean.txt", rawNames);
        }
        private static void FindNameInMapped(AssetIndex.SubAssetInfo subAssetInfo)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var hash = Hashing.PreLastToHash(subAssetInfo.PathHashPre, subAssetInfo.PathHashLast);
            var flag = mappedDictionary!.TryGetValue(hash, out var name);
            stopwatch.Stop();
            if (!flag)
            {
                Console.WriteLine("Failed to get name for {0} ({1} {2}) in {3}ms", hash, subAssetInfo.PathHashPre, subAssetInfo.PathHashLast, stopwatch.ElapsedMilliseconds);
                return;
            }
            subAssetInfo.Name = name!;
            foundCount++;
            Console.WriteLine("Success Found Name: {0} in {1}ms", name, stopwatch.ElapsedMilliseconds);
        }
        private static void GenerateMappedDictionary()
        {
            foreach (var file in Directory.GetFiles(@"N:\gi-asset-indexes\mapped"))
            {
                Console.WriteLine(file + " " + mappedDictionary!.Count);
                var ai = JsonConvert.DeserializeObject<AssetIndex>(File.ReadAllText(file))!;
                foreach (var subAsset in ai.SubAssets.Values.ToArray())
                {
                    foreach (var subAssetInfo in subAsset)
                    {
                        if (!string.IsNullOrEmpty(subAssetInfo.Name))
                        {
                            var flag = mappedDictionary.TryAdd(Hashing.PreLastToHash(subAssetInfo.PathHashPre, subAssetInfo.PathHashLast), subAssetInfo.Name.Replace("LuaBytes", "Lua"));
                            if (flag)
                                continue;
                            if (mappedDictionary[Hashing.PreLastToHash(subAssetInfo.PathHashPre, subAssetInfo.PathHashLast)] != subAssetInfo.Name.Replace("LuaBytes", "Lua"))
                            {
                                Console.WriteLine("Fuck {0} {1}", mappedDictionary[Hashing.PreLastToHash(subAssetInfo.PathHashPre, subAssetInfo.PathHashLast)], subAssetInfo.Name);
                            }
                        }
                    }
                }
            }
            File.WriteAllText("mapped-updated.json", JsonConvert.SerializeObject(mappedDictionary, Formatting.Indented));
        }
    }
}