﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated
{
    public class Config<TData> where TData : ConfigData, new()
    {
        public readonly string directory;
        public TData Data { get; private set; }

        public Config(string directory, string filename)
        {
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            this.directory = directory + filename;

            if (!File.Exists(this.directory))
                LoadDefaults();
            else
                Reload();
        }
        public void Save()
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                serializer.Serialize(writer, Data);
                writer.Close();
                file.Close();
                file.Dispose();
            }
            catch (Exception ex)
            {
                writer.Close();
                file.Close();
                file.Dispose();
                throw ex;
            }
        }
        public void Reload()
        {
            StreamReader r = File.OpenText(directory);
            try
            {
                string json = r.ReadToEnd();
                Data = JsonConvert.DeserializeObject<TData>(json);

                r.Close();
                r.Dispose();
            }
            catch (Exception ex)
            {
                if (r != default)
                {
                    r.Close();
                    r.Dispose();
                }
                throw new JSONSaver<TData>.JSONReadException(r, directory, ex);
            }
        }
        public void LoadDefaults()
        {
            StreamWriter file = File.CreateText(directory);
            JsonWriter writer = new JsonTextWriter(file);
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };
            try
            {
                Data = new TData();
                Data.SetDefaults();

                serializer.Serialize(writer, Data);
                writer.Close();
                file.Close();
                file.Dispose();
            }
            catch (Exception ex)
            {
                writer.Close();
                file.Close();
                file.Dispose();
                throw ex;
            }
        }
    }

    public abstract class ConfigData
    {
        public ConfigData() => SetDefaults();
        public abstract void SetDefaults();
    }
}
