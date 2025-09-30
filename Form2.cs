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

        // Initialize form
        this.ClientSize = new Size(600, 400);
        this.Text = "Processing...";

        // Create and configure the RichTextBox
        this.richTextBox1 = new RichTextBox();
        this.richTextBox1.Dock = DockStyle.Fill;
        this.richTextBox1.Font = new Font("Consolas", 9);
        this.Controls.Add(this.richTextBox1);

        // Execute the operations
        ExecuteOperations();
    }

    private void ExecuteOperations()
    {
        richTextBox1.AppendText("Starting operations...\n");

        try
        {
            // Process files
            foreach (string filePath in selectedFilePaths)
            {
                string fileName = Path.GetFileName(filePath);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                richTextBox1.AppendText($"Moving file: {filePath}\n");

                // Move the file to destination
                File.Move(filePath, destinationPath);

                // Create symbolic link
                string cmdCommand = $"mklink \"{filePath}\" \"{destinationPath}\"";
                ExecuteCommand(cmdCommand);

                richTextBox1.AppendText($"Created symlink: {filePath} -> {destinationPath}\n");
            }

            // Process directories
            foreach (string dirPath in selectedDirectoryPaths)
            {
                string dirName = Path.GetFileName(dirPath);
                string destinationPath = Path.Combine(destinationFolder, dirName);

                richTextBox1.AppendText($"Moving directory: {dirPath}\n");

                // Move the directory to destination
                Directory.Move(dirPath, destinationPath);

                // Create symbolic link
                string cmdCommand = $"mklink /D \"{dirPath}\" \"{destinationPath}\"";
                ExecuteCommand(cmdCommand);

                richTextBox1.AppendText($"Created symlink: {dirPath} -> {destinationPath}\n");
            }

            richTextBox1.AppendText("\nAll operations completed successfully!\n");

            MessageBox.Show("All operations completed successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Close the form after completion
            this.Close();
        }
        catch (Exception ex)
        {
            richTextBox1.AppendText($"\nError occurred: {ex.Message}\n");
            MessageBox.Show($"Error: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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