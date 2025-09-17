using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IGAntiRansomwareUI
{
    public class RuleManager
    {
        public enum RuleType
        {
            Predefined = 0,
            Dynamic = 1,
            Signature = 2,
            Behavior = 3
        }

        [Flags]
        public enum RuleFlags
        {
            None = 0,
            Match = 1,
            Block = 2,
            AlertOnly = 4,
            Backup = 8,
            Active = 16
        }

        public class RuleStruct
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public RuleType Type { get; set; }
            public RuleFlags Flags { get; set; }
            public string Pattern { get; set; }
            public string TargetPath { get; set; }
            public string? Description { get; set; }
            public uint MinFileSize { get; set; }
            public uint MaxFileSize { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }

            public RuleStruct()
            {
                Id = Guid.NewGuid();
                Created = DateTime.Now;
                Modified = DateTime.Now;
                MinFileSize = 0;
                MaxFileSize = 100 * 1024 * 1024; // 100MB default
                Flags = RuleFlags.Active | RuleFlags.AlertOnly;
            }
        }

        private List<RuleStruct> predefinedRules;
        private List<RuleStruct> dynamicRules;
        private readonly string rulesDirectory;

        public RuleManager()
        {
            rulesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules");
            Directory.CreateDirectory(rulesDirectory);

            predefinedRules = new List<RuleStruct>();
            dynamicRules = new List<RuleStruct>();

            LoadPredefinedRules();
            LoadDynamicRules();
        }

        private void LoadPredefinedRules()
        {
            string predefinedPath = Path.Combine(rulesDirectory, "default_rules.json");

            if (File.Exists(predefinedPath))
            {
                try
                {
                    string json = File.ReadAllText(predefinedPath);
                    predefinedRules = JsonSerializer.Deserialize<List<RuleStruct>>(json) ?? new List<RuleStruct>();
                }
                catch (Exception ex)
                {
                    // Log error and use default rules
                    predefinedRules = GetDefaultPredefinedRules();
                    SavePredefinedRules();
                }
            }
            else
            {
                predefinedRules = GetDefaultPredefinedRules();
                SavePredefinedRules();
            }
        }

        private List<RuleStruct> GetDefaultPredefinedRules()
        {
            return new List<RuleStruct>
            {
                new RuleStruct
                {
                    Name = "Ransomware Extension Pattern",
                    Type = RuleType.Signature,
                    Flags = RuleFlags.Active | RuleFlags.Block | RuleFlags.AlertOnly,
                    Pattern = ".crypt;.locked;.encrypted;.ransom",
                    Description = "Detects common ransomware file extensions",
                    MinFileSize = 1024, // 1KB
                    MaxFileSize = 50 * 1024 * 1024 // 50MB
                },
                new RuleStruct
                {
                    Name = "Suspicious File Renaming",
                    Type = RuleType.Behavior,
                    Flags = RuleFlags.Active | RuleFlags.AlertOnly,
                    Pattern = ".*\\.\\w{3,10}\\.\\w{3,10}$",
                    Description = "Detects suspicious file renaming patterns",
                    MinFileSize = 0,
                    MaxFileSize = 100 * 1024 * 1024
                }
            };
        }

        public void SavePredefinedRules()
        {
            string predefinedPath = Path.Combine(rulesDirectory, "predefined_rules.json");
            string json = JsonSerializer.Serialize(predefinedRules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(predefinedPath, json);
        }

        public void LoadDynamicRules()
        {
            string dynamicPath = Path.Combine(rulesDirectory, "dynamic_rules.json");

            if (File.Exists(dynamicPath))
            {
                try
                {
                    string json = File.ReadAllText(dynamicPath);
                    dynamicRules = JsonSerializer.Deserialize<List<RuleStruct>>(json) ?? new List<RuleStruct>();
                }
                catch (Exception ex)
                {
                    dynamicRules = new List<RuleStruct>();
                }
            }
        }

        public void SaveDynamicRules()
        {
            string dynamicPath = Path.Combine(rulesDirectory, "dynamic_rules.json");
            string json = JsonSerializer.Serialize(dynamicRules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dynamicPath, json);
        }

        public bool AddDynamicRule(RuleStruct rule)
        {
            if (rule == null) return false;

            // Verifica se já existe uma regra com o mesmo nome
            if (dynamicRules.Exists(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            dynamicRules.Add(rule);
            SaveDynamicRules();
            return true;
        }

        public bool RemoveDynamicRule(Guid ruleId)
        {
            int removed = dynamicRules.RemoveAll(r => r.Id == ruleId);
            if (removed > 0)
            {
                SaveDynamicRules();
                return true;
            }
            return false;
        }

        public List<RuleStruct> GetAllRules()
        {
            var allRules = new List<RuleStruct>();
            allRules.AddRange(predefinedRules);
            allRules.AddRange(dynamicRules);
            return allRules;
        }

        public List<RuleStruct> GetActiveRules()
        {
            return GetAllRules().FindAll(r => r.Flags.HasFlag(RuleFlags.Active));
        }

        public byte[] SerializeRulesForDriver()
        {
            var activeRules = GetActiveRules();
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.Unicode);

            // Escreve o cabeçalho
            writer.Write(activeRules.Count); // NumberOfRules
            writer.Write(0); // TotalSize (será atualizado depois)

            int totalSize = sizeof(int) * 2; // Tamanho do cabeçalho

            foreach (var rule in activeRules)
            {
                var header = new SerializedRuleHeader
                {
                    Id = (uint)rule.Id.GetHashCode(),
                    Type = (uint)rule.Type,
                    Flags = (uint)rule.Flags,
                    RuleNameLength = (ushort)(rule.Name?.Length * 2 ?? 0),
                    TargetPathLength = (ushort)(rule.TargetPath?.Length * 2 ?? 0),
                    PatternLength = (uint)(rule.Pattern?.Length * 2 ?? 0),
                    MinFileSize = rule.MinFileSize,
                    MaxFileSize = rule.MaxFileSize
                };

                // Escreve o cabeçalho
                byte[] headerBytes = SerializeStruct(header);
                writer.Write(headerBytes);

                // Escreve os dados
                if (!string.IsNullOrEmpty(rule.Name))
                {
                    writer.Write(Encoding.Unicode.GetBytes(rule.Name));
                }
                if (!string.IsNullOrEmpty(rule.TargetPath))
                {
                    writer.Write(Encoding.Unicode.GetBytes(rule.TargetPath));
                }
                if (!string.IsNullOrEmpty(rule.Pattern))
                {
                    writer.Write(Encoding.Unicode.GetBytes(rule.Pattern));
                }

                totalSize += headerBytes.Length + header.RuleNameLength + header.TargetPathLength + (int)header.PatternLength;
            }

            // Atualiza o tamanho total
            ms.Position = sizeof(int); // Posição do TotalSize
            writer.Write(totalSize);

            return ms.ToArray();
        }

        private byte[] SerializeStruct<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(structure);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(structure, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SerializedRuleHeader
        {
            public uint Id;
            public uint Type;
            public uint Flags;
            public ushort RuleNameLength;
            public ushort TargetPathLength;
            public uint PatternLength;
            public uint MinFileSize;
            public uint MaxFileSize;
        }
    }
}