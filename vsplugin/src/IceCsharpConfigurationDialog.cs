// **********************************************************************
//
// Copyright (c) 2009 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// LICENSE file included in this distribution.
//
// **********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using EnvDTE;

namespace Ice.VisualStudio
{
    public partial class IceCsharpConfigurationDialog : Form
    {
        public IceCsharpConfigurationDialog(Project project)
        {
            InitializeComponent();
            _project = project;
            
            //
            // Set the toolTip messages.
            //
            toolTip.SetToolTip(txtIceHome, "Ice installation directory.");
            toolTip.SetToolTip(btnSelectIceHome, "Ice installation directory.");
            toolTip.SetToolTip(chkStreaming, "Generate marshaling support for stream API (--stream).");
            toolTip.SetToolTip(chkChecksum, "Generate checksums for Slice definitions (--checksum).");
            toolTip.SetToolTip(chkIcePrefix, "Permit Ice prefixes (--ice).");
            toolTip.SetToolTip(chkTie, "Generate TIE classes (--tie).");
            toolTip.SetToolTip(chkConsole, "Enable console output.");
            
            toolTip.SetToolTip(btnClose, "Close without save configuration changes.");

            if(_project != null)
            {
                this.Text = "Ice Configuration - Project: " + _project.Name;
                bool enabled = Util.isSliceBuilderEnabled(project);
                setEnabled(enabled);
                chkEnableBuilder.Checked = enabled;
                load();
                _initialized = true;
                _changed = false;
            }
        }
        
        private void load()
        {
            if(_project != null)
            {
                System.Windows.Forms.Cursor c = Cursor.Current;
                Cursor = Cursors.WaitCursor;
                includeDirList.Items.Clear();
                txtIceHome.Text = Util.getIceHomeRaw(_project);
                txtExtraOptions.Text = Util.getProjectProperty(_project, Util.PropertyNames.IceExtraOptions);

                chkIcePrefix.Checked = Util.getProjectPropertyAsBool(_project, Util.PropertyNames.IcePrefix);
                chkTie.Checked = Util.getProjectPropertyAsBool(_project, Util.PropertyNames.IceTie);
                chkStreaming.Checked = Util.getProjectPropertyAsBool(_project, Util.PropertyNames.IceStreaming);
                chkChecksum.Checked = Util.getProjectPropertyAsBool(_project, Util.PropertyNames.IceChecksum);
                chkConsole.Checked = Util.getProjectPropertyAsBool(_project, Util.PropertyNames.ConsoleOutput);
                
                IncludePathList list =
                    new IncludePathList(Util.getProjectProperty(_project, Util.PropertyNames.IceIncludePath));
                foreach(String s in list)
                {
                    includeDirList.Items.Add(s.Trim());
                    if(Path.IsPathRooted(s.Trim()))
                    {
                        includeDirList.SetItemCheckState(includeDirList.Items.Count - 1, CheckState.Checked);
                    }
                }

                ComponentList selectedComponents = Util.getIceCSharpComponents(_project);
                foreach(String s in Util.ComponentNames.cSharpNames)
                {
                    if(String.IsNullOrEmpty(selectedComponents.Find(delegate(string d)
                                                    {
                                                        return d.Equals(s, StringComparison.CurrentCultureIgnoreCase);
                                                    })))
                    {
                        checkComponent(s, false);
                    }
                    else
                    {
                        checkComponent(s, true);
                    }
                }
                Cursor = c;
            }      
        }

        private void checkComponent(String component, bool check)
        {
            switch (component)
            {
            case "Glacier2":
            {
                chkGlacier2.Checked = check;
                break;
            }
            case "Ice":
            {
                chkIce.Checked = check;
                break;
            }
            case "IceBox":
            {
                chkIceBox.Checked = check;
                break;
            }
            case "IceGrid":
            {
                chkIceGrid.Checked = check;
                break;
            }
            case "IcePatch2":
            {
                chkIcePatch2.Checked = check;
                break;
            }
            case "IceSSL":
            {
                chkIceSSL.Checked = check;
                break;
            }
            case "IceStorm":
            {
                chkIceStorm.Checked = check;
                break;
            }
            default:
            {
                break;
            }
            }
        }
        private void chkEnableBuilder_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            if(_initialized)
            {
                _initialized = false;
                setEnabled(false);
                chkEnableBuilder.Enabled = false;
                Builder builder = Connect.getBuilder();
                if(chkEnableBuilder.Checked)
                {
                    builder.addBuilderToProject(_project);
                }
                else
                {
                    builder.removeBuilderFromProject(_project);
                }
                load();
                setEnabled(chkEnableBuilder.Checked);
                chkEnableBuilder.Enabled = true;
                _initialized = true;
            }
            Cursor = c;
        }
        
