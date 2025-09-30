using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public class Form2 : Form
{
    private List<string> selectedFilePaths;
    private List<string> selectedDirectoryPaths;
    private string destinationFolder;
    private RichTextBox richTextBox1;

    public Form2(List<string> filePaths, List<string> directoryPaths, string destFolder)
    {
        this.selectedFilePaths = filePaths;
        this.selectedDirectoryPaths = directoryPaths;
        this.destinationFolder = destFolder;
        InitializeConsoleForm();
        ExecuteOperations();
    }

    private void InitializeConsoleForm()
    {
        this.ClientSize = new Size(800, 600);
        this.Text = "Processing SymLinks...";
        this.BackColor = Color.Black;

        // Configuration of the window
        this.richTextBox1 = new RichTextBox();
        this.richTextBox1.Dock = DockStyle.Fill;
        this.richTextBox1.Font = new Font("Consolas", 10);
        this.richTextBox1.BackColor = Color.Black;
        this.richTextBox1.ForeColor = Color.LimeGreen;
        this.richTextBox1.ReadOnly = true;
        this.richTextBox1.WordWrap = false;
        this.Controls.Add(this.richTextBox1);
        this.Show();
    }

    private void ExecuteOperations()
    {
        richTextBox1.AppendText("Starting operations...\n");
        richTextBox1.AppendText("Note: This application may require administrator privileges to create symbolic links.\n\n");

        try
        {
            // Moves and links files
            MoveAndLink(false, selectedFilePaths);

            // Moves and links dirictories
            MoveAndLink(true, selectedDirectoryPaths);

            richTextBox1.AppendText("\nAll operations completed! (Some may have failed due to permissions)\n");
        }
        catch (Exception ex)
        {
            richTextBox1.AppendText($"\nCritical error occurred: {ex.Message}\n");

        }
    }
    //for bool - 0 is file and 1 is a folder
    private void MoveAndLink(bool fileOrFolder, List<string> listWithPaths)
    {
        foreach (string path in listWithPaths)
        {
            try
            {
                string name = Path.GetFileName(path);
                string destinationPath = Path.Combine(destinationFolder, name);

                switch (fileOrFolder)
                {
                    case false:
                        MoveFile(path, destinationPath);
                        richTextBox1.AppendText($"Moving file: {path}\n");

                        ExecuteCommand($"mklink \"{path}\" \"{destinationPath}\"");
                        break;
                    case true:
                        MoveDirectory(path, destinationPath);
                        richTextBox1.AppendText($"Moving directory: {path}\n");

                        ExecuteCommand($"mklink /D \"{path}\" \"{destinationPath}\"");
                        break;
                }
                richTextBox1.AppendText($"Created symlink: {path} -> {destinationPath}\n");

            }
            catch (UnauthorizedAccessException ex)
            {
                richTextBox1.AppendText($"Access denied for : {path}\n");
                richTextBox1.AppendText($"Error: {ex.Message}\n");
                richTextBox1.AppendText("You may need to run this application as Administrator.\n\n");
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"Error in processing {path}: {ex.Message}\n\n");
            }
        }    
    }

    private void MoveFile(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, true);
        File.Delete(sourcePath);
    }

    private void MoveDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        // Get all files in the source directory
        // Since I can't mobe files with Directory.Move() between disks, i had to implement this monstrocity
        foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            string relativePath = filePath.Substring(sourcePath.Length + 1);
            string destinationFilePath = Path.Combine(destinationPath, relativePath);

            string destinationFileDir = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(destinationFileDir))
            {
                Directory.CreateDirectory(destinationFileDir);
            }
            File.Copy(filePath, destinationFilePath, true);
        }

        Directory.Delete(sourcePath, true);
    }

    private void ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {command}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    richTextBox1.AppendText($"Output: {output}\n");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    richTextBox1.AppendText($"Error: {error}\n");
                }
            }
        }
        catch (Exception ex)
        {
            richTextBox1.AppendText($"Command execution failed: {ex.Message}\n");
        }
    }
}