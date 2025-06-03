using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcelToXml
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, DataTable> currentSheets;
        private string selectedExcelPath;
        private Dictionary<string, string> excelFileMap = new();
        private string selectedFolderPath = Environment.CurrentDirectory;
        private string selectedXMLPath = Environment.CurrentDirectory;
        private string selectedCodePath = Environment.CurrentDirectory;
        private const string ConfigFileName = "XMLConvertConfig.json";

        public class ToolConfig
        {
            public string ExcelFolderPath { get; set; } = Environment.CurrentDirectory;
            public string XmlFolderPath { get; set; } = Environment.CurrentDirectory;
            public string CodeFolderPath { get; set; } = Environment.CurrentDirectory;
        }

        public MainWindow()
        {
            InitializeComponent();
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (!File.Exists(configFile))
            {
                var tempconfig = new ToolConfig();
                SaveConfig(configFile, tempconfig);
            }

            var config = LoadConfig(configFile);

            selectedFolderPath = config.ExcelFolderPath ?? Environment.CurrentDirectory;
            selectedXMLPath = config.XmlFolderPath ?? Environment.CurrentDirectory;
            selectedCodePath = config.CodeFolderPath ?? Environment.CurrentDirectory;
            txtExcelFolderPath.Content = "현재 엑셀 저장 경로 : " + selectedFolderPath;
            txtXMLFolderPath.Content = "현재 XML 저장 경로 : " + selectedXMLPath;
            txtDataCodePath.Content = "현재 코드 저장 경로 : " + selectedCodePath;

            this.Closing += MainWindow_Closing;
            RefreshFileList(selectedFolderPath);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            var config = new ToolConfig
            {
                ExcelFolderPath = selectedFolderPath,
                XmlFolderPath = selectedXMLPath,
                CodeFolderPath = selectedCodePath
            };
            SaveConfig(configFile, config);
        }

        public static ToolConfig LoadConfig(string path)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ToolConfig>(json) ?? new ToolConfig();
            }
            return new ToolConfig(); // 기본값
        }

        public static void SaveConfig(string path, ToolConfig config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        void RefreshFileList(string folderPath)
        {
            selectedFolderPath = folderPath;
            if (Directory.Exists(folderPath) == false)
            {
                System.Windows.MessageBox.Show("경로가 유효하지 않습니다.\n현재 경로 : " + folderPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            txtExcelFolderPath.Content = "현재 엑셀 저장 경로 : " + folderPath;

            var xlsxFiles = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories);
            var xlsFiles = Directory.GetFiles(folderPath, "*.xls", SearchOption.AllDirectories);

            var excelFiles = xlsxFiles.Concat(xlsFiles).ToArray();
            excelFileMap.Clear();
            excelFileMap = excelFiles.ToDictionary(f => Path.GetFileName(f), f => f);
            fileList.ItemsSource = excelFileMap.Keys.ToList();

            // 선택 시 전체 경로 사용
            fileList.SelectionChanged += (s, e) =>
            {
                if (fileList.SelectedItem is string selectedName && excelFileMap.TryGetValue(selectedName, out var fullPath))
                {
                    Console.WriteLine("선택한 전체 경로: " + fullPath);
                }
            };
            btnSaveXml.Visibility = Visibility.Visible;
        }
        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls";
            dialog.InitialDirectory = selectedFolderPath;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = dialog.FileName;
                string folderPath = Path.GetDirectoryName(filePath);
                RefreshFileList(folderPath);
            }

            /*  
            //폴더 기준 처리          
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = "Select a folder containing Excel files",
                    ShowNewFolderButton = false,
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = openFileDialog.SelectedPath;
                    var excelFiles = Directory.GetFiles(folderPath, "*.xlsx|*.xls", SearchOption.AllDirectories);
                    fileList.ItemsSource = excelFiles;
                }
                */
        }

        private void fileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fileList.SelectedItem is string selectedName && excelFileMap.TryGetValue(selectedName, out var fullPath))
            {
                string filePath = fullPath;
                selectedExcelPath = filePath;
                var dataSet = ExcelHelper.LoadAllSheets(filePath);
                currentSheets = dataSet.Tables.Cast<DataTable>().ToDictionary(t => t.TableName, t => t);
                dataGrid.ItemsSource = currentSheets.First().Value.DefaultView;
                btnSaveXml.IsEnabled = true;
            }
        }

        private void btnSaveXml_Click(object sender, RoutedEventArgs e)
        {
            if (currentSheets == null)
                return;

            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog { Filter = "XML Files (*.xml)|*.xml" };
            saveDialog.InitialDirectory = selectedXMLPath;
            saveDialog.FileName = Path.GetFileNameWithoutExtension(selectedExcelPath) + ".xml"; // 기본 파일 이름 설정
            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(saveDialog.FileName))
                {
                    System.Windows.MessageBox.Show("Please select a valid file name.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if (File.Exists(saveDialog.FileName))
                {
                    var result = System.Windows.MessageBox.Show("File already exists. Do you want to overwrite?", "File Exists", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                saveDialog.FileName = Path.GetFullPath(saveDialog.FileName); // 절대 경로로 변환
                selectedXMLPath = Path.GetDirectoryName(saveDialog.FileName);
                txtXMLFolderPath.Content = "현재 XML 저장 경로 : " + selectedXMLPath;

                // Save the selected sheet to XML
                int saveResult = XmlHelper.SaveDataTableToXml(currentSheets.First().Value, saveDialog.FileName);
                if (saveResult == -1)
                {
                    System.Windows.MessageBox.Show("Invalid data type in the first column. Please check the data.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                else if (saveResult == 0)
                {
                    System.Windows.MessageBox.Show("No data to save.", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }
                else
                {
                    System.Windows.MessageBox.Show("XML saved successfully!", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }
        void btnGenerateStruct_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select a folder containing Save Data files",
                ShowNewFolderButton = true,
                SelectedPath = selectedCodePath
            };
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = openFolderDialog.SelectedPath;
                selectedCodePath = folderPath;
                if (Directory.Exists(folderPath) == false)
                {
                    System.Windows.MessageBox.Show("경로가 유효하지 않습니다.\n현재 경로 : " + folderPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                txtDataCodePath.Content = "현재 엑셀 저장 경로 : " + selectedCodePath;
                List<string> structNames = new List<string>();
                string structCode = XmlHelper.GenerateStructFromXml(selectedXMLPath, out structNames);
                if (string.IsNullOrEmpty(structCode))
                {
                    System.Windows.MessageBox.Show("Failed to generate structure code.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                string structFileName = "DataDrivenDefines.cs";
                string structFilePath = Path.Combine(selectedCodePath, structFileName);
                if (File.Exists(structFilePath))
                {
                    var result = System.Windows.MessageBox.Show("Structure file already exists. Do you want to overwrite?", "File Exists", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                }

                string savePath = Path.GetFullPath(structFilePath); // 절대 경로로 변환
                txtDataCodePath.Content = "현재 코드 저장 경로 : " + selectedCodePath;
                if (!Directory.Exists(selectedCodePath))
                {
                    Directory.CreateDirectory(selectedCodePath);
                }
                // Save the structure code to the file
                if (File.Exists(savePath))
                {
                    File.Delete(savePath); // 기존 파일 삭제
                }

                string classHeaderCode = "public class DataStorage\n{\n";

                
                for (int i = 0; i < structNames.Count; i++)
                {
                    classHeaderCode += $"\tpublic Dictionary<string,{structNames[i]}> {structNames[i]}Data;\n";
                }
                classHeaderCode += "\tpublic void LoadData()\n"
                                + "\t{\n";
                for (int i = 0; i < structNames.Count; i++)
                {
                    classHeaderCode += $"\t\t{structNames[i]}Data = DataManager.LoadDefineData<{structNames[i]}>(\"{structNames[i]}\");\n";
                }
                classHeaderCode += "\t}\n";

                structCode = classHeaderCode
                            + "\t// classDefine\n"
                            + structCode
                            + "}\n";
                string usingCode = "// This file is auto-generated from XML files.\n"
                                + "using System;\n"
                                + "using System.IO;\n"
                                + "using System.Xml.Serialization;\n"
                                + "using System.Collections.Generic;\n"
                                + "using UnityEngine;\n";
                structCode = usingCode + structCode;

                File.WriteAllText(savePath, structCode);
            }
        }
    }
}