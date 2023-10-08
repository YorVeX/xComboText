// SPDX-FileCopyrightText: © 2019-2023 YorVeX, https://github.com/YorVeX
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace xComboText
{
  public struct FileComboInputFile
  {
    public string InputFile { get; set; }
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string ReplaceSearch { get; set; }
    public string ReplaceWith { get; set; }
    public string EmptyText { get; set; }
    public bool SkipIfEmpty { get; set; }
  }

  public class FileCombo
  {
    public string OutputFile { get; set; }
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string Separator { get; set; }
    public string EmptyText { get; set; }
    [XmlArray]
    [XmlArrayItem(ElementName = "InputFile")]
    public List<FileComboInputFile> InputFiles { get; set; }

    public FileCombo()
    {
      Separator = ", ";
    }

    public FileCombo(string outputFile, string prefix, string suffix, string separator, string emptyText, List<FileComboInputFile> inputFiles)
    {
      OutputFile = outputFile;
      Prefix = prefix;
      Suffix = suffix;
      Separator = separator;
      EmptyText = emptyText;
      InputFiles = inputFiles;
    }
  }

  public class FileCombosSettings
  {
    [XmlArray]
    [XmlArrayItem(ElementName = "FileCombo")]
    public List<FileCombo> FileComboList { get; set; }

    // the XML serializer always uses this to load from settings, so here's where default values are set
    public FileCombosSettings()
    {
    }
  }

  public class MiscSettings
  {
    public string Dummy { get; set; }

    // the XML serializer always uses this to load from settings, so here's where default values are set
    public MiscSettings()
    {
      Dummy = "";
    }
  }

  public class Configuration
  {
    public Settings Settings { get; set; }

    public Configuration()
    {
      Settings = new Settings();
    }

    public Configuration(Settings settings)
    {
      Settings = settings;
    }
  }

  /// <summary>
  /// Description of Settings.
  /// </summary>
  public class Settings
  {
    public FileCombosSettings FileCombos { get; set; }
    public MiscSettings Misc { get; set; }

    public Settings()
    {
      FileCombos = new FileCombosSettings();
      Misc = new MiscSettings();
    }

    public void LoadFromFile(string fileName)
    {
      if (!File.Exists(fileName))
        return;
      Configuration loadConfig = new Configuration(this);
      using (TextReader textReader = new StreamReader(fileName))
        loadConfig = (Configuration)new XmlSerializer(loadConfig.GetType()).Deserialize(textReader);

      FileCombos = loadConfig.Settings.FileCombos;
      Misc = loadConfig.Settings.Misc;
    }

    public void SaveToFile(string fileName)
    {
      Configuration config = new Configuration(this);
      using (XmlTextWriter xmlWriter = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
      {
        xmlWriter.Formatting = Formatting.Indented;
        new XmlSerializer(config.GetType()).Serialize(xmlWriter, config);
      }
    }
  }
}

