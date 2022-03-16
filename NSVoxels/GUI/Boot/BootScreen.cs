using NSVoxels.Globals;
using NSVoxels.Globals.Mappings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSVoxels.GUI.Boot
{
    public partial class BootScreen : Form
    {
        Thread gameThread = new Thread(() =>
        {
            using (var game = new Game1())
                game.Run();
        });

        private bool gameStarted;

        public BootScreen()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.gameStarted = true;
            this.Close();
        }

        private void BootScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!gameStarted) return;

            gameThread.Start();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            PreStartSettings.UseVSync = checkBox1.Checked;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreStartSettings.ResolutionIndex = comboBox1.SelectedIndex;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreStartSettings.VolumeSize = GUIIndexMapping.VolumeSizes[comboBox2.SelectedIndex];
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreStartSettings.MinimumAcceleratorNodeSize = GUIIndexMapping.MinimumAcceleratorNodeSize[comboBox3.SelectedIndex];
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            PreStartSettings.KernelIndex = comboBox4.SelectedIndex;
        }

        private void BootScreen_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 1;
            comboBox2.SelectedIndex = 3;
            comboBox3.SelectedIndex = 1;
            comboBox4.SelectedIndex = 0;

            checkBox1.Checked = true;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            PreStartSettings.UseDoubleBufferedVoxelData = checkBox2.Checked;
        }
    }
}
