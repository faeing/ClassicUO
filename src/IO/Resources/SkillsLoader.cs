﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class SkillsLoader : UOFileLoader
    {
        private static SkillsLoader _instance;
        private UOFileMul _file;

        private SkillsLoader()
        {
        }

        public static SkillsLoader Instance => _instance ?? (_instance = new SkillsLoader());

        public int SkillsCount => Skills.Count;
        public readonly List<SkillEntry> Skills = new List<SkillEntry>();
        public readonly List<SkillEntry> SortedSkills = new List<SkillEntry>();

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    if (SkillsCount > 0)
                    {
                        return;
                    }

                    string path = UOFileManager.GetUOFilePath("skills.mul");
                    string pathidx = UOFileManager.GetUOFilePath("Skills.idx");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, 0, 16);
                    _file.FillEntries(ref Entries);

                    for (int i = 0, count = 0; i < Entries.Length; i++)
                    {
                        ref UOFileIndex entry = ref GetValidRefEntry(i);

                        if (entry.Length > 0)
                        {
                            _file.Seek(entry.Offset);
                            bool hasAction = _file.ReadBool();

                            string name = Encoding.UTF8.GetString
                                                      (_file.ReadArray<byte>(entry.Length - 1))
                                                  .TrimEnd('\0');

                            SkillEntry skill = new SkillEntry(count++, name, hasAction);

                            Skills.Add(skill);
                        }
                    }

                    SortedSkills.AddRange(Skills);
                    SortedSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
                }
            );
        }

        public int GetSortedIndex(int index)
        {
            if (index < SkillsCount)
            {
                return SortedSkills[index].Index;
            }

            return -1;
        }
    }

    internal class SkillEntry
    {
        public SkillEntry(int index, string name, bool hasAction)
        {
            Index = index;
            Name = name;
            HasAction = hasAction;
        }

        public bool HasAction;
        public readonly int Index;
        public string Name;

        public override string ToString()
        {
            return Name;
        }

        internal enum HardCodedName
        {
            Alchemy,
            Anatomy,
            AnimalLore,
            ItemID,
            ArmsLore,
            Parrying,
            Begging,
            Blacksmith,
            Bowcraft,
            Peacemaking,
            Camping,
            Carpentry,
            Cartography,
            Cooking,
            DetectHidden,
            Enticement,
            EvaluateIntelligence,
            Healing,
            Fishing,
            ForensicEvaluation,
            Herding,
            Hiding,
            Provocation,
            Inscription,
            Lockpicking,
            Magery,
            ResistingSpells,
            Tactics,
            Snooping,
            Musicanship,
            Poisoning,
            Archery,
            SpiritSpeak,
            Stealing,
            Tailoring,
            AnimalTaming,
            TasteIdentification,
            Tinkering,
            Tracking,
            Veterinary,
            Swordsmanship,
            MaceFighting,
            Fencing,
            Wrestling,
            Lumberjacking,
            Mining,
            Meditation,
            Stealth,
            Disarm,
            Necromancy,
            Focus,
            Chivalry,
            Bushido,
            Ninjitsu,
            Spellweaving
        }
    }
}