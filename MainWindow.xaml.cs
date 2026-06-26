using NicheStudioWeirdo.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NicheStudioWeirdo
{
    public partial class MainWindow : Window
    {
        private Button? activeTab = null;

        public MainWindow()
        {
            InitializeComponent();
            SettingsManager.Load();
            
            // Trigger first tab programmatically to initialize UI
            if (TabContainer.Children.Count > 0 && TabContainer.Children[0] is Button firstTab)
            {
                Nav_Click(firstTab, new RoutedEventArgs());
            }
            else
            {
                LoadView(new MinoriView());
                GuideText.Text = GetGuideText("Minori");
            }
        }

        // Mac Window Controls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        // Horizontal Tab Scrolling
        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                TabScrollViewer.LineLeft();
            else
                TabScrollViewer.LineRight();
            e.Handled = true;
        }

        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            TabScrollViewer.PageLeft();
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            TabScrollViewer.PageRight();
        }

        // Guide Toggle
        private void GuideToggle_Click(object sender, RoutedEventArgs e)
        {
            GuidePanel.Visibility = GuidePanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private string GetGuideText(string tag)
        {
            return tag switch
            {
                "Minori" => "Minori Engine Guide:\n1. Ensure you select the correct Game Index from the dropdown.\n2. To Unpack: Select the original .paz file and an empty output folder. Click Unpack.\n3. To Repack: Select the folder you modified and specify an output .paz filename. Click Repack. The index must match the game.",
                "WagaHime" => "WagaHime (ACV1) Guide:\n1. Target .dat Archive: Browse to the original .dat file you wish to unpack.\n2. Click Unpack to extract its contents.\n3. Extracted Folder: Browse to the folder containing your modified files.\n4. Click Repack. WARNING: Repacking requires the ORIGINAL .dat file to be selected in the first field as a reference to generate the new .dat.",
                "FSN" => "FSN Remastered Guide:\n1. Decompile: Select a .bin script file. It will be decompiled to a .json file for editing.\n2. Compile: Select your modified .json file. It will be compiled back into a .bin script.\n3. Do not modify the JSON structure, only edit the translation strings.",
                "WA2" => "WA2 Arch Guide:\n1. Select a .pak archive.\n2. Click Unpack to extract all contents to a folder.\n3. Edit the necessary files inside the extracted folder.\n4. To Repack, select the extracted folder and click Repack.",
                "AbogadoSdk" => "Abogado Script (SDK) Guide:\nWorkflow: Unpack DSK → Parse → Edit JSON → Inject → Repack DSK.\n1. Unpack DSK: Select scene.PFT, scene.DSK, and a Target Folder, then click Unpack.\n2. Parse SCF -> JSON: Select the extracted .SCF file, then click Parse.\n3. Edit JSON: Translate the text inside the resulting JSON file.\n4. Inject Translation: Select the translated .json and the original .SCF file, then click Inject.\n5. Repack DSK: Repack the folder containing the injected .SCF files into a new DSK archive.\n6. Verify Integrity: Confirm no files were corrupted after modification.",
                "AbogadoKg" => "Abogado Graphics (KG) Guide:\nWorkflow: Extract via GARbro → Edit PNG → Pack KG → Patch DSK.\n1. PNG Source Folder: Browse to the folder containing .png files extracted by GARbro.\n2. PNG -> KG Convert: Converts .png files to .KG format (saved in packed_kg/). Ensure kg_metadata.json is present.\n3. Target DSK + PFT: Select the GRAPHIC.DSK and GRAPHIC.PFT archive files.\n4. Patch DSK In-place: Injects modified .KG files into the archive without a full rebuild.\n5. Rebuild Full DSK: Rebuilds the .DSK archive from scratch when adding new files.",
                "HuneX" => "HuneX (TsukiRe) Guide:\n1. MRG Tool: Unpack .mrg archives to folders and repack them.\n2. TXT/CSV Tool: Convert script .txt files to .csv for translation, and repack .csv back to .txt.\n3. Graphics Tool: Convert .dtx / .txa images to .png. To encode back, you MUST select the original .dtx/.txa file as a reference alongside your edited .png.",
                "HuneXMahoyo" => "HuneX (Mahoyo) Guide:\n1. HFA Tool: Unpack .hfa archives to a folder, and repack the folder to a new .hfa.\n2. Single Files (.ctd, .cbg, .mzp): Use Decode/Decompress to extract script (.txt) or images (.png).\n3. Encode/Compress: Select your modified .txt/.png AND the original .ctd/.cbg/.mzp file. The original is strictly required as a reference to rebuild the file.",
                "BGI" => "BGI Translator Guide:\n1. Decompile: Select BGI script files to convert them to human-readable TXT.\n2. Compile: Rebuild the translated TXT back into BGI bytecode.",
                "MeltyBlood" => "TYPE-MOON (Melty Blood) Guide:\n1. Extract: Select a .p.sp archive to extract its contents.\n2. Pack: Select a folder containing your modified files and repack it into a .p.sp archive.",
                "CodeXR" => "CodeX R (Liar-soft) Guide:\n1. Operation Mode: Choose Single File or Batch Folder.\n2. Export: Converts .gsc bytecode to JSON.\n3. Edit JSON: ONLY edit the \"translated\" field. Do not modify \"original\" or remove control codes like ^ck.\n4. Import: Rebuilds the .gsc using the translated JSON. The script automatically handles standard encoding formats.",
                "Majikoai" => "Majikoi JAST Guide:\n1. Unpack: Select a .pac file to extract its contents to a directory.\n2. Repack: Select the directory to rebuild it into a .pac archive.",
                "Musicus" => "MUSICUS! (Yox) Guide:\n1. Unpack: Extract script_en.dat to a folder.\n2. Decrypt: Convert all encrypted game files into editable formats.\n3. Pack All: Converts edited files back. IMPORTANT: The output must go to the SAME folder where you decrypted them, because it relies on the manifest.json located there.\n4. Repack: Rebuild the folder back into a .dat archive.",
                "MalieKit" => "Malie Engine Guide:\n1. Decrypt/Extract: Unpack .dat or .lib archives to access their contents.\n2. Character Names: Export Names -> Edit -> Patch Names.\n3. Dialog: Export Dialog -> Edit -> Patch Dialog.\nIMPORTANT: Character Names must be patched FIRST before exporting/patching Dialog, as they are stored in separate segments.",
                "KKK" => "Kajiri Kamui Kagura (KKK) Guide:\n1. Install Base Patch: Select your game folder, pick Horizontal/Vertical layout, and install the required SVG, INI, and initial data6.dat.\n2. Wordwrap: Auto-wrap your translated message.txt.\n3. Compile Script: Compiles message.txt to exec.dat.\n4. Pack data6.dat: Packages the data folder into data6.dat which should then be copied to your game.",
                _ => "Select a tool from the top tabs to view its usage guide."
            };
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                // Reset previous active tab
                if (activeTab != null)
                {
                    activeTab.Background = Brushes.Transparent;
                    activeTab.Foreground = (SolidColorBrush)Application.Current.Resources["TextMutedBrush"];
                }

                // Set new active tab visually
                btn.Background = (SolidColorBrush)Application.Current.Resources["BgLighterBrush"];
                btn.Foreground = (SolidColorBrush)Application.Current.Resources["AccentBlueBrush"];
                activeTab = btn;

                UserControl view = btn.Tag.ToString() switch
                {
                    "Minori"    => new MinoriView(),
                    "WagaHime"  => new WagaHimeView(),
                    "FSN"       => new FSNRemasteredView(),
                    "WA2"       => new WA2ArchView(),
                    "AbogadoSdk"=> new AbogadoSdkView(),
                    "AbogadoKg" => new AbogadoKgView(),
                    "HuneX"     => new HuneXView(),
                    "HuneXMahoyo" => new HuneXMahoyoView(),
                    "BGI"       => new BGIView(),
                    "MeltyBlood"=> new MeltyBloodView(),
                    "CodeXR"    => new CodeXRView(),
                    "Majikoai"  => new MajikaiView(),
                    "Musicus"   => new MusicusView(),
                    "MalieKit"  => new MalieKitView(),
                    "KKK"       => new KKKView(),
                    _           => new MinoriView()
                };
                LoadView(view);
                GuideText.Text = GetGuideText(btn.Tag?.ToString() ?? "");
            }
        }

        private void LoadView(UserControl view)
        {
            MainContent.Content = view;
            LogToConsole($"Navigated to {view.GetType().Name}.");
        }

        public void LogToConsole(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ConsoleOutput.Text += $"\n> {message}";
                ConsoleScroll.ScrollToEnd();
            });
        }
    }
}