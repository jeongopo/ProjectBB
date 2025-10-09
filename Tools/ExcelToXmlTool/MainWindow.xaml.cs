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
    public enum BBErrorCode
    {
        Success = 1,
        NoData = 0,
        DataValue = 10,
        TypeCheck = 11,
        InvalidData = 12,
        Path = 20,
        EnumPath = 21,
        StructPath = 22,
        XmlPath = 23,
        FolderPath = 24,
        CodePath = 25,
        Cancel = 100,
    }

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

        BBErrorCode RefreshFileList(string folderPath)
        {
            selectedFolderPath = folderPath;
            if (Directory.Exists(folderPath) == false)
            {
                return BBErrorCode.FolderPath;
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
            return BBErrorCode.Success;
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

                BBErrorCode Result = RefreshFileList(folderPath);
                if(Result != BBErrorCode.Success)
                {
                    PrintErrorCode(Result);
                    return;
                } 
            }   
        }

        private void btnSelectXMLFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.Title = "Select XML File";
            dialog.CheckFileExists = true;
            dialog.InitialDirectory = selectedXMLPath;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = dialog.FileName;
                string folderPath = Path.GetDirectoryName(filePath);

                selectedXMLPath = folderPath;
                txtXMLFolderPath.Content = "현재 XML 저장 경로 : " + selectedXMLPath;
            }
        }

        private void btnSelectCodeFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "Code Files (*.cs)|*.cs";
            dialog.InitialDirectory = selectedCodePath;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = dialog.FileName;
                string folderPath = Path.GetDirectoryName(filePath);

                selectedCodePath = folderPath;
                if (Directory.Exists(selectedCodePath) == false)
                {
                    System.Windows.MessageBox.Show("경로가 유효하지 않습니다.\n현재 경로 : " + selectedCodePath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                txtDataCodePath.Content = "현재 코드 저장 경로 : " + selectedCodePath;
            }
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
            {
                return;
            }

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
                    var result = System.Windows.MessageBox.Show("동일한 이름의 XML 파일이 이미 존재합니다. 덮어쓰시겠습니까?", "File Exists", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;
                }
                saveDialog.FileName = Path.GetFullPath(saveDialog.FileName); // 절대 경로로 변환
                selectedXMLPath = Path.GetDirectoryName(saveDialog.FileName);
                txtXMLFolderPath.Content = "현재 XML 저장 경로 : " + selectedXMLPath;

                // Save the selected sheet to XML
                BBErrorCode saveResult = XmlHelper.SaveDataTableToXml(currentSheets.First().Value, saveDialog.FileName);
                if (saveResult == BBErrorCode.Success)
                {
                    System.Windows.MessageBox.Show("XML saved successfully!", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    PrintErrorCode(saveResult);
                }
            }
        }
        void btnGenerateStruct_Click(object sender, RoutedEventArgs e)
        {
            BBErrorCode Result = GenerateStruct();
            if (Result != BBErrorCode.Success)
            {
                PrintErrorCode(Result);
                return;
            }

            System.Windows.MessageBox.Show("구조체 코드를 저장했습니다.", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        BBErrorCode GenerateStruct(bool bForce = false)
        {
            string folderPath = selectedCodePath;
            if (Directory.Exists(folderPath) == false)
            {
                return BBErrorCode.FolderPath;
            }
            List<string> structNames = new List<string>();
            string structCode = XmlHelper.GenerateStructFromXml(selectedXMLPath, out structNames);
            if (string.IsNullOrEmpty(structCode))
            {
                return BBErrorCode.InvalidData;
            }

            string structFileName = "DataDrivenDefines.cs";
            string structFilePath = Path.Combine(selectedCodePath, structFileName);
            if (!bForce && File.Exists(structFilePath))
            {
                var result = System.Windows.MessageBox.Show("구조체 C# 파일이 이미 존재합니다. 덮어쓰시겠습니까?", "File Exists", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    return BBErrorCode.Cancel;
                }
            }

            string savePath = Path.GetFullPath(structFilePath); // 절대 경로로 변환
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
                            + "using UnityEngine;\n"
                            + "using DataEnumDefines;\n";
            structCode = usingCode + structCode;

            File.WriteAllText(savePath, structCode);
            return BBErrorCode.Success;
        }

        void btnGenerateEnum_Click(object sender, RoutedEventArgs e)
        {
            BBErrorCode Result = GenerateEnum();
            if (Result != BBErrorCode.Success)
            {
                PrintErrorCode(Result);
                return;
            }
            System.Windows.MessageBox.Show("Enum 코드를 저장했습니다.", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        BBErrorCode GenerateEnum(bool bForce = false)
        {
            string folderPath = selectedCodePath;
            if (Directory.Exists(folderPath) == false)
            {
                return BBErrorCode.FolderPath;
            }

            ExcelHelper.GenerateEnumFromExcel(Path.Combine(selectedFolderPath, "DataEnumDefines.xlsx"), out string enumCode);
            if (string.IsNullOrEmpty(enumCode))
            {
                return BBErrorCode.InvalidData;
            }
            string enumFileName = "DataEnumDefines.cs";
            string enumFilePath = Path.Combine(selectedCodePath, enumFileName);
            if (!bForce && File.Exists(enumFilePath))
            {
                var result = System.Windows.MessageBox.Show("Enum C# 파일이 이미 존재합니다. 덮어쓰시겠습니까?", "File Exists", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes)
                    return BBErrorCode.Cancel;
            }
            string savePath = Path.GetFullPath(enumFilePath); // 절대 경로로 변환
            if (File.Exists(savePath))
            {
                File.Delete(savePath); // 기존 파일 삭제
            }

            // Save the enum code to the file
            File.WriteAllText(savePath, enumCode);
            return BBErrorCode.Success;
        }

        void btnAllGenerator_Click(object sender, RoutedEventArgs e)
        {
            BBErrorCode Result = GenerateAll();
            if (Result != BBErrorCode.Success)
            {
                PrintErrorCode(Result);
                return;
            }
            else
            {
                System.Windows.MessageBox.Show("전체 변환을 완료했습니다.", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        void PrintErrorCode(BBErrorCode error)
        {
            switch (error)
            {
                case BBErrorCode.Success:
                    // 성공한 경우 별도의 메시지 없음
                    break;
                case BBErrorCode.NoData:
                    System.Windows.MessageBox.Show("저장할 데이터가 없습니다.", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    break;
                case BBErrorCode.DataValue:
                    System.Windows.MessageBox.Show("데이터 값이 유효하지 않습니다. 데이터를 확인해주세요.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.TypeCheck:
                    System.Windows.MessageBox.Show("데이터 타입이 유효하지 않습니다. 데이터를 확인해주세요.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.InvalidData:
                    System.Windows.MessageBox.Show("구조체 코드 생성에 실패했습니다. XML 경로나 내용을 확인해주세요.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.FolderPath:
                    System.Windows.MessageBox.Show("폴더 경로가 유효하지 않습니다.\n현재 경로 : " + selectedFolderPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.XmlPath:
                    System.Windows.MessageBox.Show("XML 경로가 유효하지 않습니다.\n현재 경로 : " + selectedXMLPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.CodePath:
                    System.Windows.MessageBox.Show("경로가 유효하지 않습니다.\n현재 경로 : " + selectedCodePath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
                case BBErrorCode.Cancel:
                    // 취소한 경우 별도의 메시지 없음
                    break;
                default:
                    System.Windows.MessageBox.Show("알 수 없는 오류가 발생했습니다.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    break;
            }
        }

        BBErrorCode GenerateAll()
        {
            RefreshFileList(selectedFolderPath);
            // 전체 테이블 읽어와서 엑셀에 로드
            var xlsxFiles = Directory.GetFiles(selectedFolderPath, "*.xlsx", SearchOption.AllDirectories);
            var xlsFiles = Directory.GetFiles(selectedFolderPath, "*.xls", SearchOption.AllDirectories);
            foreach (var file in xlsFiles.Concat(xlsxFiles))
            {
                if (file.Contains("DataEnumDefines"))
                {
                    continue; // Enum 파일은 건너뜀
                }
                var dataSet = ExcelHelper.LoadAllSheets(file);
                foreach (DataTable dataTable in dataSet.Tables)
                {
                    currentSheets = new Dictionary<string, DataTable> { { dataTable.TableName, dataTable } };
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    string xmlFileName = fileNameWithoutExt + ".xml";
                    string xmlFilePath = Path.Combine(selectedXMLPath, xmlFileName);

                    BBErrorCode saveResult = XmlHelper.SaveDataTableToXml(currentSheets.First().Value, xmlFilePath);
                    if (saveResult != BBErrorCode.Success)
                    {
                        return saveResult;
                    }
                }
            }

            // Generate struct code
            GenerateStruct(true);

            // Generate enum code
            GenerateEnum(true);

            return BBErrorCode.Success;
        }
    }
}