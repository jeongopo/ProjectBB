using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ExcelToXml
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, DataTable> currentSheets;
        private string selectedExcelPath;
        private Dictionary<string, string> excelFileMap = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
           var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = dialog.FileName;
                string folderPath = Path.GetDirectoryName(filePath);
                if(Directory.Exists(folderPath) == false)
                {
                    System.Windows.MessageBox.Show("경로가 유효하지 않습니다.\n현재 경로 : "+folderPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                System.Windows.MessageBox.Show("폴더를 선택했습니다.\n현재 경로 : " + folderPath, "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                var xlsxFiles = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories);
                var xlsFiles = Directory.GetFiles(folderPath, "*.xls", SearchOption.AllDirectories);

                // 둘 다 합치기
                var excelFiles = xlsxFiles.Concat(xlsFiles).ToArray();
                if(excelFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show("선택한 폴더에 엑셀이 없습니다.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                //fileList.ItemsSource = excelFiles;
                // 이름만 보여주기
                excelFileMap.Clear();
                excelFileMap = excelFiles.ToDictionary(f => Path.GetFileName(f), f => f);
                fileList.ItemsSource = excelFileMap.Keys.ToList();

                // 선택 시 전체 경로 사용
                fileList.SelectionChanged += (s, e) =>
                {
                    if (fileList.SelectedItem is string selectedName && excelFileMap.TryGetValue(selectedName, out var fullPath))
                    {
                        // 여기서 fullPath를 사용해 Excel 열기 등 작업
                        Console.WriteLine("선택한 전체 경로: " + fullPath);
                    }
                };
                btnSaveXml.Visibility = Visibility.Visible;
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
                sheetSelector.ItemsSource = currentSheets.Keys;
                sheetSelector.SelectedIndex = 0;
                sheetSelector.Visibility = Visibility.Visible;
                btnSaveXml.IsEnabled = true;
                if(currentSheets.Count > 1)
                {
                    sheetSelector.IsEnabled = true;
                }
                else
                {
                    sheetSelector.IsEnabled = false;
                }
            }
        }

        private void sheetSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sheetSelector.SelectedItem != null && currentSheets != null)
            {
                string sheetName = sheetSelector.SelectedItem.ToString();
                dataGrid.ItemsSource = currentSheets[sheetName].DefaultView;
            }
        }

        private void btnSaveXml_Click(object sender, RoutedEventArgs e)
        {
            if (sheetSelector.SelectedItem == null || currentSheets == null)
                return;

            string selectedSheet = sheetSelector.SelectedItem.ToString();
            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog { Filter = "XML Files (*.xml)|*.xml" };

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

                // Save the selected sheet to XML
                XmlHelper.SaveDataTableToXml(currentSheets[selectedSheet], saveDialog.FileName);
                System.Windows.MessageBox.Show("XML saved successfully!", "Done", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}