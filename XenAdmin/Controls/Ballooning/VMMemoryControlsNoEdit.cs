﻿/* Copyright (c) Citrix Systems Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using XenAdmin.Dialogs;
using XenAdmin.Wizards;
using XenAPI;


namespace XenAdmin.Controls.Ballooning
{
    public partial class VMMemoryControlsNoEdit : VMMemoryControlsBase
    {
        public VMMemoryControlsNoEdit()
        {
            InitializeComponent();
        }

        public override List<VM> VMs
        {
            set
            {
                base.VMs = value;
                foreach (VM vm in vms)
                    vm.PropertyChanged += vm_PropertyChanged;
                foreach (VM_metrics metrics in vm_metrics)
                {
                    if (metrics != null)
                        metrics.PropertyChanged += vm_metrics_PropertyChanged;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (vms == null || vms.Count == 0)
                return;

            // Edit button: don't show if one of the VMs has just been rebooted, because we don't accurately
            // know what its capabilities are in that case (cf CA-31051). Also suspended VMs can't be edited.
            // So "good" VMs are ones which are halted, or running with known virtualisation status.
            editButton.Visible =
                (null == vms.Find(vm => !(vm.power_state == vm_power_state.Halted ||
                    vm.power_state == vm_power_state.Running && !vm.virtualisation_status.HasFlag(XenAPI.VM.VirtualisationStatus.UNKNOWN))));

            // Shiny bar
            vmShinyBar.Initialize(vm0, vms.Count > 1, CalcMemoryUsed(), false);

            // Spinners
            bool ballooning = vm0.has_ballooning;
            if (ballooning)
            {
                valueDynMin.Text = Util.MemorySizeStringSuitableUnits(vm0.memory_dynamic_min, true);
                valueDynMax.Text = Util.MemorySizeStringSuitableUnits(vm0.memory_dynamic_max, true);
                if (vm0.memory_dynamic_max == vm0.memory_static_max)
                    labelStatMax.Visible = valueStatMax.Visible = false;
                else
                    valueStatMax.Text = Util.MemorySizeStringSuitableUnits(vm0.memory_static_max, true);
            }
            else
            {
                valueDynMin.Text = Util.MemorySizeStringSuitableUnits(vm0.memory_static_max, true);
                iconBoxDynMin.Visible = false;
                labelDynMin.Text = Messages.MEMORY;

                iconBoxDynMax.Visible = labelDynMax.Visible = valueDynMax.Visible = false;
                labelStatMax.Visible = valueStatMax.Visible = false;
            }
        }

        void vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "power_state" || e.PropertyName == "virtualisation_status" || e.PropertyName == "name_label")
                Program.Invoke(this,Refresh);
        }

        void vm_metrics_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "memory_actual")
                Refresh();
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            if (vms.Count == 1)
            {
                if (vm0.advanced_ballooning)
                    (new BallooningDialogAdvanced(vm0)).ShowDialog();
                else
                    (new BallooningDialog(vm0)).ShowDialog();
            }
            else
                Program.MainWindow.ShowPerConnectionWizard(vm0.Connection, new BallooningWizard(vms));
        }
    }
}
