﻿using System;
using System.IO;

namespace AtlusLibSharp.Persona3.Archives
{
    using Generic.Archives;
    using System.Collections.Generic;

    public class GenericVitaArchive : BinaryFileBase, IArchive
    {
        // Fields
        private List<GenericVitaArchiveEntry> _entries;

        // Constructors
        public GenericVitaArchive(string path)
        {
            if (!VerifyFileType(path))
            {
                throw new InvalidDataException("Not a valid ARCArchive.");
            }

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                InternalRead(reader);
            }
        }

        public GenericVitaArchive(Stream stream)
        {
            if (!VerifyFileType(stream))
            {
                throw new InvalidDataException("Not a valid ARCArchive.");
            }

            using (BinaryReader reader = new BinaryReader(stream))
            {
                InternalRead(reader);
            }
        }

        public GenericVitaArchive()
        {
            _entries = new List<GenericVitaArchiveEntry>();
        }

        // Properties
        public int EntryCount
        {
            get { return _entries.Count; }
        }

        public List<GenericVitaArchiveEntry> Entries
        {
            get { return _entries; }
        }

        // static methods
        public static GenericVitaArchive Create(string directorypath)
        {
            GenericVitaArchive arc = new GenericVitaArchive();
            string[] filepaths = Directory.GetFiles(directorypath);

            arc._entries = new List<GenericVitaArchiveEntry>(filepaths.Length);

            for (int i = 0; i < arc.EntryCount; i++)
            {
                arc._entries[i] = new GenericVitaArchiveEntry(filepaths[i]);
            }

            return arc;
        }

        public static GenericVitaArchive From(GenericPAK pak)
        {
            GenericVitaArchive arc = new GenericVitaArchive();
            foreach (GenericPAKEntry entry in pak.Entries)
            {
                arc.Entries.Add(new GenericVitaArchiveEntry(entry.Name, entry.Data));
            }
            return arc;
        }

        public static bool VerifyFileType(string path)
        {
            return InternalVerifyFileType(File.OpenRead(path));
        }

        public static bool VerifyFileType(Stream stream)
        {
            return InternalVerifyFileType(stream);
        }

        private static bool InternalVerifyFileType(Stream stream)
        {
            // check stream length
            if (stream.Length <= 4 + GenericVitaArchiveEntry.NAME_LENGTH + 4)
                return false;

            byte[] testData = new byte[4 + GenericVitaArchiveEntry.NAME_LENGTH + 4];
            stream.Read(testData, 0, 4 + GenericVitaArchiveEntry.NAME_LENGTH + 4);
            stream.Position = 0;
            int numOfFiles = BitConverter.ToInt32(testData, 0);

            // num of files sanity check
            if (numOfFiles > 1024 || numOfFiles < 1)
                return false;

            // check if the name field is correct
            bool nameTerminated = false;
            for (int i = 0; i < GenericVitaArchiveEntry.NAME_LENGTH; i++)
            {
                if (testData[4 + i] == 0x00)
                    nameTerminated = true;

                if (testData[4 + i] != 0x00 && nameTerminated == true)
                    return false;
            }

            // first entry length sanity check
            int length = BitConverter.ToInt32(testData, 4 + GenericVitaArchiveEntry.NAME_LENGTH - 1);
            if (length >= (1024 * 1024 * 100) || length < 0)
            {
                return false;
            }

            return true;
        }

        // instance methods
        internal override void InternalWrite(BinaryWriter writer)
        {
            writer.Write(_entries.Count);
            foreach (GenericVitaArchiveEntry entry in _entries)
            {
                entry.InternalWrite(writer);
            }
        }

        private void InternalRead(BinaryReader reader)
        {
            int numEntries = reader.ReadInt32();
            _entries = new List<GenericVitaArchiveEntry>(numEntries);
            for (int i = 0; i < numEntries; i++)
            {
                _entries.Add(new GenericVitaArchiveEntry(reader));
            }
        }

        IArchiveEntry IArchive.GetEntry(int index)
        {
            return _entries[index];
        }
    }
}