        private void setEnabled(bool enabled)
        {
            Util.setProjectProperty(_project, Util.PropertyNames.Ice, enabled.ToString());
            txtIceHome.Enabled = enabled;
            btnSelectIceHome.Enabled = enabled;

            chkIcePrefix.Enabled = enabled;
            chkTie.Enabled = enabled;
            chkStreaming.Enabled = enabled;
            chkChecksum.Enabled = enabled;
            chkConsole.Enabled = enabled;
            includeDirList.Enabled = enabled;
            btnAddInclude.Enabled = enabled;
            btnEditInclude.Enabled = enabled;
            btnRemoveInclude.Enabled = enabled;
            btnMoveIncludeUp.Enabled = enabled;
            btnMoveIncludeDown.Enabled = enabled;

            txtExtraOptions.Enabled = enabled;

            chkGlacier2.Enabled = enabled;
            chkIce.Enabled = enabled;
            chkIceBox.Enabled = enabled;
            chkIceGrid.Enabled = enabled;
            chkIcePatch2.Enabled = enabled;
            chkIceSSL.Enabled = enabled;
            chkIceStorm.Enabled = enabled;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if(!_changed)
            {
                if(txtExtraOptions.Modified)
                {
                    _changed = true;
                }
                else if(txtIceHome.Modified)
                {
                    _changed = true;
                }
            }
            if(_changed)
            {
                System.Windows.Forms.Cursor c = Cursor.Current;
                Cursor = Cursors.WaitCursor;
                Builder builder = Connect.getBuilder();
                builder.cleanProject(_project);
                builder.buildCSharpProject(_project, true);
                Cursor = c;
            }
            Close();
        }

        private void btnSelectIceHome_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = Util.getAbsoluteIceHome(_project);
            dialog.Description = "Select Ice Home Installation Directory";
            DialogResult result = dialog.ShowDialog();
            if(result == DialogResult.OK)
            {
                Util.updateIceHome(_project, dialog.SelectedPath, false);
                load();
                _changed = true;
            }
        }

        private void txtIceHome_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Return)
            {
                updateIceHome();
                e.Handled = true;
            }
        }

        private void txtIceHome_LostFocus(object sender, EventArgs e)
        {
            updateIceHome();
        }

        private void updateIceHome()
        {
            if(!_iceHomeUpdating)
            {
                _iceHomeUpdating = true;
                if(!txtIceHome.Text.Equals(Util.getProjectProperty(_project, Util.PropertyNames.IceHome),
                                           StringComparison.CurrentCultureIgnoreCase))
                {
                    Util.updateIceHome(_project, txtIceHome.Text, false);
                    load();
                    _changed = true;
                    txtIceHome.Modified = false;
                }
                _iceHomeUpdating = false;
            }
        }

        private void chkIcePrefix_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            Util.setProjectProperty(_project, Util.PropertyNames.IcePrefix, chkIcePrefix.Checked.ToString());
            _changed = true;
            Cursor = c;
        }

        private void chkTie_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            Util.setProjectProperty(_project, Util.PropertyNames.IceTie, chkTie.Checked.ToString());
            _changed = true;
            Cursor = c;
        }

        private void chkStreaming_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            Util.setProjectProperty(_project, Util.PropertyNames.IceStreaming, chkStreaming.Checked.ToString());
            _changed = true;
            Cursor = c;
        }
        
        private void chkChecksum_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            Util.setProjectProperty(_project, Util.PropertyNames.IceChecksum, chkChecksum.Checked.ToString());
            _changed = true;
            Cursor = c;
        }
        
        private void saveSliceIncludes()
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            IncludePathList paths = new IncludePathList();
            foreach(String s in includeDirList.Items)
            {
                paths.Add(s.Trim());
            }
            Util.setProjectProperty(_project, Util.PropertyNames.IceIncludePath, paths.ToString());
            _changed = true;
            Cursor = c;
        }

        private void btnAddInclude_Click(object sender, EventArgs e)
        {
            endEditIncludeDir(false);
            includeDirList.Items.Add("");
            includeDirList.SelectedIndex = includeDirList.Items.Count - 1;
            beginEditIncludeDir();
        }

        private void btnRemoveInclude_Click(object sender, EventArgs e)
        {
            endEditIncludeDir(false);
            if(includeDirList.SelectedIndex != -1)
            {
                System.Windows.Forms.Cursor c = Cursor.Current;
                Cursor = Cursors.WaitCursor;
                int selected = includeDirList.SelectedIndex;
                includeDirList.Items.RemoveAt(selected);
                if(includeDirList.Items.Count > 0)
                {
                    if(selected > 0)
                    {
                        selected -= 1;
                    }
                    includeDirList.SelectedIndex = selected;
                }
                saveSliceIncludes();
                Cursor = c;
            }
        }

        private void btnMoveIncludeUp_Click(object sender, EventArgs e)
        {
            endEditIncludeDir(false);
            int index = includeDirList.SelectedIndex;
            if(index > 0)
            {
                System.Windows.Forms.Cursor c = Cursor.Current;
                Cursor = Cursors.WaitCursor;
                string current = includeDirList.SelectedItem.ToString();
                includeDirList.Items.RemoveAt(index);
                includeDirList.Items.Insert(index - 1, current);
                includeDirList.SelectedIndex = index - 1;
                saveSliceIncludes();
                Cursor = c;
            }
            resetIncludeDirChecks();
        }

        private void btnMoveIncludeDown_Click(object sender, EventArgs e)
        {
            endEditIncludeDir(false);
            int index = includeDirList.SelectedIndex;
            if(index < includeDirList.Items.Count - 1)
            {
                System.Windows.Forms.Cursor c = Cursor.Current;
                Cursor = Cursors.WaitCursor;
                string current = includeDirList.SelectedItem.ToString();
                includeDirList.Items.RemoveAt(index);
                includeDirList.Items.Insert(index + 1, current);
                includeDirList.SelectedIndex = index + 1;
                saveSliceIncludes();
                Cursor = c;
            }
            resetIncludeDirChecks();
        }

        private void resetIncludeDirChecks()
        {
            _initialized = false;
            for(int i = 0; i < includeDirList.Items.Count; i++)
            {
                String path = includeDirList.Items[i].ToString();
                if(String.IsNullOrEmpty(path))
                {
                    continue;
                }

                if(Path.IsPathRooted(path))
                {
                    includeDirList.SetItemCheckState(i, CheckState.Checked);
                }
                else
                {
                    includeDirList.SetItemCheckState(i, CheckState.Unchecked);
                }
            }
            _initialized = true;
        }

        private void includeDirList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string path = includeDirList.Items[e.Index].ToString();
            if(!Util.containsEnvironmentVars(path))
            {
                if(e.NewValue == CheckState.Unchecked)
                {
                   path = Util.relativePath(Path.GetDirectoryName(_project.FileName), path);
                }
                else if(e.NewValue == CheckState.Checked)
                {
                   if(!Path.IsPathRooted(path))
                   {
                       path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_project.FileName), path));
                   }
                }
            }
            includeDirList.Items[e.Index] = path;
            saveSliceIncludes();
            _changed = true;
        }

        private void txtExtraOptions_LostFocus(object sender, EventArgs e)
        {
            if(txtExtraOptions.Modified)
            {
                Util.setProjectProperty(_project, Util.PropertyNames.IceExtraOptions, txtExtraOptions.Text);
                _changed = true;
            }
        }

        private void chkGlacier2_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("Glacier2", chkGlacier2.Checked);
        }
        
        private void componentChanged(string name, bool value)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            if(_initialized)
            {
                if(value)
                {
                    Util.addCSharpReference(_project, name);
                }
                else
                {
                    Util.removeCSharpReference(_project, name);
                }
                _changed = true;
            }
            Cursor = c;        
        }

        private void chkIce_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("Ice", chkIce.Checked);
        }

        private void chkIceBox_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("IceBox", chkIceBox.Checked);
        }

        private void chkIceGrid_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("IceGrid", chkIceGrid.Checked);
        }

        private void chkIcePatch2_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("IcePatch2", chkIcePatch2.Checked);
        }

        private void chkIceSSL_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("IceSSL", chkIceSSL.Checked);
        }

        private void chkIceStorm_CheckedChanged(object sender, EventArgs e)
        {
            componentChanged("IceStorm", chkIceStorm.Checked);
        }

        private void chkConsole_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor c = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            Util.setProjectProperty(_project, Util.PropertyNames.ConsoleOutput, chkConsole.Checked.ToString());
            Cursor = c;
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            beginEditIncludeDir();
        }

        private void includeDirList_SelectedIndexChanged(object sender, EventArgs e)
        {
            endEditIncludeDir(false);
        }

        private void beginEditIncludeDir()
        {
            endEditIncludeDir(false);
            CancelButton = null;
            if(includeDirList.SelectedIndex != -1)
            {
                int index = includeDirList.SelectedIndex;
                _txtIncludeDir = new TextBox();
                _txtIncludeDir.Text = includeDirList.Items[includeDirList.SelectedIndex].ToString();

                includeDirList.SelectionMode = SelectionMode.One;

                Rectangle rect = includeDirList.GetItemRectangle(includeDirList.SelectedIndex);
                _txtIncludeDir.Location = new Point(includeDirList.Location.X + 2,
                                                    includeDirList.Location.Y + rect.Y);
                _txtIncludeDir.Width = includeDirList.Width - 50;
                _txtIncludeDir.Parent = includeDirList;
                _txtIncludeDir.KeyDown += new KeyEventHandler(includeDirKeyDown);
                groupBox1.Controls.Add(_txtIncludeDir);

                _btnSelectInclude = new Button();
                _btnSelectInclude.Text = "...";
                _btnSelectInclude.Location = new Point(includeDirList.Location.X + _txtIncludeDir.Width,
                                                       includeDirList.Location.Y + rect.Y);
                _btnSelectInclude.Width = 49;
                _btnSelectInclude.Height = _txtIncludeDir.Height;
                _btnSelectInclude.Click += new EventHandler(selectIncludeClicked);
                groupBox1.Controls.Add(_btnSelectInclude);


                _txtIncludeDir.Show();
                _txtIncludeDir.BringToFront();
                _txtIncludeDir.Focus();

                _btnSelectInclude.Show();
                _btnSelectInclude.BringToFront();
            }
        }

        private void endEditIncludeDir(bool saveChanges)
        {
            String path;
            lock(this)
            {
                CancelButton = btnClose;
                if(_txtIncludeDir == null || _btnSelectInclude == null)
                {
                    return;
                }
                path = _txtIncludeDir.Text;

                this.groupBox1.Controls.Remove(_txtIncludeDir);
                _txtIncludeDir = null;

                this.groupBox1.Controls.Remove(_btnSelectInclude);
                _btnSelectInclude = null;

                if (String.IsNullOrEmpty(path))
                {
                    return;
                }
            }

            if(includeDirList.SelectedIndex != -1 && saveChanges)
            {
                if(!path.Equals(includeDirList.Items[includeDirList.SelectedIndex].ToString(),
                                               StringComparison.CurrentCultureIgnoreCase))
                {
                    includeDirList.Items[includeDirList.SelectedIndex] = path;
                    if(Path.IsPathRooted(path))
                    {
                        includeDirList.SetItemCheckState(includeDirList.SelectedIndex, CheckState.Checked);
                    }
                    else
                    {
                        includeDirList.SetItemCheckState(includeDirList.SelectedIndex, CheckState.Unchecked);
                    }
                    saveSliceIncludes();
                }
            }
        }

        private void includeDirKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode.Equals(Keys.Escape))
            {
                endEditIncludeDir(false);
            }
            if(e.KeyCode.Equals(Keys.Enter))
            {
                endEditIncludeDir(true);
            }
        }

        private void selectIncludeClicked(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            string projectDir = Path.GetFullPath(Path.GetDirectoryName(_project.FileName));
            dialog.SelectedPath = projectDir;
            dialog.Description = "Slice Include Directory";
            DialogResult result = dialog.ShowDialog();
            if(result == DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                if(!Util.containsEnvironmentVars(path))
                {
                    path = Util.relativePath(projectDir, Path.GetFullPath(path));
                }
                _txtIncludeDir.Text = path;
            }
            endEditIncludeDir(true);
        }
        
        private bool _changed = false;
        private bool _initialized = false;
        private Project _project;
        private bool _iceHomeUpdating = false;
        private TextBox _txtIncludeDir = null;
        private Button _btnSelectInclude = null;
    }
}
